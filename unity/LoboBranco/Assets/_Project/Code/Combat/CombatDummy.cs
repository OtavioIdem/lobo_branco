using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Autoridade: o host, por meio do <see cref="CharacterVitals"/> ao lado. Quem tira
    /// vida e quem conta o tempo de levantar e so ele; piscar, tombar e ficar de pe de
    /// novo sao reacao local ao numero que chegou replicado.
    ///
    /// Alvo greybox da cena de sandbox: uma capsula que mostra que recebeu golpe e volta
    /// a ficar de pe depois de um tempo, para nao ter que reiniciar a cena a cada teste de
    /// balanceamento.
    ///
    /// Ele nao e mais a porta do dano. Receber golpe e do <see cref="DamageReceiver"/>,
    /// que o bruxo tambem usa, e a especie e do <see cref="MonsterDef"/>. O que sobrou
    /// aqui e so o que e mesmo de sandbox, e e por isso que o inimigo de verdade da tarefa
    /// 1.21 nao carrega este componente: ele nao pisca, nao levanta sozinho, e a morte
    /// dele importa.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterVitals))]
    [RequireComponent(typeof(DamageReceiver))]
    public sealed class CombatDummy : MonoBehaviour
    {
        [Header("Sandbox")]
        [Tooltip("Segundos ate voltar de pe. Nao e balanceamento: e conveniencia de teste.")]
        [SerializeField] float reviveDelay = 4f;

        [Tooltip("Cor do piscar ao levar dano. Sem retorno visual e impossivel julgar o alcance.")]
        [SerializeField] Color hitFlashColor = Color.white;

        [SerializeField] float hitFlashDuration = 0.12f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        CharacterVitals _vitals;
        Renderer[] _renderers;
        MaterialPropertyBlock _block;
        Color _restColor = Color.red;
        float _flashRemaining;
        float _reviveRemaining;
        bool _down;

        // ---------------------------------------------------------------- leitura

        public float CurrentVitality => _vitals != null ? _vitals.CurrentVitality : 0f;

        public float MaxVitality => _vitals != null ? _vitals.MaxVitality : 0f;

        public bool IsDown => _vitals != null && _vitals.IsDown;

        // ---------------------------------------------------------------- ciclo

        void Awake()
        {
            _vitals = GetComponent<CharacterVitals>();

            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            _block = new MaterialPropertyBlock();

            if (_renderers.Length > 0 && _renderers[0].sharedMaterial != null &&
                _renderers[0].sharedMaterial.HasProperty(BaseColorId))
                _restColor = _renderers[0].sharedMaterial.GetColor(BaseColorId);
        }

        void OnEnable() => _vitals.VitalityChanged += OnVitalityChanged;

        void OnDisable() => _vitals.VitalityChanged -= OnVitalityChanged;

        void Update()
        {
            float deltaTime = Time.deltaTime;

            if (_flashRemaining > 0f)
            {
                _flashRemaining -= deltaTime;
                if (_flashRemaining <= 0f) Paint(_restColor);
            }

            // Levantar e decisao, e decisao e do host. O cliente nao conta este tempo:
            // ele so ve a vida voltar e poe a capsula de pe.
            if (_reviveRemaining > 0f && _vitals.CanResolve)
            {
                _reviveRemaining -= deltaTime;
                if (_reviveRemaining <= 0f) _vitals.RestoreToFull();
            }
        }

        // ---------------------------------------------------------------- interno

        /// <summary>
        /// Roda em todas as maquinas, porque a vida chega replicada em todas elas. E o que
        /// faz o piscar do companheiro aparecer na sua tela sem custar um RPC.
        /// </summary>
        void OnVitalityChanged(float before, float after)
        {
            if (after < before)
            {
                Paint(hitFlashColor);
                _flashRemaining = hitFlashDuration;
            }

            if (after <= 0f && !_down) GoDown();
            else if (after > 0f && _down) StandUp();
        }

        void GoDown()
        {
            _down = true;
            _reviveRemaining = reviveDelay;

            // Deita a capsula em vez de destrui-la: o alvo continua existindo para
            // inspecao e a lista de ja-atingidos do golpe corrente segue valida.
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);

            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }

        void StandUp()
        {
            _down = false;
            _reviveRemaining = 0f;

            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            Paint(_restColor);

            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = true;
        }

        /// <summary>
        /// MaterialPropertyBlock em vez de <c>renderer.material</c>: o segundo instancia
        /// um material novo por objeto no primeiro acesso, e isso vaza memoria em cena.
        /// </summary>
        void Paint(Color color)
        {
            if (_renderers == null) return;

            _block.SetColor(BaseColorId, color);

            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].SetPropertyBlock(_block);
        }
    }
}
