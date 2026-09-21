using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LoboBranco.EditorTools
{
    /// <summary>
    /// Acrescenta ao asset de controles o que a roda de sinais precisa (tarefa 1.18g), sem tocar no
    /// que ja esta la. Mesmo principio do resto do setup: rodar de novo nao desfaz rebind feito a mao.
    ///
    /// Sao duas coisas. As <b>teclas diretas</b> de sinal, uma acao por vaga, e o <b>toque</b> no
    /// botao de sinal: sem ele, segurar Q lancaria o sinal antes de a roda abrir, porque as duas
    /// acoes moram na mesma tecla (docs/02 secao 4).
    /// </summary>
    public static class ControlsSetup
    {
        const string ControlsPath = "Assets/_Project/Settings/PlayerControls.inputactions";
        const string MapName = "Player";

        /// <summary>
        /// A mesma duracao do <c>Hold</c> da roda. Os dois numeros sao um so: o toque e "soltei
        /// antes de a roda abrir", e qualquer diferenca entre eles cria uma janela em que apertar Q
        /// nao faz nem uma coisa nem outra.
        /// </summary>
        const float TapSeconds = 0.3f;

        /// <summary>
        /// As teclas 1 e 2 sao as espadas (docs/02 secao 4), entao os sinais seguem na mesma fileira,
        /// a partir do 3. No gamepad nao ha tecla direta: o direcional ja tem espada e item, e quem
        /// joga no controle usa a roda.
        /// </summary>
        static readonly string[] DirectKeys = { "3", "4", "5", "6", "7" };

        [MenuItem("Lobo Branco/Setup/10. Teclas de sinal")]
        public static void ConfigureSignKeys()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);

            if (asset == null)
            {
                Debug.LogError($"[Controls] Nao achei {ControlsPath}.");
                return;
            }

            InputActionMap map = asset.FindActionMap(MapName);

            if (map == null)
            {
                Debug.LogError($"[Controls] O asset nao tem o mapa '{MapName}'.");
                return;
            }

            bool changed = EnsureDirectKeys(map);
            changed |= EnsureTapOnCast(map);

            if (!changed)
            {
                Debug.Log("[Controls] Nada a fazer: as teclas de sinal ja estao configuradas.");
                return;
            }

            File.WriteAllText(ControlsPath, asset.ToJson());
            AssetDatabase.ImportAsset(ControlsPath);

            Debug.Log("[Controls] Teclas de sinal configuradas.");
        }

        static bool EnsureDirectKeys(InputActionMap map)
        {
            bool changed = false;

            for (int i = 0; i < DirectKeys.Length; i++)
            {
                string name = SignActionName(i);
                if (map.FindAction(name) != null) continue;

                InputAction action = map.AddAction(name, InputActionType.Button);
                action.AddBinding($"<Keyboard>/{DirectKeys[i]}");

                changed = true;
                Debug.Log($"[Controls] Criada a acao {name} na tecla {DirectKeys[i]}.");
            }

            return changed;
        }

        /// <summary>
        /// O toque vai nas ligacoes, e nao na acao, porque e la que o Input System deixa escrever
        /// sem recriar a acao inteira e perder as ligacoes que ja existem.
        /// </summary>
        static bool EnsureTapOnCast(InputActionMap map)
        {
            InputAction cast = map.FindAction("CastSign");
            if (cast == null) return false;

            string interaction = $"Tap(duration={TapSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
            bool changed = false;

            for (int i = 0; i < cast.bindings.Count; i++)
            {
                if (cast.bindings[i].interactions == interaction) continue;

                InputActionSetupExtensions.ChangeBinding(cast, i).WithInteractions(interaction);
                changed = true;
            }

            if (changed)
                Debug.Log($"[Controls] O botao de sinal virou toque ({interaction}): segurar abre a roda e nao conjura.");

            return changed;
        }

        /// <summary>O nome da acao da vaga, contado a partir de 1 como as teclas.</summary>
        public static string SignActionName(int slot) => $"SelectSign{slot + 1}";

        /// <summary>Quantas vagas tem tecla direta.</summary>
        public static int DirectKeyCount => DirectKeys.Length;
    }
}
