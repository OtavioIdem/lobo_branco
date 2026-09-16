using System.Collections.Generic;
using LoboBranco.Combat;
using LoboBranco.Stats;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O escudo numa criatura de verdade (tarefa 1.18e): o golpe chega pelo
    /// <see cref="DamageReceiver"/>, o escudo entra na frente da vida, quebra, e devolve os 30% para
    /// quem bateu. Sem rede, o proprio objeto resolve.
    ///
    /// A expiracao por tempo nao esta aqui: ela e do <c>WardStateTests</c>, com o relogio na mao.
    /// Esperar oito segundos num teste e a receita de teste intermitente.
    /// </summary>
    public sealed class WardTests
    {
        WardEffectDef _efeito;
        AbilityDef _sinal;
        CharacterVitals _bruxo;
        WardStatus _escudo;
        AtacanteFalso _inimigo;
        CharacterVitals _vidaDoInimigo;

        readonly List<Object> _criados = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _efeito = ScriptableObject.CreateInstance<WardEffectDef>();
            _sinal = ScriptableObject.CreateInstance<AbilityDef>();
            _sinal.area = new SignArea { shape = SignAreaShape.Self };
            _sinal.effects = new SignEffectDef[] { _efeito };
            _criados.Add(_efeito);
            _criados.Add(_sinal);

            var bruxo = new GameObject("Bruxo");
            _criados.Add(bruxo);
            _bruxo = bruxo.AddComponent<CharacterVitals>();
            _bruxo.Stats.SetBase(StatType.MaxVitality, 100f);
            _bruxo.RestoreToFull();
            bruxo.AddComponent<DamageReceiver>();
            _escudo = bruxo.AddComponent<WardStatus>();

            var inimigo = new GameObject("Inimigo");
            _criados.Add(inimigo);
            _vidaDoInimigo = inimigo.AddComponent<CharacterVitals>();
            _vidaDoInimigo.Stats.SetBase(StatType.MaxVitality, 55f);
            _vidaDoInimigo.RestoreToFull();
            inimigo.AddComponent<DamageReceiver>();
            _inimigo = inimigo.AddComponent<AtacanteFalso>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _criados.Count; i++)
                if (_criados[i] != null)
                    Object.DestroyImmediate(_criados[i]);

            _criados.Clear();
        }

        void Conjurar(float intensidade = 1f)
            => _efeito.Apply(new SignCast(_bruxo, _sinal, Vector3.zero, Vector3.forward, intensidade), _bruxo);

        /// <summary>Um golpe chegando pela porta de sempre, com autor.</summary>
        void Golpear(float dano, bool comAutor = true)
            => _bruxo.GetComponent<DamageReceiver>().ApplyDamage(
                new DamageResult(dano, DamageType.Slash, false, 1f, comAutor ? _inimigo : null));

        [Test]
        public void Sinal_ergue_o_escudo_de_quem_conjurou()
        {
            Conjurar();

            Assert.IsTrue(_escudo.IsUp);
            Assert.AreEqual(0.3f, _escudo.ReflectFraction, 0.001f, "docs/03 secao 8: devolve 30%.");
        }

        [Test]
        public void Escudo_engole_o_golpe_inteiro()
        {
            Conjurar();

            Golpear(40f);

            Assert.AreEqual(100f, _bruxo.CurrentVitality, "Absorve 1 golpe, seja ele de 8 ou de 80.");
            Assert.IsFalse(_escudo.IsUp, "E quebra nele.");
        }

        [Test]
        public void Escudo_devolve_trinta_por_cento_a_quem_bateu()
        {
            Conjurar();

            Golpear(40f);

            Assert.AreEqual(55f - 12f, _vidaDoInimigo.CurrentVitality, 0.001f);
        }

        [Test]
        public void Segundo_golpe_passa_pelo_escudo_quebrado()
        {
            Conjurar();
            Golpear(40f);

            Golpear(10f);

            Assert.AreEqual(90f, _bruxo.CurrentVitality, 0.001f);
            Assert.AreEqual(55f - 12f, _vidaDoInimigo.CurrentVitality, 0.001f, "So o golpe absorvido rende troco.");
        }

        [Test]
        public void Sem_escudo_o_golpe_tira_vida_como_sempre()
        {
            Golpear(10f);

            Assert.AreEqual(90f, _bruxo.CurrentVitality, 0.001f);
        }

        [Test]
        public void Intensidade_escala_o_troco_e_nao_a_absorcao()
        {
            Conjurar(intensidade: 2f);

            Golpear(40f);

            Assert.AreEqual(100f, _bruxo.CurrentVitality, "O escudo ja absorvia o golpe inteiro.");
            Assert.AreEqual(55f - 24f, _vidaDoInimigo.CurrentVitality, 0.001f);
        }

        [Test]
        public void Dano_sem_autor_e_absorvido_sem_devolver_nada()
        {
            Conjurar();

            Assert.DoesNotThrow(() => Golpear(40f, comAutor: false));

            Assert.AreEqual(100f, _bruxo.CurrentVitality);
            Assert.AreEqual(55f, _vidaDoInimigo.CurrentVitality, "Nao ha para quem devolver.");
        }

        [Test]
        public void Morrer_derruba_o_escudo()
        {
            Conjurar();

            // Direto na vida, e nao pela porta do dano: com o escudo de pe, o golpe seria absorvido
            // e o bruxo nao morreria.
            _bruxo.ApplyDamage(200f);

            Assert.IsTrue(_bruxo.IsDown);
            Assert.IsFalse(_escudo.IsUp, "Levantar com o escudo de antes seria vida de graca.");
        }

        [Test]
        public void Criatura_abatida_nao_ergue_escudo()
        {
            _bruxo.ApplyDamage(200f);

            Conjurar();

            Assert.IsFalse(_escudo.IsUp);
        }

        [Test]
        public void Criatura_sem_escudo_ignora_o_sinal_sem_erro()
        {
            var go = new GameObject("Sem_Escudo");
            _criados.Add(go);
            var vitals = go.AddComponent<CharacterVitals>();

            Assert.DoesNotThrow(() =>
                _efeito.Apply(new SignCast(_bruxo, _sinal, Vector3.zero, Vector3.forward, 1f), vitals));
        }
    }
}
