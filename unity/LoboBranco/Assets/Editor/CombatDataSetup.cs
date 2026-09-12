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
        const string MonstersFolder = "Assets/_Project/Data/Monsters";

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
            CreateMonsters();

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

            // A garra do barghest (tarefa 1.21). A anticipacao aqui nao e detalhe de
            // afinacao, e o telegrafo do docs/03 secao 10: 0,65 s antes da janela abrir,
            // dentro da faixa de 0,4 a 0,9 s que o documento pede. E esse tempo que a
            // esquiva da tarefa 1.10 vai ter para acontecer, e quem encurtar este numero
            // esta tornando o combate injusto, nao dificil.
            CreateAttack($"{AttacksFolder}/Attack_Barghest_Claw.asset", a =>
            {
                a.stance = Stance.Fast;         // besta agil ataca rapido
                a.strikeTime = 1.0f;
                a.recovery = 0.45f;
                a.hitboxOpenAt = 0.65f;         // anticipacao de 0,65 s: o tell
                a.hitboxCloseAt = 0.85f;        // janela de 0,20 s
                a.staminaCost = 0f;             // monstro nao gasta vigor (docs/03 secao 7)
                a.maxTargets = 1;
                a.arcDegrees = 90f;
                a.reach = 2.0f;
                a.radius = 0.5f;
                a.heightOffset = 1.0f;
            });
        }

        static void CreateWeapons()
        {
            EnsureFolder(WeaponsFolder);

            // docs/03 secao 12: jogador nivel 1 bate 12 de base.
            CreateWeapon($"{WeaponsFolder}/Weapon_SteelSword.asset", WeaponMaterial.Steel, 12f);
            CreateWeapon($"{WeaponsFolder}/Weapon_SilverSword.asset", WeaponMaterial.Silver, 12f);

            // A garra do barghest bate zero de dano cru, e isso esta certo: os 16 da
            // tabela do docs/03 secao 12 ja moram no AttackDamage do StatBlock_Barghest, e
            // o estagio 1 do pipeline soma os dois. Repetir os 16 aqui dobraria o dano da
            // criatura e o sintoma apareceria so no playtest. O que este asset carrega e
            // o tipo de dano e o material do estagio 4.
            CreateWeapon($"{WeaponsFolder}/Weapon_BarghestClaws.asset", WeaponMaterial.Steel, 0f);
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

        // -------------------------------------------------------------- especies

        /// <summary>
        /// A especie do alvo de sandbox. Os numeros dela nao ficam aqui: o asset aponta
        /// para o <c>StatBlock_Barghest</c>, que ja tem os 55 de vitalidade do docs/03
        /// secao 12. Quem duplica numero acaba com dois barghests diferentes.
        ///
        /// As resistencias saem vazias, e isso e deliberado: quais criaturas resistem a
        /// que e decisao de balanceamento, e balanceamento e a tarefa 1.30, com o jogo
        /// rodando. O que esta pronto aqui e o lugar onde esses numeros vao morar.
        /// </summary>
        static void CreateMonsters()
        {
            EnsureFolder(MonstersFolder);

            string barghestPath = $"{MonstersFolder}/Monster_Barghest.asset";
            var barghest = AssetDatabase.LoadAssetAtPath<MonsterDef>(barghestPath);

            if (barghest == null)
            {
                barghest = ScriptableObject.CreateInstance<MonsterDef>();
                barghest.displayName = "Barghest";
                barghest.creatureClass = CreatureClass.Beast;
                barghest.archetype = StanceArchetype.Agile;   // docs/03 secao 4: postura Rapida
                barghest.vulnerableToOil = OilClass.Beast;    // docs/05 secao 5: Oleo de Besta
                barghest.statBlock = AssetDatabase.LoadAssetAtPath<StatBlockDef>($"{StatsFolder}/StatBlock_Barghest.asset");

                AssetDatabase.CreateAsset(barghest, barghestPath);
                Debug.Log($"[CombatData] Criado: {barghestPath}");
            }

            // Golpe e arma natural entraram na tarefa 1.21, depois de o asset ja existir.
            // Preencher so o que esta vazio e o que permite rodar este setup de novo sem
            // desfazer balanceamento ja afinado a mao.
            LinkIfMissing(barghest, barghestPath);

            CreateWitcherProfile();
        }

        static void LinkIfMissing(MonsterDef monster, string path)
        {
            bool changed = false;

            if (monster.meleeAttack == null)
            {
                monster.meleeAttack = AssetDatabase.LoadAssetAtPath<AttackDef>(
                    $"{AttacksFolder}/Attack_Barghest_Claw.asset");
                changed = monster.meleeAttack != null;
            }

            if (monster.naturalWeapon == null)
            {
                monster.naturalWeapon = AssetDatabase.LoadAssetAtPath<MeleeWeaponDef>(
                    $"{WeaponsFolder}/Weapon_BarghestClaws.asset");
                changed |= monster.naturalWeapon != null;
            }

            if (!changed) return;

            EditorUtility.SetDirty(monster);
            Debug.Log($"[CombatData] Golpe e arma natural ligados em {path}.");
        }

        /// <summary>
        /// O perfil de combate do bruxo. Ele existe porque, a partir da tarefa 1.21,
        /// alguem bate no jogador, e o pipeline de dano faz ao alvo as mesmas quatro
        /// perguntas de sempre: classe, arquetipo, oleo que casa e resistencia.
        ///
        /// Humanoide nao e detalhe de fantasia, e o numero mais pesado que incide sobre o
        /// bruxo: com ele, o aco de um bandido vale 1,0x; sem ele, o alvo vira besta e o
        /// mesmo aco cai para 0,35x, o que deixaria o jogador quase invulneravel a metade
        /// dos inimigos do capitulo (docs/03 secao 3).
        ///
        /// Arquetipo Agil porque o bruxo esquiva e nao encaixa golpe: e o que decide se o
        /// golpe que vem casa ou nao casa com ele, no estagio 3.
        /// </summary>
        static void CreateWitcherProfile()
        {
            string path = $"{MonstersFolder}/Combatant_Witcher.asset";

            if (File.Exists(path))
            {
                Debug.Log($"[CombatData] Ja existe, mantido: {path}");
                return;
            }

            var asset = ScriptableObject.CreateInstance<MonsterDef>();
            asset.displayName = "Bruxo";
            asset.creatureClass = CreatureClass.Humanoid;
            asset.archetype = StanceArchetype.Agile;
            asset.vulnerableToOil = OilClass.None;        // oleo e arma de bruxo, nao contra ele
            asset.statBlock = AssetDatabase.LoadAssetAtPath<StatBlockDef>($"{StatsFolder}/StatBlock_Player.asset");

            // Os sentidos e o golpe ficam zerados de proposito: quem decide o que o bruxo
            // percebe e faz e o jogador, e nao uma arvore de comportamento.
            asset.sightRange = 0f;
            asset.hearingRange = 0f;
            asset.moveSpeed = 0f;

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
