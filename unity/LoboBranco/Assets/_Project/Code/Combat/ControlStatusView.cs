using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Autoridade: nenhuma. Cada maquina desenha o controle que le do <see cref="ControlStatus"/>,
    /// que ja chega replicado.
    ///
    /// Em greybox nao ha animacao de cair nem de cambalear (M4), entao a pose e do corpo: a
    /// capsula derrubada deita, e a atordoada balanca. Mexe so na rotacao e na altura do corpo, e
    /// nunca na cor nem na escala, que sao do telegrafo do golpe. Uma criatura sem controle nunca
    /// esta golpeando, entao os dois nao disputam o mesmo corpo no mesmo quadro.
    ///
    /// Parado nao custa nada: sem controle e ja de volta a pose de repouso, o <c>Update</c>
    /// devolve na primeira linha.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ControlStatus))]
    public sealed class ControlStatusView : MonoBehaviour
    {
        [Tooltip("O que deita e balanca. Nao pode ser a raiz: a raiz tem colisor e posicao em rede.")]
        [SerializeField] Transform body;

        [Tooltip("Graus de balanco do corpo atordoado.")]
        [SerializeField] float stunSwayDegrees = 12f;

        [Tooltip("Balancos por segundo.")]
        [SerializeField] float stunSwayFrequency = 2.5f;

        // Nao sao balanceamento: sao pose de greybox, e somem quando a animacao entrar.
        const float LyingDegrees = 80f;

        ControlStatus _status;
        Quaternion _restRotation;
        Vector3 _restPosition;
        bool _posed;

        void Awake()
        {
            _status = GetComponent<ControlStatus>();

            if (body == null) return;

            _restRotation = body.localRotation;
            _restPosition = body.localPosition;
        }

        void Update()
        {
            if (body == null) return;

            ControlKind kind = _status.Incapacitation;

            if (kind == ControlKind.None)
            {
                if (_posed) Rest();
                return;
            }

            _posed = true;

            if (kind == ControlKind.KnockedDown)
            {
                // Deitada pelo centro, a capsula afundaria metade no chao. Ela desce ate o raio,
                // que e a altura do centro de uma capsula deitada.
                body.localRotation = _restRotation * Quaternion.Euler(0f, 0f, LyingDegrees);
                body.localPosition = new Vector3(_restPosition.x, body.localScale.x * 0.5f, _restPosition.z);
                return;
            }

            float sway = Mathf.Sin(Time.time * stunSwayFrequency * Mathf.PI * 2f) * stunSwayDegrees;
            body.localRotation = _restRotation * Quaternion.Euler(sway, 0f, sway * 0.5f);
            body.localPosition = _restPosition;
        }

        void Rest()
        {
            _posed = false;
            body.localRotation = _restRotation;
            body.localPosition = _restPosition;
        }
    }
}
