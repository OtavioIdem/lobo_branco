using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LoboBranco.Combat;
using LoboBranco.Stats;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O controle do abridor aplicado numa criatura de verdade (tarefa 1.18c): vida, perfil e
    /// <see cref="ControlStatus"/>, sem rede, que e o caminho em que o proprio objeto resolve.
    ///
    /// Em PlayMode porque a folha de atributos e o controle nascem no <c>Awake</c>. As duracoes sao
    /// lidas logo depois de aplicar, contra o relogio que o componente usa, e nenhum teste espera o
    /// controle acabar.
    /// </summary>
    public sealed class ControlEffectTests
    {
        const float Folga = 0.05f;

        static readonly FieldInfo ProfileField =
            typeof(DamageReceiver).GetField("profile", BindingFlags.Instance | BindingFlags.NonPublic);

        ControlEffectDef _efeito;
        AbilityDef _sinal;
        readonly List<Object> _criados = new List<Object>();

        // Guardada em vez de buscada: uma busca na cena pode achar o objeto de outro teste.
        GameObject _ultimaCriada;

        [SetUp]
        public void SetUp()
        {
            _efeito = ScriptableObject.CreateInstance<ControlEffectDef>();
            _sinal = ScriptableObject.CreateInstance<AbilityDef>();
            _sinal.area = new SignArea { shape = SignAreaShape.Cone, range = 6f, coneAngleDegrees = 90f };
            _sinal.effects = new SignEffectDef[] { _efeito };

            _criados.Add(_efeito);
            _criados.Add(_sinal);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _criados.Count; i++)
                if (_criados[i] != null)
                    Object.DestroyImmediate(_criados[i]);

            _criados.Clear();
        }

        /// <summary>Uma criatura com vida, perfil de porte e controle. Sem perfil, o receptor diz medio.</summary>
        ControlStatus CriarCriatura(BodyWeight? porte, Vector3 pes = default, bool comControle = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = porte.HasValue ? $"Criatura_{porte}" : "Criatura_sem_perfil";
            go.transform.position = pes + Vector3.up;
            _criados.Add(go);
            _ultimaCriada = go;

            var vitals = go.AddComponent<CharacterVitals>();
            vitals.Stats.SetBase(StatType.MaxVitality, 50f);
            vitals.RestoreToFull();

            var receiver = go.AddComponent<DamageReceiver>();

            if (porte.HasValue)
            {
                var perfil = ScriptableObject.CreateInstance<MonsterDef>();
                perfil.bodyWeight = porte.Value;
                _criados.Add(perfil);
                ProfileField.SetValue(receiver, perfil);
            }

            Physics.SyncTransforms();
            return comControle ? go.AddComponent<ControlStatus>() : null;
        }

        void Aplicar(Component alvo, float intensidade = 1f)
        {
            var cast = new SignCast(null, _sinal, Vector3.zero, Vector3.forward, intensidade);
            _efeito.Apply(cast, alvo.GetComponent<CharacterVitals>());
        }

        [Test]
        public void O_perfil_privado_ainda_se_chama_profile()
        {
            Assert.IsNotNull(ProfileField,
                "Os testes daqui escrevem o perfil por reflexao. Se o campo mudou de nome, mude aqui tambem.");
        }

        [Test]
        public void Criatura_leve_e_derrubada_por_dois_segundos()
        {
            ControlStatus leve = CriarCriatura(BodyWeight.Light);

            Aplicar(leve);

            Assert.AreEqual(ControlKind.KnockedDown, leve.Incapacitation);
            Assert.AreEqual(2f, leve.IncapacitatedRemaining, Folga);
        }

        [Test]
        public void Criatura_media_e_atordoada_por_um_segundo_e_meio()
        {
            ControlStatus media = CriarCriatura(BodyWeight.Medium);

            Aplicar(media);

            Assert.AreEqual(ControlKind.Stunned, media.Incapacitation);
            Assert.AreEqual(1.5f, media.IncapacitatedRemaining, Folga, "docs/03 secao 8.");
        }

        [Test]
        public void Criatura_pesada_nao_perde_o_controle()
        {
            ControlStatus pesada = CriarCriatura(BodyWeight.Heavy);

            Aplicar(pesada);

            Assert.IsFalse(pesada.IsIncapacitated, "docs/03 secao 11: pesados nem se movem.");
        }

        [Test]
        public void Criatura_sem_perfil_conta_como_media()
        {
            ControlStatus semPerfil = CriarCriatura(null);

            Aplicar(semPerfil);

            Assert.AreEqual(ControlKind.Stunned, semPerfil.Incapacitation,
                "Sem porte declarado, o meio da tabela: atordoa e nunca derruba.");
        }

        [Test]
        public void Intensidade_alonga_o_controle_e_nao_troca_o_tipo()
        {
            ControlStatus media = CriarCriatura(BodyWeight.Medium);

            Aplicar(media, intensidade: 2f);

            Assert.AreEqual(ControlKind.Stunned, media.Incapacitation,
                "Um sinal intenso segura mais tempo, e nunca derruba o que o documento diz que so atordoa.");
            Assert.AreEqual(3f, media.IncapacitatedRemaining, Folga);
        }

        [Test]
        public void Criatura_sem_controle_ignora_o_efeito_sem_erro()
        {
            CriarCriatura(BodyWeight.Light, comControle: false);
            GameObject alvo = _ultimaCriada;

            Assert.DoesNotThrow(() => Aplicar(alvo.transform),
                "E o alvo parado da sandbox: ele pisca e levanta, e nao tem controle a perder.");
        }

        [UnityTest]
        public IEnumerator Abridor_derruba_a_matilha_no_cone_e_poupa_quem_esta_atras()
        {
            ControlStatus frente = CriarCriatura(BodyWeight.Light, new Vector3(-1f, 0f, 3f));
            ControlStatus flanco = CriarCriatura(BodyWeight.Light, new Vector3(1.5f, 0f, 2.5f));
            ControlStatus atras = CriarCriatura(BodyWeight.Light, new Vector3(0f, 0f, -3f));
            yield return null;

            var resolver = new SignResolver();
            var cast = new SignCast(null, _sinal, Vector3.zero, Vector3.forward, 1f);

            Assert.AreEqual(2, resolver.Resolve(cast, null, Physics.AllLayers));
            Assert.AreEqual(ControlKind.KnockedDown, frente.Incapacitation);
            Assert.AreEqual(ControlKind.KnockedDown, flanco.Incapacitation);
            Assert.IsFalse(atras.IsIncapacitated, "O cone e a frente de quem conjura.");
        }
    }
}
