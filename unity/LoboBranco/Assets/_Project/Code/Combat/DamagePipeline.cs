using System;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Resolve um golpe percorrendo os onze estagios do docs/03 secao 9, na ordem.
    ///
    /// Classe simples, sem MonoBehaviour, com o contexto reutilizado entre chamadas.
    /// Nao e reentrante nem segura para varias threads: um golpe por vez, na thread
    /// principal, que e como o jogo funciona.
    /// </summary>
    public sealed class DamagePipeline
    {
        readonly IDamageStage[] _stages;
        readonly DamageContext _context = new DamageContext();
        readonly CombatTuningDef _tuning;

        /// <summary>
        /// Liga o registro passo a passo do calculo. Custa alocacao de string por golpe,
        /// entao fica desligado no jogo e ligado na cena de sandbox.
        /// </summary>
        public bool LoggingEnabled { get; set; }

        /// <summary>Contexto do ultimo golpe resolvido, para inspecao e debug.</summary>
        public DamageContext LastContext => _context;

        public DamagePipeline(CombatTuningDef tuning, IDamageStage[] stages = null)
        {
            _tuning = tuning != null
                ? tuning
                : throw new ArgumentNullException(nameof(tuning));

            _stages = stages ?? CreateDefaultStages();
        }

        /// <summary>
        /// A ordem canonica. Ela e a especificacao: trocar duas linhas aqui muda o
        /// balanceamento do jogo inteiro, porque armadura entra por subtracao.
        /// </summary>
        public static IDamageStage[] CreateDefaultStages() => new IDamageStage[]
        {
            new WeaponBaseStage(),          //  1
            new StanceMultiplierStage(),    //  2
            new StanceAffinityStage(),      //  3
            new WeaponMaterialStage(),      //  4
            new FlowChainStage(),           //  5
            new BladeOilStage(),            //  6
            new BestiaryKnowledgeStage(),   //  7
            new BuffStage(),                //  8
            new CriticalHitStage(),         //  9
            new ArmorStage(),               // 10
            new ResistanceStage(),          // 11
        };

        /// <summary>
        /// Os estagios que valem para o dano de um sinal (tarefa 1.18d), na mesma ordem canonica,
        /// e sem os que sao de lamina.
        ///
        /// Postura, afinidade, material, Fluxo, oleo e critico falam da espada e do golpe, e um
        /// sinal nao tem nenhum dos dois: fogo nao e aco nem prata, e oleo esta na lamina. O bonus
        /// plano do estagio 1 tambem fica de fora, porque ele soma ao dano da arma. O que sobra vale
        /// para qualquer dano: pesquisar o bestiario, a pocao, a armadura e a resistencia ao tipo,
        /// que e onde mora a fraqueza a fogo do docs/03 secao 8.
        ///
        /// Lista separada, e nao estagio perguntando a origem do dano: os onze estagios continuam
        /// sem saber que sinal existe, e a razao de 5,3 vezes do golpe fica intocada.
        /// </summary>
        public static IDamageStage[] CreateSignStages() => new IDamageStage[]
        {
            new BestiaryKnowledgeStage(),   //  7
            new BuffStage(),                //  8
            new ArmorStage(),               // 10
            new ResistanceStage(),          // 11
        };

        /// <summary>Calcula o dano sem aplicar. Use para previsao e para teste.</summary>
        public DamageResult Resolve(in DamageRequest request)
        {
            _context.Begin(request, LoggingEnabled);

            for (int i = 0; i < _stages.Length; i++)
                _stages[i].Apply(_context, _tuning);

            return _context.ToResult();
        }

        /// <summary>Calcula e entrega ao alvo.</summary>
        public DamageResult Deal(in DamageRequest request)
        {
            DamageResult result = Resolve(request);
            request.Target?.ApplyDamage(result);
            return result;
        }

        public int StageCount => _stages.Length;
    }
}
