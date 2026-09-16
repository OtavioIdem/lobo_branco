using System.Collections.Generic;
using LoboBranco.Combat;
using LoboBranco.Stats;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O sinal como dado (tarefa 1.18b): a geometria da area, o que impede uma habilidade de
    /// alcancar alguem, e a conta da intensidade.
    ///
    /// A intensidade e matematica que ninguem confere. Um bloco de atributos sem o multiplicador
    /// faz todo efeito sair com potencia zero, e o sintoma em jogo e um sinal que cobra, acerta e
    /// nao muda nada, sem uma linha no Console.
    /// </summary>
    public sealed class SignDataTests
    {
        AbilityDef _sinal;
        CombatTuningDef _tuning;
        readonly List<string> _problemas = new List<string>();
        readonly List<Object> _criados = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _sinal = ScriptableObject.CreateInstance<AbilityDef>();
            _tuning = CombatTuningDef.CreateDefault();
            _criados.Add(_sinal);
            _criados.Add(_tuning);
            _problemas.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _criados.Count; i++)
                Object.DestroyImmediate(_criados[i]);

            _criados.Clear();
        }

        EfeitoDeSinalMudo CriarEfeito(string problema = null)
        {
            var efeito = ScriptableObject.CreateInstance<EfeitoDeSinalMudo>();
            efeito.name = "Efeito_Mudo";
            efeito.Problema = problema;
            _criados.Add(efeito);

            return efeito;
        }

        static SignArea Cone(float alcance, float abertura)
            => new SignArea { shape = SignAreaShape.Cone, range = alcance, coneAngleDegrees = abertura };

        // ---------------------------------------------------------------- geometria

        [Test]
        public void Cone_de_90_graus_aceita_44_graus_e_recusa_46()
        {
            SignArea cone = Cone(6f, 90f);
            Vector3 dentro = Quaternion.Euler(0f, 44f, 0f) * Vector3.forward * 3f;
            Vector3 fora = Quaternion.Euler(0f, -46f, 0f) * Vector3.forward * 3f;

            Assert.IsTrue(cone.IsWithinAngle(Vector3.zero, Vector3.forward, dentro));
            Assert.IsFalse(cone.IsWithinAngle(Vector3.zero, Vector3.forward, fora),
                "A abertura e total: 90 graus sao 45 para cada lado.");
        }

        [Test]
        public void Cone_ignora_a_altura_do_alvo()
        {
            SignArea cone = Cone(6f, 90f);

            Assert.IsTrue(cone.IsWithinAngle(Vector3.zero, Vector3.forward, new Vector3(0f, 5f, 2f)),
                "Um alvo a frente e acima continua a frente. A altura nao entra na abertura.");
        }

        [Test]
        public void Raio_aceita_quem_esta_pelas_costas()
        {
            var raio = new SignArea { shape = SignAreaShape.Radius, range = 4f };

            Assert.IsTrue(raio.IsWithinAngle(Vector3.zero, Vector3.forward, Vector3.back * 2f));
        }

        [Test]
        public void Alvo_colado_em_quem_conjura_esta_dentro_do_cone()
        {
            SignArea cone = Cone(6f, 30f);

            Assert.IsTrue(cone.IsWithinAngle(Vector3.zero, Vector3.forward, new Vector3(0.001f, 1f, -0.001f)),
                "Sem direcao, o inimigo mais perto seria justamente o que o sinal erra.");
        }

        // ------------------------------------------------------------ dado da habilidade

        [Test]
        public void Habilidade_sem_area_e_sem_efeito_esta_pronta()
        {
            _sinal.CollectProblems(_problemas);

            Assert.IsEmpty(_problemas, "Sem area e sem efeito e o sinal da tarefa 1.32: cobra e nao faz nada.");
            Assert.IsFalse(_sinal.area.HasArea);
        }

        [Test]
        public void Habilidade_com_area_e_efeito_esta_pronta()
        {
            _sinal.area = Cone(6f, 90f);
            _sinal.effects = new SignEffectDef[] { CriarEfeito() };

            _sinal.CollectProblems(_problemas);

            Assert.IsEmpty(_problemas, string.Join("; ", _problemas));
        }

        [Test]
        public void Cone_de_abertura_zero_e_problema()
        {
            _sinal.area = Cone(6f, 0f);

            _sinal.CollectProblems(_problemas);

            Assert.AreEqual(1, _problemas.Count);
            StringAssert.Contains("abertura zero", _problemas[0]);
        }

        [Test]
        public void Efeito_sem_area_e_problema()
        {
            _sinal.effects = new SignEffectDef[] { CriarEfeito() };

            _sinal.CollectProblems(_problemas);

            Assert.AreEqual(1, _problemas.Count);
            StringAssert.Contains("nao tem area", _problemas[0]);
        }

        [Test]
        public void Vaga_de_efeito_vazia_e_problema()
        {
            _sinal.area = Cone(6f, 90f);
            _sinal.effects = new SignEffectDef[] { CriarEfeito(), null };

            _sinal.CollectProblems(_problemas);

            Assert.AreEqual(1, _problemas.Count);
            StringAssert.Contains("efeito 1 esta vazio", _problemas[0]);
        }

        [Test]
        public void Problema_do_efeito_aparece_na_habilidade_com_o_nome_dele()
        {
            _sinal.area = Cone(6f, 90f);
            _sinal.effects = new SignEffectDef[] { CriarEfeito("dura zero segundos") };

            _sinal.CollectProblems(_problemas);

            Assert.AreEqual(1, _problemas.Count);
            StringAssert.Contains("Efeito_Mudo", _problemas[0]);
            StringAssert.Contains("dura zero segundos", _problemas[0]);
        }

        // --------------------------------------------------------------- intensidade

        static StatSheet Folha(float inteligencia, float intensidade)
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.Intelligence, inteligencia);
            sheet.SetBase(StatType.SignIntensity, intensidade);

            return sheet;
        }

        [Test]
        public void Bruxo_de_referencia_conjura_com_os_numeros_do_asset()
        {
            Assert.AreEqual(1f, _tuning.SignIntensity(Folha(10f, 1f)), 0.0001f,
                "Inteligencia 10 e multiplicador neutro: o sinal sai exatamente como o asset diz.");
        }

        [Test]
        public void Dobro_de_inteligencia_dobra_a_intensidade()
        {
            Assert.AreEqual(2f, _tuning.SignIntensity(Folha(20f, 1f)), 0.0001f,
                "E a Inteligencia que o docs/02 secao 5 poe na intensidade, e o vies do Grifo.");
        }

        [Test]
        public void Sinal_reforcado_multiplica_por_cima_da_inteligencia()
        {
            StatSheet sheet = Folha(15f, 1f);
            sheet.AddModifier(StatModifier.PercentMult(StatType.SignIntensity, 2f, this));

            Assert.AreEqual(3f, _tuning.SignIntensity(sheet), 0.0001f,
                "O sinal reforcado da adrenalina dobra a intensidade (docs/03 secao 7).");
        }

        [Test]
        public void Bloco_sem_multiplicador_conjura_com_intensidade_zero()
        {
            Assert.AreEqual(0f, _tuning.SignIntensity(Folha(10f, 0f)), 0.0001f,
                "E o defeito silencioso que o SchoolAssetsTests existe para pegar nos assets de verdade.");
        }

        [Test]
        public void Atributo_negativo_nao_inverte_o_sinal()
        {
            Assert.AreEqual(0f, _tuning.SignIntensity(Folha(10f, -1f)), 0.0001f,
                "Potencia negativa curaria quem o fogo deveria queimar.");
        }

        [Test]
        public void Folha_nula_conjura_com_intensidade_zero()
        {
            Assert.AreEqual(0f, _tuning.SignIntensity(null));
        }

        [Test]
        public void Escala_multiplica_a_potencia_pela_intensidade()
        {
            var cast = new SignCast(null, _sinal, Vector3.zero, Vector3.forward, 1.5f);

            Assert.AreEqual(2.25f, cast.Scale(1.5f), 0.0001f);
        }
    }
}
