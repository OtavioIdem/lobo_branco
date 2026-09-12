using System;
using LoboBranco.Stats;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Autoridade: o host, por meio do <see cref="CharacterVitals"/> ao lado. Quem tira
    /// vida e quem conta o tempo de levantar e so ele; piscar, tombar e ficar de pe de
    /// novo sao reacao local ao numero que chegou replicado.
    ///
    /// Alvo greybox da cena de sandbox: uma capsula que recebe dano, mostra que recebeu e
    /// volta a ficar de pe depois de um tempo, para nao ter que reiniciar a cena a cada
    /// teste de balanceamento.
    ///
    /// Ele ja nao guarda mais nenhum numero nem nenhuma classificacao: a vida e do
    /// <see cref="CharacterVitals"/> e a especie e do <see cref="MonsterDef"/>. O que
    /// sobrou aqui e o que e mesmo de sandbox, o piscar e o levantar sozinho, e a ponte
    /// entre os dois. O inimigo de verdade da tarefa 1.21 reusa os dois assets e troca
    /// este componente por um que tenha behavior tree.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterVitals))]
    public sealed class CombatDummy : MonoBehaviour, IDamageable
    {
        [Header("Especie")]
        [Tooltip("Classe, arquetipo e vulnerabilidades. Sem asset, vale o neutro de besta agil.")]
        [SerializeField] MonsterDef monster;

        [Header("Sandbox")]
        [Tooltip("Segundos ate voltar de pe. Nao e balanceamento: e conveniencia de teste.")]
        [SerializeField] float reviveDelay = 4f;

        [Tooltip("Cor do piscar ao levar dano. Sem retorno visual e impossivel julgar o alcance.")]
        [SerializeField] Color hitFlashColor = Color.white;

        [SerializeField] float hitFlashDuration = 0.12f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        CharacterVitals _vitals;
        Renderer[] _renderers;
        MaterialPropertyBlock _block;
        Color _restColor = Color.red;
        float _flashRemaining;
        float _reviveRemaining;
        bool _down;

        // ---------------------------------------------------------------- leitura

        public StatSheet Stats => _vitals != null ? _vitals.Stats : null;

        // Sem asset de especie o alvo continua existindo, como besta agil e sem oleo que
        // case: uma capsula muda e melhor do que uma capsula que nao aceita golpe.
        public CreatureClass CreatureClass => monster != null ? monster.creatureClass : Combat.CreatureClass.Beast;
        public StanceArchetype Archetype => monster != null ? monster.archetype : StanceArchetype.Agile;
        public OilClass VulnerableToOil => monster != null ? monster.vulnerableToOil : OilClass.None;

        public float CurrentVitality => _vitals != null ? _vitals.CurrentVitality : 0f;
        public float MaxVitality => _vitals != null ? _vitals.MaxVitality : 0f;
        public bool IsDown => _vitals != null && _vitals.IsDown;

        /// <summary>
        /// Dispara a cada golpe recebido, so em quem resolveu o golpe. Quem quiser reagir
        /// em todas as maquinas escuta <see cref="CharacterVitals.VitalityChanged"/>.
        /// </summary>
        public event Action<CombatDummy, DamageResult> Damaged;

        // ---------------------------------------------------------------- ciclo

        void Awake()
        {
            _vitals = GetComponent<CharacterVitals>();

            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            _block = new MaterialPropertyBlock();

            if (_renderers.Length > 0 && _renderers[0].sharedMaterial != null &&
                _renderers[0].sharedMaterial.HasProperty(BaseColorId))
                _restColor = _renderers[0].sharedMaterial.GetColor(BaseColorId);
        }

        void OnEnable() => _vitals.VitalityChanged += OnVitalityChanged;

        void OnDisable() => _vitals.VitalityChanged -= OnVitalityChanged;

        void Update()
        {
            float deltaTime = Time.deltaTime;

            if (_flashRemaining > 0f)
            {
                _flashRemaining -= deltaTime;
                if (_flashRemaining <= 0f) Paint(_restColor);
            }

            // Levantar e decisao, e decisao e do host. O cliente nao conta este tempo:
            // ele so ve a vida voltar e poe a capsula de pe.
            if (_reviveRemaining > 0f && _vitals.CanResolve)
            {
                _reviveRemaining -= deltaTime;
                if (_reviveRemaining <= 0f) _vitals.RestoreToFull();
            }
        }

        // ---------------------------------------------------------------- IDamageable

        /// <summary>
        /// Resistencia e propriedade da especie, entao ela vem do asset. Os valores em si
        /// ainda sao neutros: quais criaturas resistem a que e decisao de balanceamento, e
        /// ela e a tarefa 1.30, com o jogo rodando.
        /// </summary>
        public float GetResistance(DamageType type) => monster != null ? monster.GetResistance(type) : 1f;

        public void ApplyDamage(in DamageResult result)
        {
            // Chegar aqui sem autoridade significa que alguem rodou o pipeline no lugar
            // errado. Recusar e melhor do que tirar vida que o host nunca vai confirmar.
            if (!_vitals.CanResolve)
            {
                Debug.LogError($"{name}: dano resolvido fora do host. Ver ADR 0008.", this);
                return;
            }

            if (IsDown) return;

            _vitals.ApplyDamage(result.Amount);

            Damaged?.Invoke(this, result);
        }

        // ---------------------------------------------------------------- interno

        /// <summary>
        /// Roda em todas as maquinas, porque a vida chega replicada em todas elas. E o que
        /// faz o piscar do companheiro aparecer na sua tela sem custar um RPC.
        /// </summary>
        void OnVitalityChanged(float before, float after)
        {
            if (after < before)
            {
                Paint(hitFlashColor);
                _flashRemaining = hitFlashDuration;
            }

            if (after <= 0f && !_down) GoDown();
            else if (after > 0f && _down) StandUp();
        }

        void GoDown()
        {
            _down = true;
            _reviveRemaining = reviveDelay;

            // Deita a capsula em vez de destrui-la: o alvo continua existindo para
            // inspecao e a lista de ja-atingidos do golpe corrente segue valida.
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);

            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }

        void StandUp()
        {
            _down = false;
            _reviveRemaining = 0f;

            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            Paint(_restColor);

            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = true;
        }

        /// <summary>
        /// MaterialPropertyBlock em vez de <c>renderer.material</c>: o segundo instancia
        /// um material novo por objeto no primeiro acesso, e isso vaza memoria em cena.
        /// </summary>
        void Paint(Color color)
        {
            if (_renderers == null) return;

            _block.SetColor(BaseColorId, color);

            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].SetPropertyBlock(_block);
        }
    }
}
