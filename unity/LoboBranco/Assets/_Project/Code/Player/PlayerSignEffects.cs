using LoboBranco.Combat;
using LoboBranco.Core;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Autoridade: o host (ADR 0008, tech/adr/0012). O efeito de um sinal so acontece em quem
    /// resolve, porque o <see cref="PlayerAbilityCaster.CastTriggered"/> so dispara la. O que o
    /// efeito muda nos alvos, controle e vida, chega nas outras maquinas pelo estado replicado de
    /// cada alvo, e este componente nao manda mensagem nenhuma.
    ///
    /// A posicao e a frente usadas sao a copia do host, que o <c>NetworkTransform</c> traz do dono.
    /// E o mesmo arranjo da janela de dano do golpe, e pelo mesmo motivo: mandar a mira do dono
    /// junto com o pedido de efeito deixaria o cliente declarar onde o sinal pegou.
    ///
    /// E uma ponte fina. A area e os efeitos sao da habilidade, a variante e da escola, a
    /// intensidade e da folha de atributos pela conta do <see cref="CombatTuningDef"/>, e quem
    /// esta dentro e decidido pelo <see cref="SignResolver"/>. Aqui nao ha numero nenhum.
    ///
    /// Sem rede, o mesmo objeto e dono e host (risco X8 do doc 13).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerAbilityCaster))]
    public sealed class PlayerSignEffects : MonoBehaviour
    {
        [Header("Dados")]
        [Tooltip("De onde sai a conta da intensidade. Sem asset, os sinais cobram e nao fazem nada.")]
        [SerializeField] CombatTuningDef tuning;

        [Header("Alvos")]
        [Tooltip("Deixe em Nothing para usar a mascara padrao de GameLayers.PlayerAttackTargets.")]
        [SerializeField] LayerMask targetMask;

        [Tooltip("Teto de colisores por consulta. Estourar faz alvos alem do teto ficarem invisiveis.")]
        [SerializeField] int maxCollidersPerQuery = 32;

        [Tooltip("Teto de alvos por conjuracao, do mais perto para o mais longe. E de memoria, nao de design.")]
        [SerializeField] int maxTargetsPerCast = 16;

        [Header("Depuracao (cena de sandbox)")]
        [Tooltip("Segundos que a area do ultimo sinal fica desenhada na Scene view.")]
        [SerializeField] float gizmoSeconds = 1.5f;

        PlayerAbilityCaster _caster;
        CharacterVitals _vitals;
        PlayerSchool _school;
        SignResolver _resolver;
        LayerMask _resolvedMask;
        bool _warnedZeroIntensity;

        SignCast _lastCast;
        float _lastCastTime = float.NegativeInfinity;

        PlayerSchool School => _school != null ? _school : (_school = GetComponent<PlayerSchool>());

        // ---------------------------------------------------------------- leitura

        /// <summary>Quantas criaturas o ultimo sinal alcancou. So vale em quem resolve.</summary>
        public int LastTargetCount { get; private set; }

        /// <summary>A intensidade do ultimo sinal. So vale em quem resolve.</summary>
        public float LastIntensity { get; private set; }

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _caster = GetComponent<PlayerAbilityCaster>();
            _vitals = GetComponent<CharacterVitals>();

            if (tuning == null)
            {
                Debug.LogError($"{name}: {nameof(PlayerSignEffects)} sem {nameof(CombatTuningDef)}. Sinais sem efeito.", this);
                enabled = false;
                return;
            }

            _resolver = new SignResolver(maxCollidersPerQuery, maxTargetsPerCast);

            // LayerMask.GetMask aloca, entao ela nunca acontece no instante do efeito.
            _resolvedMask = targetMask.value != 0 ? targetMask : GameLayers.PlayerAttackTargets;
        }

        void OnEnable() => _caster.CastTriggered += OnCastTriggered;

        void OnDisable() => _caster.CastTriggered -= OnCastTriggered;

        // -------------------------------------------------------------- resolucao

        void OnCastTriggered(int slot, AbilityDef ability)
        {
            if (ability == null || _vitals == null) return;

            float intensity = tuning.SignIntensity(_vitals.Stats);
            WarnIfPowerless(intensity);

            var cast = new SignCast(_vitals, ability, transform.position, transform.forward, intensity);
            SignEffectDef[] variant = School != null ? School.VariantEffectsFor(ability) : null;

            LastTargetCount = _resolver.Resolve(cast, variant, _resolvedMask);
            LastIntensity = intensity;

            _lastCast = cast;
            _lastCastTime = Time.time;
        }

        /// <summary>
        /// Intensidade zero faz todo efeito sair com potencia zero: o sinal cobra, acerta e nao
        /// muda nada. Avisar uma vez liga o sintoma ("o abridor nao empurra") a causa, que e um
        /// atributo faltando no bloco da escola.
        /// </summary>
        void WarnIfPowerless(float intensity)
        {
            if (intensity > 0f || _warnedZeroIntensity) return;

            _warnedZeroIntensity = true;
            Debug.LogWarning(
                $"{name}: intensidade de sinal zero. O bloco de atributos precisa de Intelligence e SignIntensity.", this);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            AbilityDef ability = _lastCast.Ability;
            if (ability == null || Time.time - _lastCastTime > gizmoSeconds) return;

            SignArea area = ability.area;
            Vector3 origin = _lastCast.Origin + Vector3.up * 0.1f;

            Gizmos.color = new Color(0.4f, 0.8f, 1f);

            if (area.shape == SignAreaShape.Radius)
            {
                Gizmos.DrawWireSphere(origin, area.range);
            }
            else
            {
                Vector3 forward = new Vector3(_lastCast.Forward.x, 0f, _lastCast.Forward.z).normalized;
                const int Segments = 12;
                float half = area.coneAngleDegrees * 0.5f;
                Vector3 previous = origin;

                for (int i = 0; i <= Segments; i++)
                {
                    float angle = Mathf.Lerp(-half, half, i / (float)Segments);
                    Vector3 point = origin + Quaternion.Euler(0f, angle, 0f) * forward * area.range;
                    Gizmos.DrawLine(previous, point);
                    previous = point;
                }

                Gizmos.DrawLine(previous, origin);
            }

            if (_resolver == null) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < _resolver.TargetCount; i++)
            {
                CharacterVitals target = _resolver.TargetAt(i);
                if (target != null)
                    Gizmos.DrawLine(origin, target.transform.position + Vector3.up);
            }
        }
#endif
    }
}
