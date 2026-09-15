using LoboBranco.Combat;
using UnityEngine;

namespace LoboBranco.AI
{
    /// <summary>
    /// Autoridade: o host (ADR 0008). Ele so existe de verdade em quem decide; no cliente
    /// ninguem pede token, porque no cliente ninguem decide atacar.
    ///
    /// O dono dos tokens de ataque do encontro (docs/07 secao 6). Ele nao manda ninguem
    /// atacar: ele so responde "agora nao" para a terceira criatura que quer golpear o
    /// mesmo bruxo, e essa recusa e o que transforma um amontoado em um combate legivel.
    ///
    /// Ele e procurado por <see cref="Current"/> e nao apontado no Inspector porque as
    /// criaturas sao prefab, e prefab nao guarda referencia de cena. O acesso estatico e
    /// deliberado e limitado: um encontro por vez e o que o slice tem, ja que ele e uma
    /// arena so. Quando o mundo do M3 tiver encontros simultaneos, isto vira um coordenador
    /// por volume de encontro e as criaturas passam a perguntar ao volume em que estao.
    ///
    /// A ausencia dele nao e silenciosa. Sem coordenador na cena, toda criatura ataca ao
    /// mesmo tempo, e esse e exatamente o sintoma que a tarefa 1.22 existe para evitar: um
    /// erro no Console e melhor do que descobrir isso num playtest.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EncounterCoordinator : MonoBehaviour
    {
        [Header("Dados")]
        [Tooltip("De onde sai o teto de atacantes por alvo. Sem asset, valem dois.")]
        [SerializeField] CombatTuningDef tuning;

        static EncounterCoordinator s_current;

        // Um bit para avisar uma vez so. Sem ele, uma cena sem coordenador enche o Console
        // com a mesma linha a cada golpe de cada criatura.
        static bool s_warnedMissing;

        readonly AttackTokenPool _tokens = new AttackTokenPool();

        /// <summary>O coordenador da cena, ou nulo quando nao ha nenhum.</summary>
        public static EncounterCoordinator Current => s_current;

        /// <summary>Os tokens, para o painel de debug e para os testes.</summary>
        public AttackTokenPool Tokens => _tokens;

        void Awake()
        {
            _tokens.MaxPerTarget = tuning != null ? tuning.maxAttackersPerTarget : 2;
        }

        void OnEnable()
        {
            if (s_current != null && s_current != this)
            {
                Debug.LogWarning(
                    $"{name}: ja existe um {nameof(EncounterCoordinator)} na cena. " +
                    "Os dois concederiam tokens sem saber um do outro.", this);
            }

            s_current = this;
        }

        void OnDisable()
        {
            if (s_current == this) s_current = null;

            // Sair de cena com tokens concedidos deixaria criaturas achando que ainda tem
            // permissao. Limpar aqui faz a proxima entrada comecar do zero.
            _tokens.Clear();
        }

        // ---------------------------------------------------------------- comandos

        /// <summary>
        /// Pede permissao para golpear. Sem coordenador na cena a permissao e dada, e o
        /// erro fica no Console: recusar todos os golpes deixaria o combate sem inimigos,
        /// que e pior do que deixa-lo desorganizado.
        /// </summary>
        public static bool TryAcquire(ulong attackerId, ulong targetId)
        {
            if (s_current == null)
            {
                WarnMissing();
                return true;
            }

            return s_current._tokens.TryAcquire(attackerId, targetId);
        }

        static void WarnMissing()
        {
            if (s_warnedMissing) return;

            s_warnedMissing = true;
            Debug.LogError(
                $"Cena sem {nameof(EncounterCoordinator)}: toda criatura vai golpear ao mesmo " +
                "tempo, que e exatamente o que o token de ataque existe para evitar (docs/07 secao 6).");
        }

        public static void Release(ulong attackerId)
        {
            if (s_current == null) return;

            s_current._tokens.Release(attackerId);
        }

        /// <summary>Chamado quando um alvo cai ou sai da sessao.</summary>
        public static void ReleaseAllFor(ulong targetId)
        {
            if (s_current == null) return;

            s_current._tokens.ReleaseAllFor(targetId);
        }
    }
}
