using System.IO;
using LoboBranco.Combat;
using LoboBranco.Player;
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
        const string AttacksFolder = "Assets/_Project/Data/Combat/Attacks";
        const string WeaponsFolder = "Assets/_Project/Data/Combat/Weapons";
        const string PlayerFolder = "Assets/_Project/Data/Player";

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

            // docs/03 secao 12: barghest tem 55 de vitalidade, 16 de dano, 2 de armadura.
            CreateStatBlock($"{StatsFolder}/StatBlock_Barghest.asset", new[]
            {
                Entry(StatType.MaxVitality, 55f),
                Entry(StatType.AttackDamage, 16f),
                Entry(StatType.Armor, 2f),
            });

            CreateAttacks();
            CreateWeapons();
            CreatePlayerTuning();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CombatData] Assets de combate prontos.");
        }

        // --------------------------------------------------------------- ataques

        /// <summary>
        /// Os tres golpes da tabela do docs/03 secao 4. Tempo do golpe e recuperacao vem
        /// direto de la; a divisao do tempo do golpe entre anticipacao e janela de dano
        /// nao esta no documento e foi decidida aqui (tech/adr/0007).
        /// </summary>
        static void CreateAttacks()
        {
            EnsureFolder(AttacksFolder);

            CreateAttack($"{AttacksFolder}/Attack_Light.asset", a =>
            {
                a.stance = Stance.Fast;         // 0,75x de dano
                a.strikeTime = 0.25f;
                a.recovery = 0.15f;
                a.hitboxOpenAt = 0.55f;         // anticipacao de 0,14 s, janela de 0,10 s
                a.hitboxCloseAt = 0.95f;
                a.staminaCost = 4f;
                a.maxTargets = 1;
                a.arcDegrees = 110f;
                a.reach = 2.2f;
                a.radius = 0.55f;
                a.heightOffset = 1.1f;
            });

            CreateAttack($"{AttacksFolder}/Attack_Heavy.asset", a =>
            {
                a.stance = Stance.Strong;       // 1,45x de dano
                a.strikeTime = 0.55f;
                a.recovery = 0.45f;
                a.hitboxOpenAt = 0.72f;         // anticipacao de 0,40 s: e o golpe telegrafado
                a.hitboxCloseAt = 0.96f;
                a.staminaCost = 8f;
                a.maxTargets = 1;
                a.arcDegrees = 100f;
                a.reach = 2.4f;
                a.radius = 0.6f;
                a.heightOffset = 1.1f;
            });

            // Sem input ligado ainda: a troca de postura e a tarefa 1.14. O asset existe
            // agora porque ele e o unico que exercita arco de 180 graus e quatro alvos.
            CreateAttack($"{AttacksFolder}/Attack_Group.asset", a =>
            {
                a.stance = Stance.Group;        // 0,90x de dano
                a.strikeTime = 0.40f;
                a.recovery = 0.55f;
                a.hitboxOpenAt = 0.62f;
                a.hitboxCloseAt = 0.95f;
                a.staminaCost = 12f;
                a.maxTargets = 4;
                a.arcDegrees = 180f;
                a.reach = 2.6f;
                a.radius = 0.9f;
                a.heightOffset = 1.1f;
            });
        }

        static void CreateWeapons()
        {
            EnsureFolder(WeaponsFolder);

            // docs/03 secao 12: jogador nivel 1 bate 12 de base.
            CreateWeapon($"{WeaponsFolder}/Weapon_SteelSword.asset", WeaponMaterial.Steel, 12f);
            CreateWeapon($"{WeaponsFolder}/Weapon_SilverSword.asset", WeaponMaterial.Silver, 12f);
        }

        static void CreatePlayerTuning()
        {
            EnsureFolder(PlayerFolder);

            if (File.Exists($"{PlayerFolder}/PlayerTuning.asset"))
            {
                Debug.Log($"[CombatData] Ja existe, mantido: {PlayerFolder}/PlayerTuning.asset");
                return;
            }

            var asset = ScriptableObject.CreateInstance<PlayerTuningDef>();
            asset.inputBufferSeconds = 0.2f;    // docs/07 secao 4.4

            AssetDatabase.CreateAsset(asset, $"{PlayerFolder}/PlayerTuning.asset");
            Debug.Log($"[CombatData] Criado: {PlayerFolder}/PlayerTuning.asset");
        }

        static void CreateAttack(string path, System.Action<AttackDef> configure)
        {
            if (File.Exists(path))
            {
                Debug.Log($"[CombatData] Ja existe, mantido: {path}");
                return;
            }

            var asset = ScriptableObject.CreateInstance<AttackDef>();
            configure(asset);

            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[CombatData] Criado: {path}");
        }

        static void CreateWeapon(string path, WeaponMaterial material, float baseDamage)
        {
            if (File.Exists(path))
            {
                Debug.Log($"[CombatData] Ja existe, mantido: {path}");
                return;
            }

            var asset = ScriptableObject.CreateInstance<MeleeWeaponDef>();
            asset.material = material;
            asset.damageType = DamageType.Slash;
            asset.baseDamage = baseDamage;

            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[CombatData] Criado: {path}");
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
