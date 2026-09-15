using LoboBranco.Combat;
using Unity.Netcode;
using UnityEngine;

namespace LoboBranco.AI
{
    /// <summary>
    /// Autoridade: o host decide que o golpe comecou; todo mundo desenha o aviso. O que
    /// viaja e um instante por golpe, e nao a fase a cada quadro.
    ///
    /// O problema que este componente resolve e de rede, antes de ser de arte. A linha do
    /// tempo do golpe roda so no host (ADR 0008), entao no cliente nao existe anticipacao
    /// nenhuma para desenhar: sem isto, quem hospeda ve o tell e o companheiro apanha sem
    /// aviso. O host manda o instante de inicio no relogio do servidor, e cada maquina
    /// calcula o aviso sozinha a partir do mesmo <see cref="AttackDef"/>, que ja esta no
    /// disco dela. Mandar a fase por quadro seria mandar pela rede uma conta que o outro
    /// lado sabe fazer.
    ///
    /// <b>Com ping, o cliente comeca o aviso atrasado, mas termina no tempo certo.</b> O
    /// tempo decorrido e medido contra o relogio do servidor, entao um cliente com 100 ms
    /// de ida e volta recebe a mensagem com o aviso ja no meio, e o aviso dele fecha junto
    /// com a janela de dano do host. A alternativa, comecar do zero ao receber, daria a
    /// todo cliente um aviso que termina depois do golpe ja ter acertado, e ensinaria o
    /// tempo errado justamente a quem joga com latencia. E um dos motivos de a anticipacao
    /// do barghest ser 0,65 s e nao 0,4: o que o ping come sai dessa folga.
    ///
    /// O golpe cortado no meio tambem viaja, porque sem isso o cliente continuaria avisando
    /// um golpe que o host ja cancelou. O fim natural nao viaja: cada maquina chega nele
    /// sozinha pela mesma conta.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMeleeAttacker))]
    public sealed class EnemyTelegraph : NetworkBehaviour
    {
        [Header("Dados")]
        [Tooltip("Cores e pose do aviso. Os tempos nao moram la: sao os do golpe.")]
        [SerializeField] TelegraphStyleDef style;

        [Header("Corpo")]
        [Tooltip("O que pisca e se abaixa. Nao pode ser a raiz: a raiz tem colisor e posicao em rede.")]
        [SerializeField] Transform body;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // Os valores de quem nao tem asset, iguais aos padroes do TelegraphStyleDef.
        const float FallbackStartWarning = 0.35f;
        const float FallbackCrouch = 0.15f;

        EnemyMeleeAttacker _attacker;
        Renderer[] _renderers;
        MaterialPropertyBlock _block;
        Color _restColor = Color.red;
        Vector3 _restScale = Vector3.one;
        Vector3 _restPosition;

        AttackDef _attack;
        double _startTime;
        bool _playing;

        // ---------------------------------------------------------------- leitura

        /// <summary>O aviso desenhado agora nesta maquina. O painel de debug mostra isto.</summary>
        public TelegraphSample Current { get; private set; } = TelegraphSample.Idle;

        /// <summary>
        /// O relogio contra o qual o golpe e medido. Com rede, e a estimativa do tempo do
        /// servidor, que no host e o proprio relogio dele; sem rede, o relogio local.
        /// </summary>
        double Now => IsSpawned ? NetworkManager.ServerTime.Time : Time.timeAsDouble;

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _attacker = GetComponent<EnemyMeleeAttacker>();
            _block = new MaterialPropertyBlock();

            Transform source = body != null ? body : transform;
            _renderers = source.GetComponentsInChildren<Renderer>(includeInactive: true);

            if (_renderers.Length > 0 && _renderers[0].sharedMaterial != null &&
                _renderers[0].sharedMaterial.HasProperty(BaseColorId))
                _restColor = _renderers[0].sharedMaterial.GetColor(BaseColorId);

            if (body != null)
            {
                _restScale = body.localScale;
                _restPosition = body.localPosition;
            }
        }

        void OnEnable()
        {
            _attacker.SwingStarted += OnSwingStarted;
            _attacker.SwingInterrupted += OnSwingInterrupted;
        }

        void OnDisable()
        {
            _attacker.SwingStarted -= OnSwingStarted;
            _attacker.SwingInterrupted -= OnSwingInterrupted;
        }

        void Update()
        {
            // Parado nao custa nada por quadro: nenhuma conta e nenhum SetPropertyBlock.
            if (!_playing) return;

            float startWarning = style != null ? style.startWarning : FallbackStartWarning;
            TelegraphSample sample = TelegraphCurve.Evaluate(_attack, (float)(Now - _startTime), startWarning);

            Apply(sample);

            if (sample.Finished) _playing = false;
        }

        // ----------------------------------------------------------------- avisos

        // Os eventos do atacante so disparam em quem resolve o golpe, e quem resolve e o
        // host. No cliente estes dois metodos nunca rodam: o aviso chega pelos RPCs.

        void OnSwingStarted()
        {
            double start = Now;

            if (IsSpawned) SwingStartedRpc(start);
            else Play(start);
        }

        void OnSwingInterrupted()
        {
            if (IsSpawned) SwingInterruptedRpc();
            else Stop();
        }

        // Server como permissao faz o proprio NGO recusar um aviso vindo de cliente. Sem
        // isso, um participante poderia fazer as criaturas dos outros fingirem que atacam.

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        void SwingStartedRpc(double serverStartTime) => Play(serverStartTime);

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        void SwingInterruptedRpc() => Stop();

        // ---------------------------------------------------------------- interno

        /// <summary>
        /// Comeca a desenhar. O golpe e lido do proprio atacante e nao viaja: a especie e a
        /// mesma nas duas maquinas. Quando a tarefa 1.24 der mais de um golpe a uma
        /// criatura, o RPC passa a levar o indice dele.
        /// </summary>
        void Play(double startTime)
        {
            _attack = _attacker.Attack;
            _startTime = startTime;
            _playing = _attack != null;
        }

        void Stop()
        {
            _playing = false;
            Apply(TelegraphSample.Idle);
        }

        void Apply(in TelegraphSample sample)
        {
            Current = sample;

            Color warningColor = style != null ? style.warningColor : Color.yellow;
            Color strikeColor = style != null ? style.strikeColor : Color.white;

            Color color = Color.Lerp(_restColor, warningColor, sample.Warning);
            color = Color.Lerp(color, strikeColor, sample.Flash);

            _block.SetColor(BaseColorId, color);
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].SetPropertyBlock(_block);

            ApplyCrouch(sample);
        }

        /// <summary>
        /// O corpo se abaixa conforme o aviso enche, e so durante a anticipacao. O pe fica
        /// no chao: encolher a capsula pelo centro a faria flutuar, e uma criatura flutuando
        /// antes de atacar parece erro de fisica e nao pose.
        /// </summary>
        void ApplyCrouch(in TelegraphSample sample)
        {
            if (body == null) return;

            float crouch = style != null ? style.crouch : FallbackCrouch;
            float amount = sample.Phase == AttackPhase.Anticipation ? crouch * sample.Warning : 0f;

            float height = _restScale.y * (1f - amount);
            float bottom = _restPosition.y - _restScale.y;

            body.localScale = new Vector3(
                _restScale.x * (1f + amount * 0.5f), height, _restScale.z * (1f + amount * 0.5f));
            body.localPosition = new Vector3(_restPosition.x, bottom + height, _restPosition.z);
        }
    }
}
