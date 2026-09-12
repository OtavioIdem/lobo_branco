using System;
using LoboBranco.Stats;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Uma especie, em asset (docs/07 secao 4.1). E o que separa "o que esta criatura e"
    /// de "como ela decide": aqui ficam classe, arquetipo, vulnerabilidades, resistencias,
    /// o alcance dos sentidos e qual golpe ela desfere; quem escolhe entre patrulhar,
    /// perseguir e bater e o grafo de behavior tree (ADR 0004), e nao este asset.
    ///
    /// Sentidos e golpe entraram na tarefa 1.21, e nao antes, pelo mesmo criterio que
    /// deixou bestiario e loot de fora: campo de dado para sistema inexistente e promessa,
    /// nao dado. Agora a IA existe e le estes numeros.
    ///
    /// Duas coisas que o esboco do docs/07 previa e que aqui saem diferentes, de proposito:
    ///
    /// Vitalidade, dano e armadura nao moram aqui. Eles moram no
    /// <see cref="StatBlockDef"/>, que ja existia antes deste tipo e ja e lido pelo
    /// <c>CharacterVitals</c> e pelo pipeline de dano. Repetir os mesmos tres numeros em
    /// dois assets criaria duas verdades sobre a vida do barghest, e a segunda so seria
    /// descoberta quando as duas discordassem.
    ///
    /// Entrada de bestiario, tabela de loot e vulnerabilidade a sinal tambem nao estao
    /// aqui ainda, porque bestiario, loot e sinais nao existem (tarefas 3.16, M2 e 1.18).
    /// Campo de dado para sistema que nao existe e promessa, nao dado.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Combat/Monster", fileName = "Monster_")]
    public sealed class MonsterDef : ScriptableObject
    {
        /// <summary>Resistencia da especie a um tipo de dano. Estagio 11 do pipeline.</summary>
        [Serializable]
        public struct Resistance
        {
            public DamageType type;

            [Tooltip("1,0 e neutro. Abaixo resiste, acima e fraqueza.")]
            [Min(0f)] public float multiplier;
        }

        [Header("Identidade")]
        [Tooltip("Nome exibido. Vem de asset e nunca de codigo (tech/adr/0005).")]
        public string displayName = "Criatura";

        [Tooltip("Decide aco contra prata no estagio 4 e qual oleo casa.")]
        public CreatureClass creatureClass = CreatureClass.Beast;

        [Tooltip("Decide qual postura tem afinidade com ela, no estagio 3.")]
        public StanceArchetype archetype = StanceArchetype.Agile;

        [Header("Numeros")]
        [Tooltip("Vitalidade, dano e armadura da especie. docs/03 secao 12.")]
        public StatBlockDef statBlock;

        [Header("Sentidos (docs/07 secao 6)")]
        [Tooltip("Ate onde ela enxerga, em metros. Fora disso o bruxo nao existe para ela.")]
        [Min(0f)] public float sightRange = 18f;

        [Tooltip("Metade da abertura do cone de visao, em graus. 60 da um campo de 120.")]
        [Range(1f, 180f)] public float sightHalfAngle = 60f;

        [Tooltip("Raio em que ela percebe sem ver, inclusive pelas costas. Zero desliga o ouvido.")]
        [Min(0f)] public float hearingRange = 6f;

        [Tooltip("Segundos que ela continua cacando depois de perder o alvo de vista.")]
        [Min(0f)] public float loseTargetAfter = 4f;

        [Tooltip("Velocidade de deslocamento em m/s. O MoveSpeed da folha de atributos " +
                 "multiplica este numero, e e por ali que uma poeira de Yrden vai lentificar.")]
        [Min(0f)] public float moveSpeed = 3.5f;

        [Header("Ataque")]
        [Tooltip("O golpe da criatura, com o proprio telegrafo na anticipacao (docs/03 secao 10). " +
                 "Sem asset ela persegue e nao bate.")]
        public AttackDef meleeAttack;

        [Tooltip("Garra, mordida ou lamina. O dano cru vem do AttackDamage do bloco de " +
                 "atributos, entao o campo de dano deste asset fica em zero: e dele que " +
                 "saem o tipo de dano e o material do estagio 4.")]
        public MeleeWeaponDef naturalWeapon;

        [Tooltip("Segundos de pausa entre um golpe e a proxima tentativa, alem da recuperacao.")]
        [Min(0f)] public float attackCooldown = 1.2f;

        [Header("Vulnerabilidades")]
        [Tooltip("Classe de oleo que multiplica o dano contra ela. None se nenhuma.")]
        public OilClass vulnerableToOil = OilClass.None;

        [Tooltip("So o que foge do neutro. Tipo nao listado vale 1,0.")]
        public Resistance[] resistances = Array.Empty<Resistance>();

        /// <summary>
        /// Multiplicador do estagio 11. Varredura linear de proposito: sao poucos tipos,
        /// e um Dictionary aqui alocaria no caminho de um golpe.
        /// </summary>
        public float GetResistance(DamageType type)
        {
            if (resistances == null) return 1f;

            for (int i = 0; i < resistances.Length; i++)
                if (resistances[i].type == type)
                    return resistances[i].multiplier;

            return 1f;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (resistances == null) return;

            // Tipo duplicado nao quebra nada (o primeiro vence), mas quase sempre e engano
            // de quem editou, e o sintoma aparece muito depois, em balanceamento.
            for (int i = 0; i < resistances.Length; i++)
            for (int j = i + 1; j < resistances.Length; j++)
                if (resistances[i].type == resistances[j].type)
                    Debug.LogWarning($"{name}: resistencia a '{resistances[i].type}' aparece mais de uma vez.", this);
        }
#endif
    }
}
