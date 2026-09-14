using System.Collections.Generic;
using LoboBranco.Combat;
using LoboBranco.Stats;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Uma escola de bruxo, em asset (docs/13 secao 5.1). E a regra 7 do CLAUDE.md feita
    /// tipo: escola e dado, e um <c>if</c> por escola em codigo de combate e erro de revisao.
    ///
    /// Ela junta alavancas que ja existiam, e nenhuma nova:
    ///
    /// O bloco de atributos e o vies da escola. A intensidade de sinal mora nele, no
    /// atributo <c>SignIntensity</c>, e nao em um campo daqui: o atributo ja existe, ja aceita
    /// modificador de pocao, e um segundo lugar para o mesmo numero seria uma segunda verdade.
    ///
    /// Os tres golpes, um por postura, e a postura favorecida. <b>Aqui o docs/13 secao 5.1 esta
    /// impreciso</b>, e a correcao foi registrada nele: a afinidade que o
    /// <c>StanceAffinityStage</c> aplica compara a postura do golpe com o arquetipo do
    /// <em>alvo</em>, e a escola de quem bate nao entra nessa conta. A postura favorecida de
    /// uma escola e outra coisa, mais simples e sem multiplicador novo: e a postura com que o
    /// bruxo entra na luta, e a vaga em que a escola poe o golpe mais bem feito. Como o que
    /// muda e o <see cref="AttackDef"/> e nao um bonus de dano, a razao de 5,3 vezes do
    /// docs/03 fica intacta.
    ///
    /// Custo de sinal e filtro de vestigio (docs/13 secao 4.1) nao estao aqui, porque sinais e
    /// investigacao ainda nao existem (tarefas 1.18 e M3). Campo para sistema inexistente e
    /// promessa, nao dado.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Player/School", fileName = "School_")]
    public sealed class SchoolDef : ScriptableObject
    {
        [Header("Identidade")]
        [Tooltip("Nome exibido. Vem de asset e nunca de codigo (tech/adr/0005).")]
        public string displayName = "Escola";

        [Header("Atributos")]
        [Tooltip("O vies de atributo da escola. A intensidade de sinal mora aqui, no SignIntensity.")]
        public StatBlockDef statBlock;

        [Header("Posturas")]
        [Tooltip("Postura com que o bruxo entra na luta. E a vaga em que a escola poe o melhor golpe.")]
        public Stance favoredStance = Stance.Fast;

        [Tooltip("Golpe da postura Forte. Tem que declarar a postura Forte.")]
        public AttackDef strongAttack;

        [Tooltip("Golpe da postura Rapida. Tem que declarar a postura Rapida.")]
        public AttackDef fastAttack;

        [Tooltip("Golpe da postura Grupo. Tem que declarar a postura Grupo.")]
        public AttackDef groupAttack;

        /// <summary>O golpe desta escola para uma postura, ou nulo se a vaga estiver vazia.</summary>
        public AttackDef AttackFor(Stance stance) => stance switch
        {
            Stance.Strong => strongAttack,
            Stance.Group => groupAttack,
            _ => fastAttack,
        };

        /// <summary>
        /// Escreve em <paramref name="problems"/> o que impede esta escola de ser jogada. Nada
        /// escrito quer dizer que ela esta pronta.
        ///
        /// A regra mais importante e a da vaga: cada golpe tem que declarar a postura da vaga
        /// em que esta. O golpe viaja pela rede como postura, e o host resolve o asset do
        /// proprio lado por essa postura. Um golpe Forte na vaga Rapida faria o dono desferir
        /// um golpe e o host resolver outro, com tempo e dano diferentes, sem erro nenhum.
        /// </summary>
        public void CollectProblems(List<string> problems)
        {
            if (problems == null) return;

            if (statBlock == null)
                problems.Add("sem bloco de atributos");

            CheckSlot(Stance.Strong, strongAttack, problems);
            CheckSlot(Stance.Fast, fastAttack, problems);
            CheckSlot(Stance.Group, groupAttack, problems);

            if (AttackFor(favoredStance) == null)
                problems.Add($"a postura favorecida {favoredStance} nao tem golpe");
        }

        static void CheckSlot(Stance slot, AttackDef attack, List<string> problems)
        {
            if (attack == null)
            {
                problems.Add($"sem golpe na postura {slot}");
                return;
            }

            if (attack.stance != slot)
                problems.Add($"o golpe '{attack.name}' esta na vaga {slot} mas declara a postura {attack.stance}");
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            var problems = new List<string>();
            CollectProblems(problems);

            for (int i = 0; i < problems.Count; i++)
                Debug.LogWarning($"{name}: {problems[i]}.", this);
        }
#endif
    }
}
