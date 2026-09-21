using LoboBranco.Combat;
using NUnit.Framework;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O escudo como regra pura (tarefa 1.18e): absorve um golpe por 8 s e devolve 30% ao quebrar.
    /// O relogio e passado a mao, entao a expiracao e verificada sem esperar oito segundos.
    ///
    /// O defeito que importa e o do escudo que nao quebra: um escudo que absorvesse o segundo golpe
    /// viraria vida infinita, e nada no Console diria isso.
    /// </summary>
    public sealed class WardStateTests
    {
        WardState _escudo;

        [SetUp]
        public void SetUp() => _escudo = default;

        [Test]
        public void Escudo_do_documento_fica_de_pe_por_oito_segundos()
        {
            Assert.IsTrue(_escudo.Raise(0d, 8f, 0.3f));

            Assert.IsTrue(_escudo.IsUp(7.9d));
            Assert.IsFalse(_escudo.IsUp(8d));
            Assert.AreEqual(3f, _escudo.RemainingAt(5d), 0.001f);
        }

        [Test]
        public void Quebrar_devolve_trinta_por_cento_do_que_absorveu()
        {
            _escudo.Raise(0d, 8f, 0.3f);

            Assert.IsTrue(_escudo.TryBreak(1d, 40f, out float devolvido));
            Assert.AreEqual(12f, devolvido, 0.001f);
        }

        [Test]
        public void Escudo_quebra_no_primeiro_golpe_e_nao_absorve_o_segundo()
        {
            _escudo.Raise(0d, 8f, 0.3f);
            _escudo.TryBreak(1d, 10f, out _);

            Assert.IsFalse(_escudo.IsUp(2d));
            Assert.IsFalse(_escudo.TryBreak(2d, 10f, out float devolvido),
                "Absorver o segundo golpe faria do escudo vida infinita.");
            Assert.AreEqual(0f, devolvido);
        }

        [Test]
        public void Golpe_grande_e_golpe_pequeno_custam_o_mesmo_escudo()
        {
            _escudo.Raise(0d, 8f, 0.3f);
            Assert.IsTrue(_escudo.TryBreak(1d, 80f, out float devolvido));

            Assert.AreEqual(24f, devolvido, 0.001f,
                "docs/03 secao 8: absorve 1 golpe, e nao uma quantidade de dano.");
        }

        [Test]
        public void Escudo_caido_nao_absorve_nada()
        {
            _escudo.Raise(0d, 8f, 0.3f);

            Assert.IsFalse(_escudo.TryBreak(8d, 10f, out _));
        }

        [Test]
        public void Erguer_de_novo_substitui_e_nao_soma()
        {
            _escudo.Raise(0d, 8f, 0.3f);
            _escudo.Raise(4d, 8f, 0.3f);

            Assert.IsTrue(_escudo.TryBreak(5d, 10f, out _));
            Assert.IsFalse(_escudo.IsUp(5d), "Dois escudos nao absorvem dois golpes.");
            Assert.IsTrue(_escudo.IsUp(11d) == false);
        }

        [Test]
        public void Escudo_mais_fraco_e_mais_curto_nao_substitui_o_que_esta_de_pe()
        {
            _escudo.Raise(0d, 8f, 0.6f);

            Assert.IsFalse(_escudo.Raise(1d, 2f, 0.3f), "Nao vale a mensagem de rede.");
            Assert.AreEqual(0.6f, _escudo.ReflectFraction);
        }

        [Test]
        public void Escudo_mais_forte_substitui_o_troco()
        {
            _escudo.Raise(0d, 8f, 0.3f);

            Assert.IsTrue(_escudo.Raise(1d, 8f, 0.6f));
            _escudo.TryBreak(2d, 10f, out float devolvido);

            Assert.AreEqual(6f, devolvido, 0.001f);
        }

        [Test]
        public void Escudo_de_duracao_zero_nao_sobe()
        {
            Assert.IsFalse(_escudo.Raise(0d, 0f, 0.3f));
            Assert.IsFalse(_escudo.IsUp(0d));
        }

        [Test]
        public void Trocar_de_relogio_preserva_quanto_falta()
        {
            _escudo.Raise(0d, 8f, 0.3f);
            _escudo.Rebase(1000d);

            Assert.AreEqual(8f, _escudo.RemainingAt(1000d), 0.001f);
        }

        [Test]
        public void Derrubar_encerra_o_escudo()
        {
            _escudo.Raise(0d, 8f, 0.3f);
            _escudo.Clear();

            Assert.IsFalse(_escudo.IsUp(1d));
        }
    }
}
