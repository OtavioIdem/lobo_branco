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
        const string AbilitiesFolder = "Assets/_Project/Data/Combat/Abilities";
        const string PlayerFolder = "Assets/_Project/Data/Player";
        const string MonstersFolder = "Assets/_Project/Data/Monsters";
        const string SignEffectsFolder = "Assets/_Project/Data/Combat/SignEffects";
        const string KnockbackControlPath = SignEffectsFolder + "/SignEffect_Knockback_Control.asset";
        const string FireDamagePath = SignEffectsFolder + "/SignEffect_Fire_Damage.asset";
        const string FireBurnPath = SignEffectsFolder + "/SignEffect_Fire_Burn.asset";
        const string FireSignPath = AbilitiesFolder + "/Sign_Fire.asset";
        const string WardPath = SignEffectsFolder + "/SignEffect_Ward.asset";
        const string WardSignPath = AbilitiesFolder + "/Sign_Ward.asset";

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
                Entry(StatType.SignIntensity, 1f),     // neutro; a Inteligencia e que escala (tarefa 1.18b)
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
                Entry(StatType.MoveSpeed, 1f),         // neutro; a lentidao multiplica isto
            });

            // O MoveSpeed entrou na tarefa 1.18a, depois de o bloco ja existir. Sem ele, a
            // lentidao multiplica zero e nao pega (ver MonsterAssetsTests).
            EnsureStatEntry($"{StatsFolder}/StatBlock_Barghest.asset", StatType.MoveSpeed, 1f);

            // O SignIntensity entrou na tarefa 1.18b, pelo mesmo motivo: zero nele e todo sinal
            // sai com potencia zero (ver SchoolAssetsTests).
            EnsureStatEntry($"{StatsFolder}/StatBlock_Player.asset", StatType.SignIntensity, 1f);

            CreateAttacks();
            CreateWeapons();
            CreatePlayerTuning();
            CreateMonsters();
            CreateTelegraphStyle();
            CreateHitFeedback();

            // Antes das habilidades: o abridor aponta para o efeito de controle.
            CreateSignEffects();
            CreateAbilities();
            CreateFireSign();
            CreateWardSign();

            // Por ultimo: a escola aponta para golpes, habilidades e bloco de atributos criados acima.
            CreateSchools();

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
                barghest.bodyWeight = BodyWeight.Light;       // decidido na 1.18c, ver abaixo
                barghest.statBlock = AssetDatabase.LoadAssetAtPath<StatBlockDef>($"{StatsFolder}/StatBlock_Barghest.asset");

                AssetDatabase.CreateAsset(barghest, barghestPath);
                Debug.Log($"[CombatData] Criado: {barghestPath}");
            }

            // Golpe e arma natural entraram na tarefa 1.21, depois de o asset ja existir.
            // Preencher so o que esta vazio e o que permite rodar este setup de novo sem
            // desfazer balanceamento ja afinado a mao.
            LinkIfMissing(barghest, barghestPath);

            // O porte entrou na tarefa 1.18c, depois de o asset existir, e o zero do enum e medio.
            // Nao da para distinguir "medio de proposito" de "nunca preenchido" pelo valor, entao
            // a pergunta e ao arquivo: sem a linha, o campo nunca foi gravado.
            //
            // Leve porque a composicao Matilha do docs/03 secao 10 existe para ensinar o abridor, e
            // um abridor que so atordoa barghest nao ensina nada que a espada nao ensine.
            if (FieldNeverSaved(barghestPath, "bodyWeight"))
            {
                barghest.bodyWeight = BodyWeight.Light;
                EditorUtility.SetDirty(barghest);
                Debug.Log($"[CombatData] Porte leve ligado em {barghestPath}.");
            }

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

        // ---------------------------------------------------------------- escolas

        /// <summary>
        /// A Escola do Lobo, que o docs/13 secao 5 descreve como "o kit que ja existe". Por isso
        /// ela aponta para os mesmos assets que o jogador ja usava, e nenhum numero muda: criar a
        /// escola nao pode mudar o jogo. Mudar o jogo com uma escola nova e a tarefa 1.33.
        /// </summary>
        static void CreateSchools()
        {
            EnsureFolder(PlayerFolder);

            string path = $"{PlayerFolder}/School_Wolf.asset";
            var asset = AssetDatabase.LoadAssetAtPath<SchoolDef>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SchoolDef>();
                asset.displayName = "Lobo";
                asset.statBlock = AssetDatabase.LoadAssetAtPath<StatBlockDef>($"{StatsFolder}/StatBlock_Player.asset");
                asset.favoredStance = Stance.Fast;    // docs/13 secao 5: o Lobo favorece a Rapida
                asset.strongAttack = AssetDatabase.LoadAssetAtPath<AttackDef>($"{AttacksFolder}/Attack_Heavy.asset");
                asset.fastAttack = AssetDatabase.LoadAssetAtPath<AttackDef>($"{AttacksFolder}/Attack_Light.asset");
                asset.groupAttack = AssetDatabase.LoadAssetAtPath<AttackDef>($"{AttacksFolder}/Attack_Group.asset");

                AssetDatabase.CreateAsset(asset, path);
                Debug.Log($"[CombatData] Criado: {path}");
            }

            var knockback = AssetDatabase.LoadAssetAtPath<AbilityDef>($"{AbilitiesFolder}/Sign_Knockback.asset");
            if (knockback == null) return;

            // As habilidades entraram na tarefa 1.32, depois de a escola ja existir. Mesmo
            // arranjo do barghest: preencher so o que esta vazio.
            if (asset.abilities == null || asset.abilities.Length == 0)
            {
                // docs/13 secao 5: "equilibrado, espada e sinal". O sinal e o abridor do docs/03 secao 8.
                asset.abilities = new[] { knockback };
                EditorUtility.SetDirty(asset);
                Debug.Log($"[CombatData] Habilidades ligadas em {path}.");
            }

            // O fogo (1.18d) e o escudo (1.18e) entraram nas vagas seguintes. Todas as escolas tem os
            // cinco sinais (docs/13 secao 5). Ate a roda da 1.18g, Q so usa a primeira vaga, e os
            // outros esperam la.
            EnsureAbilitySlot(asset, path, AssetDatabase.LoadAssetAtPath<AbilityDef>(FireSignPath));
            EnsureAbilitySlot(asset, path, AssetDatabase.LoadAssetAtPath<AbilityDef>(WardSignPath));

            // A especializacao entrou na tarefa 1.18b. O Lobo e especializado no abridor pelo
            // docs/13 secao 5, e a variante sai vazia: o efeito dela ainda nao foi desenhado.
            if (asset.specializedAbility == null)
            {
                asset.specializedAbility = knockback;
                EditorUtility.SetDirty(asset);
                Debug.Log($"[CombatData] Sinal especializado ligado em {path}.");
            }
        }

        /// <summary>Poe um sinal na proxima vaga livre da escola, se ele ja nao estiver em alguma.</summary>
        static void EnsureAbilitySlot(SchoolDef school, string schoolPath, AbilityDef ability)
        {
            if (ability == null || school.abilities == null) return;
            if (System.Array.IndexOf(school.abilities, ability) >= 0) return;

            var slots = new AbilityDef[school.abilities.Length + 1];
            school.abilities.CopyTo(slots, 0);
            slots[slots.Length - 1] = ability;

            school.abilities = slots;
            EditorUtility.SetDirty(school);
            Debug.Log($"[CombatData] '{ability.name}' ligado na vaga {slots.Length - 1} de {schoolPath}.");
        }

        // ----------------------------------------------------------- habilidades

        /// <summary>
        /// O abridor do docs/03 secao 8, o primeiro sinal do jogo (tarefa 1.32). Custo e recarga
        /// sao os do documento. O tempo de conjurar e a recuperacao nao estao la, e foram decididos
        /// aqui: 0,7 s no total, o mesmo da troca de espada, porque um sinal e uma decisao do mesmo
        /// peso. O efeito e a tarefa 1.18, e ate la o sinal cobra, recarrega e nao faz nada.
        ///
        /// O nome do asset e o do efeito, e o nome do sinal vem do campo exibido (tech/adr/0005).
        /// </summary>
        static void CreateAbilities()
        {
            EnsureFolder(AbilitiesFolder);

            string path = $"{AbilitiesFolder}/Sign_Knockback.asset";
            var asset = AssetDatabase.LoadAssetAtPath<AbilityDef>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AbilityDef>();
                asset.displayName = "Aard";
                asset.staminaCost = 30f;       // docs/03 secao 8
                asset.cooldownSeconds = 4f;    // docs/03 secao 8
                asset.castTime = 0.3f;         // decidido na tarefa 1.32
                asset.recovery = 0.4f;         // decidido na tarefa 1.32

                AssetDatabase.CreateAsset(asset, path);
                Debug.Log($"[CombatData] Criado: {path}");
            }

            // O efeito entrou na tarefa 1.18c. Preencher so a lista vazia, como o resto.
            if (asset.effects == null || asset.effects.Length == 0)
            {
                var control = AssetDatabase.LoadAssetAtPath<ControlEffectDef>(KnockbackControlPath);

                if (control != null)
                {
                    asset.effects = new SignEffectDef[] { control };
                    EditorUtility.SetDirty(asset);
                    Debug.Log($"[CombatData] Efeito de controle ligado em {path}.");
                }
            }

            // A area entrou na tarefa 1.18b, depois de o asset ja existir. Os 6 m sao do docs/03
            // secao 8; a abertura nao esta la, e 90 graus foi decidido na 1.18b: larga o bastante
            // para pegar os dois barghests que flanqueiam, estreita o bastante para o abridor ser
            // mirado. Os efeitos sao da tarefa 1.18c.
            if (!asset.area.HasArea)
            {
                asset.area = new SignArea
                {
                    shape = SignAreaShape.Cone,
                    range = 6f,
                    coneAngleDegrees = 90f,
                };

                EditorUtility.SetDirty(asset);
                Debug.Log($"[CombatData] Area ligada em {path}.");
            }
        }

        /// <summary>
        /// O escudo do docs/03 secao 8 (tarefa 1.18e). Custo e recarga sao do documento; conjurar e
        /// recuperar repetem os dos outros sinais.
        ///
        /// A area e a forma <c>Self</c>: o escudo nao procura alvo, e por isso nao tem alcance nem
        /// abertura para decidir.
        /// </summary>
        static void CreateWardSign()
        {
            var asset = AssetDatabase.LoadAssetAtPath<AbilityDef>(WardSignPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AbilityDef>();
                asset.displayName = "Quen";
                asset.staminaCost = 25f;       // docs/03 secao 8
                asset.cooldownSeconds = 6f;    // docs/03 secao 8
                asset.castTime = 0.3f;
                asset.recovery = 0.4f;
                asset.area = new SignArea { shape = SignAreaShape.Self };

                AssetDatabase.CreateAsset(asset, WardSignPath);
                Debug.Log($"[CombatData] Criado: {WardSignPath}");
            }

            if (asset.effects != null && asset.effects.Length > 0) return;

            var ward = AssetDatabase.LoadAssetAtPath<WardEffectDef>(WardPath);
            if (ward == null) return;

            asset.effects = new SignEffectDef[] { ward };
            EditorUtility.SetDirty(asset);
            Debug.Log($"[CombatData] Efeito ligado em {WardSignPath}.");
        }

        // -------------------------------------------------------- efeitos de sinal

        /// <summary>
        /// O controle do abridor, do docs/03 secao 8 (tarefa 1.18c). Os numeros ficam nos padroes do
        /// proprio <see cref="ControlEffectDef"/>: derruba leves, atordoa medios por 1,5 s, e nao
        /// move pesados. A derrubada de 2 s nao esta no documento e foi decidida na 1.18c: mais longa
        /// que o atordoamento, porque derrubar e o controle mais forte da tabela.
        ///
        /// O nome do asset e o do efeito, e nao o do sinal (tech/adr/0005).
        /// </summary>
        static void CreateSignEffects()
        {
            EnsureFolder(SignEffectsFolder);
            CreateIfMissing<ControlEffectDef>(KnockbackControlPath);

            // O fogo (tarefa 1.18d). Os numeros ficam nos padroes dos proprios tipos, que sao os do
            // docs/03 secao 8: 0,8 vezes a espada como fogo, e Queimadura de 4 por segundo por 5 s.
            CreateIfMissing<DamageEffectDef>(FireDamagePath);
            CreateIfMissing<BurnEffectDef>(FireBurnPath);

            // O escudo (tarefa 1.18e): 8 s e 30% de troco, os padroes do proprio tipo.
            CreateIfMissing<WardEffectDef>(WardPath);

            var damage = AssetDatabase.LoadAssetAtPath<DamageEffectDef>(FireDamagePath);
            if (damage != null && damage.tuning == null)
            {
                damage.tuning = AssetDatabase.LoadAssetAtPath<CombatTuningDef>($"{CombatFolder}/CombatTuning.asset");
                EditorUtility.SetDirty(damage);
                Debug.Log($"[CombatData] Multiplicadores ligados em {FireDamagePath}.");
            }
        }

        /// <summary>
        /// O fogo do docs/03 secao 8 (tarefa 1.18d). Custo e recarga sao do documento; conjurar e
        /// recuperar repetem os do abridor, porque o documento nao os da e os dois sinais sao
        /// decisoes do mesmo peso.
        ///
        /// A abertura de 60 graus nao esta no documento e foi decidida na 1.18d. O fogo e mais
        /// estreito que o abridor de proposito: o abridor derruba a matilha em volta, e o fogo e
        /// mirado no alvo que tem a fraqueza.
        /// </summary>
        static void CreateFireSign()
        {
            var asset = AssetDatabase.LoadAssetAtPath<AbilityDef>(FireSignPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AbilityDef>();
                asset.displayName = "Igni";
                asset.staminaCost = 35f;       // docs/03 secao 8
                asset.cooldownSeconds = 5f;    // docs/03 secao 8
                asset.castTime = 0.3f;         // o mesmo do abridor
                asset.recovery = 0.4f;         // o mesmo do abridor
                asset.area = new SignArea
                {
                    shape = SignAreaShape.Cone,
                    range = 5f,                // docs/03 secao 8
                    coneAngleDegrees = 60f,    // decidido na 1.18d
                };

                AssetDatabase.CreateAsset(asset, FireSignPath);
                Debug.Log($"[CombatData] Criado: {FireSignPath}");
            }

            if (asset.effects != null && asset.effects.Length > 0) return;

            var damage = AssetDatabase.LoadAssetAtPath<DamageEffectDef>(FireDamagePath);
            var burn = AssetDatabase.LoadAssetAtPath<BurnEffectDef>(FireBurnPath);
            if (damage == null || burn == null) return;

            // O dano antes da queimadura: se o dano matar, a queimadura nao acende num cadaver.
            asset.effects = new SignEffectDef[] { damage, burn };
            EditorUtility.SetDirty(asset);
            Debug.Log($"[CombatData] Efeitos ligados em {FireSignPath}.");
        }

        // -------------------------------------------------------------- sensacao

        /// <summary>
        /// Os numeros de sensacao do docs/03 secao 11 (tarefa 1.25). Os valores ficam nos
        /// padroes do proprio <see cref="HitFeedbackDef"/>, que sao os do documento: 0,08 s
        /// de hitstop no golpe forte, 0,04 s no leve, e 2 graus de soco de camera.
        /// </summary>
        static void CreateHitFeedback()
        {
            string path = $"{CombatFolder}/HitFeedback_Default.asset";

            if (File.Exists(path))
            {
                Debug.Log($"[CombatData] Ja existe, mantido: {path}");
                return;
            }

            var asset = ScriptableObject.CreateInstance<HitFeedbackDef>();

            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[CombatData] Criado: {path}");
        }

        // ------------------------------------------------------------- telegrafo

        /// <summary>
        /// A aparencia padrao do telegrafo de ataque (tarefa 1.23). Os valores ficam nos
        /// padroes do proprio <see cref="TelegraphStyleDef"/>, e o motivo de cada um esta
        /// no tooltip: ambar e nao vermelho porque vermelho e o tell de Unblockable, e um
        /// aviso comum vermelho ensinaria ao jogador a resposta errada (docs/03 secao 5).
        /// </summary>
        static void CreateTelegraphStyle()
        {
            string path = $"{CombatFolder}/TelegraphStyle_Default.asset";

            if (File.Exists(path))
            {
                Debug.Log($"[CombatData] Ja existe, mantido: {path}");
                return;
            }

            var asset = ScriptableObject.CreateInstance<TelegraphStyleDef>();

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
        /// Acrescenta um atributo que falta num bloco que ja existe, sem tocar nos que estao la.
        /// Mesmo principio do resto deste setup: rodar de novo nunca desfaz balanceamento afinado a
        /// mao, e so preenche o que um sistema novo passou a precisar.
        /// </summary>
        static void EnsureStatEntry(string path, StatType stat, float value)
        {
            var asset = AssetDatabase.LoadAssetAtPath<StatBlockDef>(path);
            if (asset == null) return;

            var so = new SerializedObject(asset);
            SerializedProperty array = so.FindProperty("entries");
            int index = EnumIndexOf(stat);

            for (int i = 0; i < array.arraySize; i++)
                if (array.GetArrayElementAtIndex(i).FindPropertyRelative("stat").enumValueIndex == index)
                    return;

            array.arraySize++;
            SerializedProperty element = array.GetArrayElementAtIndex(array.arraySize - 1);
            element.FindPropertyRelative("stat").enumValueIndex = index;
            element.FindPropertyRelative("value").floatValue = value;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            Debug.Log($"[CombatData] {stat} acrescentado em {path}.");
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

        /// <summary>
        /// Se um campo novo nunca foi gravado no asset. Serve para campo de enum cujo zero e um
        /// valor valido, em que o valor carregado nao diz se alguem escolheu ou se ninguem mexeu.
        /// </summary>
        static bool FieldNeverSaved(string assetPath, string fieldName)
            => File.Exists(assetPath) && !File.ReadAllText(assetPath).Contains($"  {fieldName}:");

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
