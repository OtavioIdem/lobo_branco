using UnityEngine;

namespace LoboBranco.CameraSystem
{
    /// <summary>
    /// Autoridade: nenhuma. E camera, e camera e local por natureza: cada jogador sente o
    /// proprio golpe na propria tela, e so nela.
    ///
    /// O soco de camera do docs/03 secao 11: um deslocamento de alguns graus no instante do
    /// impacto, que volta ao lugar desacelerando.
    ///
    /// Ele e separado do tremor do Cinemachine Impulse de proposito. O tremor diz
    /// <em>quanto</em> doeu e oscila; o soco diz <em>que</em> conectou e tem uma direcao so.
    /// Juntos num ruido so, o jogador perde a leitura do impacto no meio da vibracao.
    ///
    /// O deslocamento nao entra no pitch que o jogador controla. Ele e somado na hora de
    /// escrever a rotacao, e por isso nunca acumula: dez golpes seguidos nao deixam a camera
    /// vinte graus mais baixa.
    ///
    /// Classe pura, com o tempo por parametro, testavel em EditMode sem camera nenhuma.
    /// </summary>
    public sealed class CameraPunch
    {
        float _amplitude;
        float _recovery;
        float _elapsed;

        /// <summary>Deslocamento atual em graus. Positivo abaixa a camera, na direcao do golpe.</summary>
        public float Offset { get; private set; }

        /// <summary>
        /// Da o soco. Um soco menor que o deslocamento que ainda sobra e ignorado: um golpe
        /// leve logo depois de um forte nao pode encolher a sensacao do forte.
        /// </summary>
        public void Kick(float degrees, float recoverySeconds)
        {
            if (degrees <= 0f || degrees < Offset) return;

            _amplitude = degrees;
            _recovery = Mathf.Max(0f, recoverySeconds);
            _elapsed = 0f;
            Offset = degrees;
        }

        /// <summary>
        /// Volta ao lugar. A curva e quadratica e nao linear: a camera sai rapido do ponto
        /// mais baixo e chega devagar, que e como um impacto fisico se desfaz. A linear
        /// termina com um tranco no ultimo quadro.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (Offset <= 0f) return;

            _elapsed += deltaTime;

            if (_recovery <= 0f || _elapsed >= _recovery)
            {
                Offset = 0f;
                _amplitude = 0f;
                return;
            }

            float remaining = 1f - _elapsed / _recovery;
            Offset = _amplitude * remaining * remaining;
        }
    }
}
