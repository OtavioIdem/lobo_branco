using LoboBranco.Player;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O buffer de input de 0,2 s do docs/07 secao 4.4.
    ///
    /// Vale testar porque ele falha em silencio: um buffer que expira cedo demais nao
    /// lanca erro nem aparece no Console. Ele so faz o combate parecer que ignora o
    /// clique, e o sintoma e indistinguivel de "os numeros estao ruins". Aqui o tempo
    /// entra por parametro, entao a janela e verificada em passos exatos.
    /// </summary>
    public sealed class InputBufferTests
    {
        InputBuffer _buffer;

        const float Window = 0.2f;
        const float Step = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _buffer = new InputBuffer { Window = Window };
        }

        void Avancar(float segundos)
        {
            int passos = Mathf.RoundToInt(segundos / Step);
            for (int i = 0; i < passos; i++)
                _buffer.Tick(Step);
        }

        // ------------------------------------------------------------ guardar

        [Test]
        public void Acao_apertada_fica_pendente_no_mesmo_instante()
        {
            _buffer.Push(BufferedAction.AttackLight);

            Assert.AreEqual(BufferedAction.AttackLight, _buffer.Pending);
            Assert.IsTrue(_buffer.HasPending);
        }

        [Test]
        public void Buffer_recem_criado_nao_tem_nada_pendente()
        {
            Assert.AreEqual(BufferedAction.None, _buffer.Pending);
            Assert.IsFalse(_buffer.HasPending);
        }

        [Test]
        public void Push_de_None_limpa_em_vez_de_guardar()
        {
            _buffer.Push(BufferedAction.AttackHeavy);
            _buffer.Push(BufferedAction.None);

            Assert.AreEqual(BufferedAction.None, _buffer.Pending);
        }

        // ------------------------------------------------------------ expirar

        [Test]
        public void Acao_continua_valida_logo_antes_do_fim_da_janela()
        {
            _buffer.Push(BufferedAction.AttackLight);
            _buffer.Tick(Window - 0.01f);

            Assert.AreEqual(BufferedAction.AttackLight, _buffer.Pending,
                "Dentro da janela de 0,2 s o input ainda precisa valer.");
        }

        [Test]
        public void Acao_expira_ao_completar_a_janela()
        {
            _buffer.Push(BufferedAction.AttackLight);
            _buffer.Tick(Window);

            Assert.AreEqual(BufferedAction.None, _buffer.Pending);
        }

        [Test]
        public void Janela_expira_igual_somando_passos_fixos_ou_de_uma_vez()
        {
            _buffer.Push(BufferedAction.AttackLight);
            Avancar(0.25f);

            Assert.AreEqual(BufferedAction.None, _buffer.Pending,
                "Doze passos de 1/60 passam de 0,2 s e a acao precisa ter expirado.");
        }

        [Test]
        public void Tick_com_delta_zero_nao_envelhece_a_acao()
        {
            _buffer.Push(BufferedAction.Dodge);
            _buffer.Tick(0f);
            _buffer.Tick(0f);

            Assert.AreEqual(Window, _buffer.Remaining, 0.0001f);
        }

        [Test]
        public void Janela_maior_no_asset_faz_a_acao_durar_mais()
        {
            _buffer.Window = 0.35f;
            _buffer.Push(BufferedAction.AttackHeavy);
            _buffer.Tick(0.3f);

            Assert.AreEqual(BufferedAction.AttackHeavy, _buffer.Pending,
                "A janela vem do asset; mudar o asset precisa mudar o comportamento.");
        }

        // ---------------------------------------------------------- substituir

        [Test]
        public void Input_novo_substitui_o_anterior_em_vez_de_enfileirar()
        {
            _buffer.Push(BufferedAction.AttackLight);
            _buffer.Tick(0.1f);
            _buffer.Push(BufferedAction.Dodge);

            Assert.AreEqual(BufferedAction.Dodge, _buffer.Pending,
                "O ultimo input e o que o jogador quer agora, e por isso ele vence.");
            Assert.AreEqual(Window, _buffer.Remaining, 0.0001f,
                "Substituir tambem renova a janela inteira.");
        }

        // ------------------------------------------------------------ consumir

        [Test]
        public void TryConsume_da_acao_certa_consome_e_devolve_verdadeiro()
        {
            _buffer.Push(BufferedAction.AttackLight);

            Assert.IsTrue(_buffer.TryConsume(BufferedAction.AttackLight));
            Assert.AreEqual(BufferedAction.None, _buffer.Pending);
        }

        [Test]
        public void TryConsume_da_acao_errada_nao_consome_nada()
        {
            _buffer.Push(BufferedAction.AttackLight);

            Assert.IsFalse(_buffer.TryConsume(BufferedAction.AttackHeavy));
            Assert.AreEqual(BufferedAction.AttackLight, _buffer.Pending,
                "Uma tentativa recusada nao pode engolir o input de outro estado.");
        }

        [Test]
        public void TryConsume_depois_de_expirar_falha()
        {
            _buffer.Push(BufferedAction.AttackLight);
            _buffer.Tick(Window);

            Assert.IsFalse(_buffer.TryConsume(BufferedAction.AttackLight));
        }

        [Test]
        public void ConsumeAny_devolve_o_que_estava_guardado_e_esvazia()
        {
            _buffer.Push(BufferedAction.CastSign);

            Assert.AreEqual(BufferedAction.CastSign, _buffer.ConsumeAny());
            Assert.AreEqual(BufferedAction.None, _buffer.Pending);
        }

        [Test]
        public void Clear_esquece_a_acao_guardada()
        {
            _buffer.Push(BufferedAction.Interact);
            _buffer.Clear();

            Assert.AreEqual(BufferedAction.None, _buffer.Pending);
            Assert.AreEqual(0f, _buffer.Remaining, 0.0001f);
        }
    }
}
