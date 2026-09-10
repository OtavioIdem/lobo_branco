using LoboBranco.Stats;
using NUnit.Framework;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A folha de atributos e onde pocao, talento, mutagenio e equipamento se encontram.
    /// Um erro na ordem das operacoes aqui e invisivel: o numero final parece plausivel e
    /// o balanceamento inteiro fica errado sem ninguem perceber. Por isso ela e testada.
    /// </summary>
    public sealed class StatSheetTests
    {
        StatSheet _sheet;

        // Origens fictícias. Importa serem objetos distintos, nao o que eles sao.
        readonly object _potion = new object();
        readonly object _talent = new object();
        readonly object _armorPiece = new object();

        [SetUp]
        public void SetUp()
        {
            _sheet = new StatSheet();
            _sheet.SetBase(StatType.AttackDamage, 100f);
        }

        // ------------------------------------------------------------- basico

        [Test]
        public void Sem_modificador_devolve_o_valor_base()
        {
            Assert.AreEqual(100f, _sheet.Get(StatType.AttackDamage), 0.001f);
        }

        [Test]
        public void Atributo_nunca_definido_vale_zero()
        {
            Assert.AreEqual(0f, _sheet.Get(StatType.SignIntensity), 0.001f);
        }

        [Test]
        public void Flat_soma_ao_base()
        {
            _sheet.AddModifier(StatModifier.Flat(StatType.AttackDamage, 25f, _potion));

            Assert.AreEqual(125f, _sheet.Get(StatType.AttackDamage), 0.001f);
        }

        // ---------------------------------------------------- ordem das operacoes

        [Test]
        public void PercentAdd_soma_entre_si_antes_de_multiplicar()
        {
            _sheet.AddModifier(StatModifier.PercentAdd(StatType.AttackDamage, 0.30f, _potion));
            _sheet.AddModifier(StatModifier.PercentAdd(StatType.AttackDamage, 0.30f, _talent));

            // 100 * (1 + 0,60) = 160. Se fosse composto daria 169, e a diferenca cresce
            // com o numero de buffs ate o balanceamento perder o sentido.
            Assert.AreEqual(160f, _sheet.Get(StatType.AttackDamage), 0.001f);
        }

        [Test]
        public void PercentMult_compoe_um_com_o_outro()
        {
            _sheet.AddModifier(StatModifier.PercentMult(StatType.AttackDamage, 1.20f, _potion));
            _sheet.AddModifier(StatModifier.PercentMult(StatType.AttackDamage, 1.20f, _talent));

            Assert.AreEqual(144f, _sheet.Get(StatType.AttackDamage), 0.001f);
        }

        [Test]
        public void A_formula_e_base_mais_flat_vezes_percentAdd_vezes_percentMult()
        {
            _sheet.AddModifier(StatModifier.Flat(StatType.AttackDamage, 20f, _armorPiece));
            _sheet.AddModifier(StatModifier.PercentAdd(StatType.AttackDamage, 0.50f, _potion));
            _sheet.AddModifier(StatModifier.PercentMult(StatType.AttackDamage, 2.0f, _talent));

            // (100 + 20) * 1,50 * 2,0 = 360
            Assert.AreEqual(360f, _sheet.Get(StatType.AttackDamage), 0.001f);
        }

        [Test]
        public void A_ordem_de_chegada_nao_muda_o_resultado()
        {
            _sheet.AddModifier(StatModifier.PercentMult(StatType.AttackDamage, 1.5f, _talent));
            _sheet.AddModifier(StatModifier.Flat(StatType.AttackDamage, 10f, _armorPiece));
            _sheet.AddModifier(StatModifier.PercentAdd(StatType.AttackDamage, 0.20f, _potion));
            float first = _sheet.Get(StatType.AttackDamage);

            var other = new StatSheet();
            other.SetBase(StatType.AttackDamage, 100f);
            other.AddModifier(StatModifier.PercentAdd(StatType.AttackDamage, 0.20f, _potion));
            other.AddModifier(StatModifier.PercentMult(StatType.AttackDamage, 1.5f, _talent));
            other.AddModifier(StatModifier.Flat(StatType.AttackDamage, 10f, _armorPiece));

            // Beber duas pocoes em ordem diferente tem que dar o mesmo numero.
            Assert.AreEqual(first, other.Get(StatType.AttackDamage), 0.001f);
        }

        // ---------------------------------------------------------- remocao

        [Test]
        public void RemoveAllFromSource_tira_so_o_que_veio_daquela_origem()
        {
            _sheet.AddModifier(StatModifier.Flat(StatType.AttackDamage, 50f, _potion));
            _sheet.AddModifier(StatModifier.Flat(StatType.AttackDamage, 30f, _talent));

            int removed = _sheet.RemoveAllFromSource(_potion);

            Assert.AreEqual(1, removed);
            Assert.AreEqual(130f, _sheet.Get(StatType.AttackDamage), 0.001f,
                "O bonus do talento deveria ter sobrevivido.");
        }

        [Test]
        public void Uma_origem_com_varios_modificadores_sai_inteira()
        {
            _sheet.AddModifier(StatModifier.Flat(StatType.AttackDamage, 50f, _potion));
            _sheet.AddModifier(StatModifier.PercentAdd(StatType.AttackDamage, 0.5f, _potion));
            _sheet.AddModifier(StatModifier.Flat(StatType.MaxVitality, 40f, _potion));

            int removed = _sheet.RemoveAllFromSource(_potion);

            Assert.AreEqual(3, removed);
            Assert.AreEqual(100f, _sheet.Get(StatType.AttackDamage), 0.001f);
            Assert.AreEqual(0f, _sheet.Get(StatType.MaxVitality), 0.001f);
        }

        [Test]
        public void Pocao_que_vence_nao_deixa_bonus_para_tras()
        {
            // O bug classico do sistema de buffs, e a razao de StatModifier carregar Source.
            _sheet.AddModifier(StatModifier.PercentAdd(StatType.AttackDamage, 0.30f, _potion));
            Assert.AreEqual(130f, _sheet.Get(StatType.AttackDamage), 0.001f);

            _sheet.RemoveAllFromSource(_potion);

            Assert.AreEqual(100f, _sheet.Get(StatType.AttackDamage), 0.001f);
            Assert.IsFalse(_sheet.HasSource(_potion));
        }

        [Test]
        public void RemoveModifier_tira_uma_ocorrencia_so()
        {
            var mod = StatModifier.Flat(StatType.AttackDamage, 10f, _potion);
            _sheet.AddModifier(mod);
            _sheet.AddModifier(mod);

            Assert.IsTrue(_sheet.RemoveModifier(mod));

            Assert.AreEqual(110f, _sheet.Get(StatType.AttackDamage), 0.001f);
            Assert.AreEqual(1, _sheet.ModifierCount);
        }

        [Test]
        public void Remover_origem_inexistente_nao_faz_nada()
        {
            _sheet.AddModifier(StatModifier.Flat(StatType.AttackDamage, 10f, _potion));

            Assert.AreEqual(0, _sheet.RemoveAllFromSource(_talent));
            Assert.AreEqual(0, _sheet.RemoveAllFromSource(null));
            Assert.AreEqual(110f, _sheet.Get(StatType.AttackDamage), 0.001f);
        }

        // -------------------------------------------------------------- cache

        [Test]
        public void Mudar_o_base_invalida_o_valor_ja_lido()
        {
            Assert.AreEqual(100f, _sheet.Get(StatType.AttackDamage), 0.001f);

            _sheet.SetBase(StatType.AttackDamage, 200f);

            Assert.AreEqual(200f, _sheet.Get(StatType.AttackDamage), 0.001f);
        }

        [Test]
        public void ClearModifiers_volta_para_os_valores_base()
        {
            _sheet.AddModifier(StatModifier.Flat(StatType.AttackDamage, 50f, _potion));
            _sheet.AddModifier(StatModifier.Flat(StatType.Armor, 5f, _armorPiece));
            _sheet.Get(StatType.AttackDamage);

            _sheet.ClearModifiers();

            Assert.AreEqual(100f, _sheet.Get(StatType.AttackDamage), 0.001f);
            Assert.AreEqual(0f, _sheet.Get(StatType.Armor), 0.001f);
            Assert.AreEqual(0, _sheet.ModifierCount);
        }

        // -------------------------------------------------------------- evento

        [Test]
        public void StatChanged_avisa_qual_atributo_mudou()
        {
            StatType? changed = null;
            _sheet.StatChanged += stat => changed = stat;

            _sheet.AddModifier(StatModifier.Flat(StatType.SignIntensity, 5f, _potion));

            Assert.AreEqual(StatType.SignIntensity, changed,
                "A UI depende deste evento para nao fazer polling.");
        }

        [Test]
        public void StatChanged_dispara_tambem_ao_remover()
        {
            _sheet.AddModifier(StatModifier.Flat(StatType.Armor, 5f, _armorPiece));

            int calls = 0;
            _sheet.StatChanged += _ => calls++;

            _sheet.RemoveAllFromSource(_armorPiece);

            Assert.AreEqual(1, calls);
        }
    }
}
