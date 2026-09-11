using System;
using LoboBranco.Stats;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Alvo greybox da cena de sandbox: uma capsula que recebe dano, mostra que recebeu e
    /// volta a ficar de pe depois de um tempo, para nao ter que reiniciar a cena a cada
    /// teste de balanceamento.
    ///
    /// Provisorio. O inimigo de verdade nasce do <c>MonsterDef</c> (tarefa 1.20) com a
    /// behavior tree da tarefa 1.21; ai a classificacao, as resistencias e a tabela de
    /// loot saem daqui para o asset, e este componente vira apenas o que aplica dano.
    /// Ate la, os unicos numeros vivem no <see cref="StatBlockDef"/>, em asset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatDummy : MonoBehaviour, IDamageable
    {
        [Header("Dados")]
        [Tooltip("Vitalidade e armadura. docs/03 secao 12.")]
        [SerializeField] StatBlockDef statBlock;

        [Header("Classificacao (migra para MonsterDef na tarefa 1.20)")]
        [SerializeField] CreatureClass creatureClass = Combat.CreatureClass.Beast;
        [SerializeField] StanceArchetype archetype = StanceArchetype.Agile;
        [SerializeField] OilClass vulnerableToOil = OilClass.Beast;

        [Header("Sandbox")]
        [Tooltip("Segundos ate voltar de pe. Nao e balanceamento: e conveniencia de teste.")]
        [SerializeField] float reviveDelay = 4f;

        [Tooltip("Cor do piscar ao levar dano. Sem retorno visual e impossivel julgar o alcance.")]
        [SerializeField] Color hitFlashColor = Color.white;

        [SerializeField] float hitFlashDuration = 0.12f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        StatSheet _stats;
        Renderer[] _renderers;
        MaterialPropertyBlock _block;
        Color _restColor = Color.red;
        float _flashRemaining;
        float _reviveRemaining;

        // ---------------------------------------------------------------- leitura

        public StatSheet Stats => _stats;
        public CreatureClass CreatureClass => creatureClass;
        public StanceArchetype Archetype => archetype;
        public OilClass VulnerableToOil => vulnerableToOil;

        public float CurrentVitality { get; private set; }
        public float MaxVitality => _stats?.Get(StatType.MaxVitality) ?? 0f;
        public bool IsDown => CurrentVitality <= 0f;

        /// <summary>Dispara a cada golpe recebido. O painel de debug escuta isto.</summary>
        public event Action<CombatDummy, DamageResult> Damaged;

        // ---------------------------------------------------------------- ciclo

        void Awake()
        {
            _stats = statBlock != null ? statBlock.CreateSheet() : new StatSheet();
            CurrentVitality = _stats.Get(StatType.MaxVitality);

            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            _block = new MaterialPropertyBlock();

            if (_renderers.Length > 0 && _renderers[0].sharedMaterial != null &&
                _renderers[0].sharedMaterial.HasProperty(BaseColorId))
                _restColor = _renderers[0].sharedMaterial.GetColor(BaseColorId);
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;

            if (_flashRemaining > 0f)
            {
                _flashRemaining -= deltaTime;
                if (_flashRemaining <= 0f) Paint(_restColor);
            }

            if (_reviveRemaining > 0f)
            {
                _reviveRemaining -= deltaTime;
                if (_reviveRemaining <= 0f) Revive();
            }
        }

        // ---------------------------------------------------------------- IDamageable

        /// <summary>
        /// Neutro para todo tipo, por enquanto. As resistencias sao propriedade da
        /// especie, entao elas nascem junto com o <c>MonsterDef</c> na tarefa 1.20;
        /// inventa-las aqui seria fixar numero de balanceamento em MonoBehaviour.
        /// </summary>
        public float GetResistance(DamageType type) => 1f;

        public void ApplyDamage(in DamageResult result)
        {
            if (IsDown) return;

            CurrentVitality = Mathf.Max(0f, CurrentVitality - result.Amount);

            Paint(hitFlashColor);
            _flashRemaining = hitFlashDuration;

            Damaged?.Invoke(this, result);

            if (CurrentVitality <= 0f)
                GoDown();
        }

        // ---------------------------------------------------------------- interno

        void GoDown()
        {
            _reviveRemaining = reviveDelay;

            // Deita a capsula em vez de destrui-la: o alvo continua existindo para
            // inspecao e a lista de ja-atingidos do golpe corrente segue valida.
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);

            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }

        void Revive()
        {
            CurrentVitality = _stats.Get(StatType.MaxVitality);
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
