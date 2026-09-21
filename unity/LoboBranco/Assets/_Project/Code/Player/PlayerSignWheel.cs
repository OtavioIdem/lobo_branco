using LoboBranco.Combat;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// A roda de sinais (tarefa 1.18g, docs/02 secao 4): tocar o botao de sinal conjura, segurar
    /// abre a roda, e as teclas 3 a 7 escolhem direto.
    ///
    /// <b>Autoridade: ninguem.</b> Escolher sinal e local, como escolher postura, e nada disso viaja.
    /// A vaga so vai para o host quando a conjuracao e pedida (tech/adr/0011). Por isso este
    /// componente so faz alguma coisa no personagem que esta maquina controla.
    ///
    /// <b>A roda nunca desacelera o tempo</b>, ao contrario do Witcher 3 (tech/adr/0010). Em coop a
    /// escala de tempo e global na maquina, e no host ela congelaria a sessao de todo mundo enquanto
    /// um jogador escolhe. O mundo corre enquanto o bruxo pensa, e e isso que faz escolher no meio da
    /// luta ser uma decisao com risco.
    ///
    /// A regra e a <see cref="SignWheelSelection"/>, que nao conhece input nem tela. Aqui so mora a
    /// ligacao com o leitor de input e o desenho de greybox.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerBrain))]
    public sealed class PlayerSignWheel : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerBrain brain;
        [SerializeField] PlayerSchool school;
        [SerializeField] PlayerAbilityCaster caster;

        [Tooltip("Quanto o ponteiro anda por unidade de mouse. O analogico ja vem de -1 a 1 e mal sente isto.")]
        [Min(0.0001f)] [SerializeField] float pointerSensitivity = 0.02f;

        [Tooltip("Raio da roda de greybox, em pixels. Some quando o HUD de verdade existir (docs/02 secao 7).")]
        [SerializeField] float wheelRadius = 120f;

        readonly SignWheelSelection _wheel = new SignWheelSelection();

        GUIStyle _style;

        /// <summary>A roda esta aberta. O painel de debug mostra isto.</summary>
        public bool IsOpen => _wheel.IsOpen;

        /// <summary>A vaga escolhida.</summary>
        public int Selected => _wheel.Selected;

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (brain == null) brain = GetComponent<PlayerBrain>();
            if (school == null) school = GetComponent<PlayerSchool>();
            if (caster == null) caster = GetComponent<PlayerAbilityCaster>();
        }

        void OnEnable()
        {
            if (input == null) return;

            input.SignWheelOpened += OnOpened;
            input.SignWheelClosed += OnClosed;
            input.SignSlotPressed += OnDirectKey;
        }

        void OnDisable()
        {
            if (input == null) return;

            input.SignWheelOpened -= OnOpened;
            input.SignWheelClosed -= OnClosed;
            input.SignSlotPressed -= OnDirectKey;

            _wheel.Close();
        }

        void Update()
        {
            // O companheiro tem este componente e nao le input nenhum: quem escolhe o sinal dele e a
            // maquina de quem o controla.
            if (brain == null || !brain.DrivesThisCharacter) return;

            _wheel.SlotCount = school != null ? school.AbilityCount : 0;

            if (_wheel.IsOpen && input != null)
                _wheel.Point(input.Look * pointerSensitivity);

            brain.SelectAbilitySlot(_wheel.Selected);
        }

        void OnOpened()
        {
            if (brain != null && !brain.DrivesThisCharacter) return;

            _wheel.Open();
        }

        void OnClosed()
        {
            // Fechar nao conjura. O sinal escolhido sai no proximo toque, e e por isso que soltar a
            // roda nunca gasta vigor sem o jogador mandar.
            _wheel.Close();
        }

        void OnDirectKey(int slot)
        {
            if (brain != null && !brain.DrivesThisCharacter) return;

            _wheel.SelectDirect(slot);
        }

        /// <summary>
        /// Desenho de greybox, em IMGUI, pelo mesmo motivo do painel de debug: e feio, custa zero, e
        /// nao concorre com o HUD em UI Toolkit do docs/02 secao 7, que e a tarefa 1.29.
        /// </summary>
        void OnGUI()
        {
            if (!_wheel.IsOpen || caster == null) return;
            if (brain != null && !brain.DrivesThisCharacter) return;

            _style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };

            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            for (int slot = 0; slot < _wheel.SlotCount; slot++)
            {
                AbilityDef ability = caster.AbilityAt(slot);
                if (ability == null) continue;

                float radians = _wheel.AngleOf(slot) * Mathf.Deg2Rad;
                Vector2 position = center + new Vector2(Mathf.Sin(radians), -Mathf.Cos(radians)) * wheelRadius;

                float remaining = caster.CooldownRemaining(slot);
                string label = remaining > 0f
                    ? $"{ability.displayName}\n{remaining:F1}s"
                    : ability.displayName;

                var rect = new Rect(position.x - 55f, position.y - 22f, 110f, 44f);

                Color previous = GUI.color;
                GUI.color = slot == _wheel.Selected ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                GUI.Box(rect, label, _style);
                GUI.color = previous;
            }
        }
    }
}
