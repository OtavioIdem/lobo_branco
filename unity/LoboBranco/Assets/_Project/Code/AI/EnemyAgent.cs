using LoboBranco.Combat;
using LoboBranco.Core;
using LoboBranco.Stats;
using UnityEngine;
using UnityEngine.AI;

namespace LoboBranco.AI
{
    /// <summary>
    /// Autoridade: o host (ADR 0008). IA e do host, sempre, e este componente e onde essa
    /// regra e cobrada: fora do host ele nao procura alvo, nao percebe, nao gira e desliga
    /// o <see cref="NavMeshAgent"/>. Dois lados decidindo dariam dois monstros diferentes
    /// na mesma sessao, e o cliente veria o inimigo andando para um lado enquanto o dano
    /// chegaria do outro. O que o cliente ve e a posicao replicada, e so.
    ///
    /// E a ponte entre o grafo de behavior tree (ADR 0004) e o resto do jogo. O grafo
    /// pergunta tres coisas e manda duas, e nada alem disso: existe alvo, ele esta
    /// percebido, ele esta no alcance; encare-o, bata nele. Toda a percepcao e a busca
    /// acontecem aqui, em <c>Update</c>, e nao dentro dos nos, por um motivo pratico: um
    /// no so roda quando o galho dele esta ativo, e uma criatura que so percebe o bruxo
    /// enquanto esta no ramo de patrulha nunca o perceberia enquanto volta para o posto.
    ///
    /// A busca por alvo nao acontece todo quadro. Ela e uma consulta de esfera na fisica,
    /// e quatro por segundo por criatura ja e mais reacao do que qualquer um percebe, com
    /// um quarto do custo (regra 5 do CLAUDE.md).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterVitals))]
    public sealed class EnemyAgent : MonoBehaviour
    {
        [Header("Dados")]
        [Tooltip("A especie: de onde saem alcance de visao, cone, ouvido e memoria.")]
        [SerializeField] MonsterDef monster;

        [Header("Alvos")]
        [Tooltip("Deixe em Nothing para usar a mascara padrao de GameLayers.EnemyAttackTargets.")]
        [SerializeField] LayerMask targetMask;

        [Tooltip("O que corta a linha de visao. Deixe em Nothing para usar o cenario.")]
        [SerializeField] LayerMask sightBlockers;

        [Tooltip("Altura dos olhos acima da base. A linha de visao sai daqui, e nao do chao.")]
        [SerializeField] float eyeHeight = 1.2f;

        [Header("Corpo")]
        [Tooltip("Graus por segundo ao encarar o alvo. Lento demais e o golpe erra por giro.")]
        [SerializeField] float turnSpeedDegrees = 360f;

        [Tooltip("Segundos entre duas buscas por alvo. Quatro por segundo e o suficiente.")]
        [Min(0.05f)] [SerializeField] float scanInterval = 0.25f;

        [Tooltip("Teto de candidatos por busca. Em coop de quatro, quatro ja basta.")]
        [SerializeField] int maxCandidates = 8;

        readonly EnemySenses _senses = new EnemySenses();

        CharacterVitals _vitals;
        ControlStatus _control;
        NavMeshAgent _navAgent;
        Collider[] _candidates;
        LayerMask _resolvedTargetMask;
        LayerMask _resolvedBlockers;

        Transform _target;
        IDamageable _targetDamageable;
        float _scanRemaining;
        bool _navWasEnabled;
        bool _held;

        // ---------------------------------------------------------------- leitura

        /// <summary>Quem decide: o host, ou eu mesmo quando nao ha rede.</summary>
        public bool CanDecide => _vitals == null || _vitals.CanResolve;

        /// <summary>Ja caiu. Uma criatura no chao nao decide nada.</summary>
        public bool IsDown => _vitals != null && _vitals.IsDown;

        /// <summary>
        /// Atordoada ou derrubada (tarefa 1.18a). Nao anda, nao gira e devolve a vez de golpear.
        /// Sem <see cref="ControlStatus"/> no prefab, nunca.
        /// </summary>
        public bool IsIncapacitated => _control != null && _control.IsIncapacitated;

        /// <summary>O alvo corrente, ou nulo. E o que o no de aquisicao poe no quadro negro.</summary>
        public Transform Target => _target;

        public GameObject TargetObject => _target != null ? _target.gameObject : null;

        /// <summary>
        /// Verdadeiro enquanto ela caca, vendo o alvo ou lembrando dele. E a pergunta que
        /// separa o ramo de patrulha do ramo de combate.
        /// </summary>
        public bool IsAware => _senses.Aware && _target != null;

