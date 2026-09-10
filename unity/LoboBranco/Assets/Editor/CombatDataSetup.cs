using System.IO;
using LoboBranco.Combat;
using LoboBranco.Stats;
using UnityEditor;
using UnityEngine;

namespace LoboBranco.EditorTools
{
    /// <summary>
    /// Cria os assets de dados de combate com os valores de referencia do docs/03
    /// secao 12, por codigo em vez de a mao.
    ///
    /// Motivo: asset criado a mao nao e reproduzivel e ninguem sabe de onde veio o numero.
    /// Aqui cada valor tem a secao do documento ao lado. Rodar de novo NAO sobrescreve
    /// um asset existente, para nao apagar balanceamento ja afinado.
    /// </summary>
    public static class CombatDataSetup
    {
        const string CombatFolder = "Assets/_Project/Data/Combat";
        const string StatsFolder = "Assets/_Project/Data/Stats";

        [MenuItem("Lobo Branco/Setup/6. Criar assets de combate")]
        public static void CreateCombatData()
        {
            EnsureFolder(CombatFolder);
            EnsureFolder(StatsFolder);

            // Os multiplicadores padrao ja estao nos campos de CombatTuningDef; criar a
            // instancia basta. Ela e a superficie de balanceamento do combate inteiro.
            CreateIfMissing<CombatTuningDef>($"{CombatFolder}/CombatTuning.asset");

            // docs/03 secao 12: jogador nivel 1 tem 100 de vitalidade, 12 de dano, 4 de armadura.
            CreateStatBlock($"{StatsFolder}/StatBlock_Player.asset", new[]
            {
                Entry(StatType.Strength, 10f),
                Entry(StatType.Dexterity, 10f),
                Entry(StatType.Endurance, 10f),
                Entry(StatType.Intelligence, 10f),

                Entry(StatType.MaxVitality, 100f),
                Entry(StatType.MaxStamina, 100f),
                Entry(StatType.MaxToxicity, 60f),      // docs/02 secao 5
                Entry(StatType.StaminaRegen, 18f),     // docs/03 secao 7
                Entry(StatType.StaminaRegenInCombat, 6f),

                Entry(StatType.AttackDamage, 0f),      // o dano vem da arma; isto e bonus
                Entry(StatType.DamageMultiplier, 1f),  // neutro; pocoes somam por cima
                Entry(StatType.Armor, 4f),

                Entry(StatType.MoveSpeed, 1f),
                Entry(StatType.CarryWeight, 60f),
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CombatData] Assets de combate prontos.");
        }

        // ------------------------------------------------------------------ util

        static StatBlockDef.Entry Entry(StatType stat, float value)
            => new StatBlockDef.Entry { stat = stat, value = value };

        static void CreateStatBlock(string path, StatBlockDef.Entry[] entries)
        {
            if (File.Exists(path))
            {
                Debug.Log($"[CombatData] Ja existe, mantido: {path}");
                return;
            }

            var asset = ScriptableObject.CreateInstance<StatBlockDef>();

            // O campo e privado com [SerializeField]; SerializedObject e o caminho
            // que respeita a serializacao da Unity.
            var so = new SerializedObject(asset);
            SerializedProperty array = so.FindProperty("entries");
            array.arraySize = entries.Length;

            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("stat").enumValueIndex = EnumIndexOf(entries[i].stat);
                element.FindPropertyRelative("value").floatValue = entries[i].value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[CombatData] Criado: {path}");
        }

        /// <summary>
        /// StatType e esparso de proposito, entao enumValueIndex nao e o valor numerico:
        /// e a posicao na lista de valores. Confundir os dois grava o atributo errado.
        /// </summary>
        static int EnumIndexOf(StatType stat)
        {
            string[] names = System.Enum.GetNames(typeof(StatType));
            string target = stat.ToString();

            for (int i = 0; i < names.Length; i++)
                if (names[i] == target)
                    return i;

            return 0;
        }

        static void CreateIfMissing<T>(string path) where T : ScriptableObject
        {
            if (File.Exists(path))
            {
                Debug.Log($"[CombatData] Ja existe, mantido: {path}");
                return;
            }

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
            Debug.Log($"[CombatData] Criado: {path}");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
