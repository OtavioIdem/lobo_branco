using LoboBranco.Combat;
using LoboBranco.Stats;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Autoridade: por enquanto, ninguem escreve em jogo. A escola vem do prefab, e o prefab e o
    /// mesmo em todas as maquinas, e isso importa mais do que parece: e com a escola que o host
    /// resolve o golpe do companheiro. Se host e dono discordassem de escola, o dono desferiria
    /// um golpe e o host resolveria outro, com atributos e tempos diferentes.
    ///
    /// Quando a tarefa 1.34 deixar escolher a escola na sala, ela vira um indice replicado,
    /// escrito pelo host a pedido do dono. E ela precisa ser decidida antes do spawn, e nao
    /// depois: a folha de atributos nasce no <c>Awake</c> do <see cref="CharacterVitals"/>, e
    /// trocar de escola com o personagem vivo e trocar a folha de alguem com vida e vigor em
    /// andamento.
    ///
    /// E o unico lugar do personagem que sabe qual e a escola. A folha de atributos pergunta
    /// por <see cref="IStatBlockProvider"/>, o atacante pergunta pelos golpes, e o cerebro
    /// pergunta pela postura inicial. Nenhum deles tem um <c>if</c> de escola.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSchool : MonoBehaviour, IStatBlockProvider
    {
        [Tooltip("A escola deste bruxo. docs/13 secao 5.")]
        [SerializeField] SchoolDef school;

        /// <summary>A escola, ou nulo quando o prefab esta mal montado.</summary>
        public SchoolDef School => school;

        public StatBlockDef StatBlock => school != null ? school.statBlock : null;

        /// <summary>Com que postura o bruxo entra na luta. Sem escola, a Rapida de sempre.</summary>
        public Stance FavoredStance => school != null ? school.favoredStance : Stance.Fast;

        /// <summary>O golpe da escola para uma postura, ou nulo sem escola.</summary>
        public AttackDef AttackFor(Stance stance) => school != null ? school.AttackFor(stance) : null;

        void Awake()
        {
            // Erro e nao aviso: um bruxo sem escola nao tem atributos nem golpes, e o sintoma
            // em jogo seria uma capsula que nao bate e morre com um golpe.
            if (school == null)
                Debug.LogError($"{name}: {nameof(PlayerSchool)} sem {nameof(SchoolDef)}. Bruxo sem atributos e sem golpes.", this);
        }
    }
}