        /// <summary>Os sentidos, para o painel de debug e para os testes.</summary>
        public EnemySenses Senses => _senses;

        /// <summary>Distancia em que ela ronda o alvo enquanto espera a vez de golpear.</summary>
        public float EngagementDistance => monster != null ? monster.engagementDistance : 3.5f;

        /// <summary>Verdadeiro enquanto ela tem permissao para golpear (docs/07 secao 6).</summary>
        public bool HasAttackToken { get; private set; }

        /// <summary>
        /// O identificador estavel deste objeto, como numero. <c>GetInstanceID</c> saiu de
        /// circulacao no editor 6000.6, e <see cref="EntityId"/> nao serve de chave em uma
        /// classe pura: <c>ToULong</c> e a conversao oficial entre os dois mundos.
        /// </summary>
        public ulong EntityKey => EntityId.ToULong(gameObject.GetEntityId());

        /// <summary>Distancia no plano ate o alvo, ou infinito sem alvo.</summary>
        public float DistanceToTarget
        {
            get
            {
                if (_target == null) return float.PositiveInfinity;

                Vector3 delta = _target.position - transform.position;
                delta.y = 0f;

                return delta.magnitude;
            }
        }

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _vitals = GetComponent<CharacterVitals>();
            _control = GetComponent<ControlStatus>();
            _navAgent = GetComponent<NavMeshAgent>();
            _candidates = new Collider[Mathf.Max(1, maxCandidates)];

            // O espelho comeca valendo o estado real do componente. Comecando em falso, o
            // primeiro pedido de desligar no cliente acharia que ja estava desligado e
            // devolveria cedo, e o NavMeshAgent continuaria empurrando o transform contra
            // a posicao que chega replicada do host.
            _navWasEnabled = _navAgent != null && _navAgent.enabled;

            _resolvedTargetMask = targetMask.value != 0 ? targetMask : GameLayers.EnemyAttackTargets;
            _resolvedBlockers = sightBlockers.value != 0 ? sightBlockers : GameLayers.CameraBlockers;

            ApplyMonster();
        }

        /// <summary>
        /// Le os numeros da especie para dentro dos sentidos. Separado do <c>Awake</c>
        /// porque o dia em que uma criatura cega por Yrden existir (tarefa 1.18), e so
        /// mexer no <see cref="EnemySenses"/> e chamar isto de novo.
        /// </summary>
        void ApplyMonster()
        {
            if (monster == null)
            {
                Debug.LogWarning($"{name}: {nameof(EnemyAgent)} sem {nameof(MonsterDef)}. Valem os sentidos padrao.", this);
                return;
            }

            _senses.SightRange = monster.sightRange;
            _senses.SightHalfAngle = monster.sightHalfAngle;
            _senses.HearingRange = monster.hearingRange;
            _senses.LoseTargetAfter = monster.loseTargetAfter;
        }

        void Update()
        {
            // O cliente nao percebe e nao anda por conta propria: a posicao dele chega
            // replicada do host. Deixar o NavMeshAgent ligado aqui faria os dois empurrarem
            // o mesmo transform, e o sintoma seria o inimigo tremendo no lugar.
            if (!CanDecide)
            {
                SetNavEnabled(false);
                return;
            }

            SetNavEnabled(true);

            if (IsDown)
            {
                DropTarget();
                return;
            }

            float deltaTime = Time.deltaTime;

            // Sem controle ela para no lugar e devolve a vez de golpear, para que outra criatura
            // possa atacar o bruxo enquanto esta esta no chao: em coop, derrubar uma e abrir a
            // vaga dela. Os sentidos continuam contando, porque atordoada nao e esquecida.
            bool incapacitated = IsIncapacitated;
            SetHeld(incapacitated);

            if (incapacitated) ReleaseAttackToken();

            _scanRemaining -= deltaTime;
            if (_scanRemaining <= 0f)
            {
                _scanRemaining = scanInterval;
                ScanForTarget();
            }

            UpdateSenses(deltaTime);
            ApplySpeedFromStats();
        }

        // ------------------------------------------------------------- percepcao

        /// <summary>
        /// Procura o alvo mais proximo dentro do alcance de visao. Ela busca mesmo ja
        /// tendo alvo: em coop, o bruxo que chegou mais perto e mais relevante que o que
        /// ela viu primeiro, e sem isto dois jogadores dividiriam a atencao de um monstro
        /// pela ordem de chegada e nao pelo que estao fazendo.
        /// </summary>
        void ScanForTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, _senses.SightRange, _candidates, _resolvedTargetMask);

            Transform best = null;
            IDamageable bestDamageable = null;
            float bestSqr = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                Collider candidate = _candidates[i];
                if (candidate == null) continue;

                IDamageable damageable = ResolveDamageable(candidate);

                // Quem ja caiu deixa de ser alvo. Sem isto a criatura ficaria batendo em
                // um companheiro no chao enquanto o outro a corta pelas costas.
                if (damageable == null || damageable.IsDown) continue;

                float sqr = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sqr >= bestSqr) continue;

                bestSqr = sqr;
                best = candidate.transform;
                bestDamageable = damageable;
            }

            for (int i = 0; i < count; i++)
                _candidates[i] = null;

            if (best == null)
            {
                // Perder o candidato nao e perder o alvo: a memoria dos sentidos ainda
                // esta contando, e e ela que decide quando a caca acaba.
                if (_targetDamageable != null && _targetDamageable.IsDown)
                    DropTarget();

                return;
            }

            _target = best;
            _targetDamageable = bestDamageable;
        }

        void UpdateSenses(float deltaTime)
        {
            if (_target == null)
            {
                _senses.Tick(deltaTime, perceivedNow: false);
                return;
            }

            // Quem caiu deixa de ser alvo na hora, e nao na proxima varredura: a busca
            // roda 4 vezes por segundo, e ate um quarto de segundo batendo em um
            // companheiro caido e um quarto de segundo de token preso.
            if (_targetDamageable != null && _targetDamageable.IsDown)
            {
                EncounterCoordinator.ReleaseAllFor(EntityId.ToULong(_target.gameObject.GetEntityId()));
                DropTarget();
                return;
            }

            Vector3 eyes = transform.position + Vector3.up * eyeHeight;
            Vector3 targetPoint = _target.position + Vector3.up * eyeHeight;

            bool clear = !Physics.Linecast(eyes, targetPoint, _resolvedBlockers, QueryTriggerInteraction.Ignore);
            bool perceived = _senses.Perceives(transform.position, transform.forward, _target.position, clear);

            if (!_senses.Tick(deltaTime, perceived))
                DropTarget();
        }

        void DropTarget()
        {
            // Devolver o token antes de soltar o alvo. Sem isto, uma criatura que perde o
            // bruxo de vista no meio do golpe levaria a permissao dela embora, e a vaga
            // ficaria bloqueada ate o fim do encontro.
            ReleaseAttackToken();

            _target = null;
            _targetDamageable = null;
            _senses.Forget();
        }

        // ------------------------------------------------------------------ token

        /// <summary>
        /// Pede a vez de golpear ao coordenador do encontro (docs/07 secao 6). Devolve
        /// falso quando o alvo ja tem o teto de atacantes, e e assim que a terceira
        /// criatura passa a rondar em vez de somar o golpe dela ao dos outros dois.
        /// </summary>
        public bool TryTakeAttackToken()
        {
            if (_target == null) return false;

            HasAttackToken = EncounterCoordinator.TryAcquire(
                EntityKey, EntityId.ToULong(_target.gameObject.GetEntityId()));

            return HasAttackToken;
        }

        /// <summary>Devolve a vez. Chamado no fim de cada golpe, tenha ele acertado ou nao.</summary>
        public void ReleaseAttackToken()
        {
            if (!HasAttackToken) return;

            HasAttackToken = false;
            EncounterCoordinator.Release(EntityKey);
        }

        static IDamageable ResolveDamageable(Collider collider)
        {
            if (collider.TryGetComponent(out IDamageable target)) return target;

            return collider.GetComponentInParent<IDamageable>();
        }

        // ------------------------------------------------------------------ corpo

        void OnDisable()
        {
            // Sair de cena com o token na mao bloquearia a vaga de um alvo que continua
            // vivo. Uma criatura que morre, despawna ou e desligada devolve a vez.
            ReleaseAttackToken();
        }

        /// <summary>
        /// O ponto do anel de engajamento em que ela deveria esperar a vez.
        ///
        /// Ele e calculado a partir do lado em que ela ja esta, e nao de um angulo fixo:
        /// mandar todas as criaturas para o mesmo ponto do anel faria todas atravessarem o
        /// alvo para chegar la, o que e pior do que amontoar. O deslocamento angular
        /// separa duas que estejam no mesmo lado.
        /// </summary>
        public Vector3 CirclePosition(float angleOffsetDegrees)
        {
            if (_target == null) return transform.position;

            Vector3 fromTarget = transform.position - _target.position;
            fromTarget.y = 0f;

            // Em cima do alvo nao ha lado. Sair pela frente dele e melhor do que sortear:
            // e de onde o bruxo consegue ver a criatura recuando.
            if (fromTarget.sqrMagnitude < 0.0001f) fromTarget = _target.forward;

            fromTarget.Normalize();

            Vector3 rotated = Quaternion.Euler(0f, angleOffsetDegrees, 0f) * fromTarget;

            return _target.position + rotated * EngagementDistance;
        }

        /// <summary>
        /// Manda a criatura para um ponto. Devolve falso quando nao ha NavMeshAgent ou
        /// quando ela esta fora da malha, e nesse caso quem chamou move pelo transform.
        /// </summary>
        public bool MoveTo(Vector3 destination)
        {
            // A trava mora aqui, e nao nos nos, para valer em qualquer galho do grafo.
            if (IsIncapacitated) return false;

            if (_navAgent == null || !_navAgent.enabled || !_navAgent.isOnNavMesh) return false;

            _navAgent.SetDestination(destination);
            return true;
        }

        /// <summary>Para de andar, sem desligar o agente.</summary>
        public void StopMoving()
        {
            if (_navAgent == null || !_navAgent.enabled || !_navAgent.isOnNavMesh) return;

            _navAgent.ResetPath();
        }

        /// <summary>
        /// Gira em direcao ao alvo. Devolve verdadeiro quando ja esta encarando dentro da
        /// tolerancia, que e o que o no de ataque espera antes de deixar o golpe sair: um
        /// golpe desferido de lado erra por causa do filtro de arco da hitbox.
        /// </summary>
        public bool FaceTarget(float deltaTime, float toleranceDegrees = 12f)
        {
            if (_target == null || IsIncapacitated) return false;

            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.000001f) return true;

            Quaternion desired = Quaternion.LookRotation(toTarget, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, desired, turnSpeedDegrees * deltaTime);

            return Quaternion.Angle(transform.rotation, desired) <= toleranceDegrees;
        }

        /// <summary>
        /// A velocidade de deslocamento sai da especie, e nao do proprio
        /// <see cref="NavMeshAgent"/>: velocidade gravada no componente seria um numero de
        /// balanceamento fora de asset, e o campo do NavMeshAgent nao aceita modificador.
        ///
        /// O <c>MoveSpeed</c> da folha multiplica, e e por ali que uma poeira de Yrden vai
        /// lentificar (tarefa 1.18). Zero ali quer dizer "nao configurado", e o neutro e
        /// um: uma criatura cujo bloco de atributos nao lista o atributo anda normal, em
        /// vez de ficar parada por um numero que ninguem escreveu.
        /// </summary>
        void ApplySpeedFromStats()
        {
            if (_navAgent == null || monster == null) return;

            float multiplier = _vitals != null && _vitals.Stats != null
                ? _vitals.Stats.Get(StatType.MoveSpeed)
                : 1f;

            if (multiplier <= 0f) multiplier = 1f;

            _navAgent.speed = monster.moveSpeed * multiplier;
        }

        /// <summary>
        /// Segura o corpo no lugar. Zerar a velocidade junto importa: so parar o caminho deixaria
        /// o agente desacelerar, e um barghest atordoado deslizaria meio metro depois do Aard.
        /// </summary>
        void SetHeld(bool held)
        {
            if (_held == held) return;

            _held = held;

            if (_navAgent == null || !_navAgent.enabled || !_navAgent.isOnNavMesh) return;

            _navAgent.isStopped = held;

            if (!held) return;

            _navAgent.ResetPath();
            _navAgent.velocity = Vector3.zero;
        }

        void SetNavEnabled(bool value)
        {
            if (_navAgent == null || _navWasEnabled == value) return;

            _navWasEnabled = value;
            _navAgent.enabled = value;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Desenha o cone de visao e o circulo do ouvido. Sem isto, afinar percepcao e
        /// adivinhar por que a criatura reagiu ou nao reagiu.
        /// </summary>
        void OnDrawGizmosSelected()
        {
            if (monster == null) return;

            Vector3 eyes = transform.position + Vector3.up * eyeHeight;

            Gizmos.color = new Color(0.95f, 0.85f, 0.2f, 0.5f);
            Quaternion left = Quaternion.Euler(0f, -monster.sightHalfAngle, 0f);
            Quaternion right = Quaternion.Euler(0f, monster.sightHalfAngle, 0f);
            Gizmos.DrawLine(eyes, eyes + left * transform.forward * monster.sightRange);
            Gizmos.DrawLine(eyes, eyes + right * transform.forward * monster.sightRange);

            Gizmos.color = new Color(0.3f, 0.7f, 0.95f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, monster.hearingRange);
        }
#endif
    }
}
