using System.Collections.Generic;
using System.Text;
using LoboBranco.Combat;
using NUnit.Framework;
using UnityEditor;

namespace LoboBranco.Tests
{
    /// <summary>
    /// As habilidades que o projeto de fato entrega, lidas do disco (tarefa 1.32).
    ///
    /// O <c>SchoolAssetsTests</c> ja confere as habilidades que estao numa escola. Este confere
    /// todas, porque uma habilidade solta em <c>Data</c> e a que alguem vai arrastar para a
    /// escola do Grifo na tarefa 1.33 sem abrir o asset.
    /// </summary>
    public sealed class AbilityAssetsTests
    {
        static readonly string[] DataFolders = { "Assets/_Project/Data" };

        const string KnockbackPath = "Assets/_Project/Data/Combat/Abilities/Sign_Knockback.asset";

        [Test]
        public void Toda_habilidade_do_projeto_pode_ser_usada()
        {
            var problems = new List<string>();
            var report = new StringBuilder();

            foreach (string guid in AssetDatabase.FindAssets("t:AbilityDef", DataFolders))
            {
                var ability = AssetDatabase.LoadAssetAtPath<AbilityDef>(AssetDatabase.GUIDToAssetPath(guid));
                if (ability == null) continue;

                problems.Clear();
                ability.CollectProblems(problems);

                for (int i = 0; i < problems.Count; i++)
                    report.Append(ability.name).Append(": ").Append(problems[i]).Append('\n');
            }

            Assert.AreEqual(0, report.Length, report.ToString());
        }

        /// <summary>
        /// Os dois numeros do docs/03 secao 8, repetidos de proposito: se o asset mudar sem o
        /// documento mudar, o teste avisa. O tempo de conjurar nao esta aqui porque nao esta no
        /// documento.
        /// </summary>
        [Test]
        public void O_abridor_tem_o_custo_e_a_recarga_do_documento()
        {
            var knockback = AssetDatabase.LoadAssetAtPath<AbilityDef>(KnockbackPath);

            Assert.IsNotNull(knockback, $"Nao achei {KnockbackPath}. Rode 'Lobo Branco/Setup/6. Criar assets de combate'.");
            Assert.AreEqual(30f, knockback.staminaCost, "docs/03 secao 8: custo 30.");
            Assert.AreEqual(4f, knockback.cooldownSeconds, "docs/03 secao 8: recarga de 4 s.");
        }

        const string FirePath = "Assets/_Project/Data/Combat/Abilities/Sign_Fire.asset";
        const string WolfPath = "Assets/_Project/Data/Player/School_Wolf.asset";

        /// <summary>
        /// A linha do fogo no docs/03 secao 8 (tarefa 1.18d): custo, recarga, cone de 5 m, dano 0,8x e
        /// Queimadura de 4 por segundo por 5 s. A abertura nao esta no documento e nao e conferida.
        /// </summary>
        [Test]
        public void O_fogo_tem_os_numeros_do_documento()
        {
            var fire = AssetDatabase.LoadAssetAtPath<AbilityDef>(FirePath);
            Assert.IsNotNull(fire, $"Nao achei {FirePath}. Rode 'Lobo Branco/Setup/6. Criar assets de combate'.");

            Assert.AreEqual(35f, fire.staminaCost, "docs/03 secao 8: custo 35.");
            Assert.AreEqual(5f, fire.cooldownSeconds, "docs/03 secao 8: recarga de 5 s.");
            Assert.AreEqual(SignAreaShape.Cone, fire.area.shape);
            Assert.AreEqual(5f, fire.area.range, "docs/03 secao 8: cone de 5 m.");

            DamageEffectDef damage = null;
            BurnEffectDef burn = null;

            if (fire.effects != null)
                foreach (SignEffectDef effect in fire.effects)
                {
                    if (effect is DamageEffectDef d) damage = d;
                    if (effect is BurnEffectDef b) burn = b;
                }

            Assert.IsNotNull(damage, "O fogo nao fere.");
            Assert.IsNotNull(burn, "O fogo nao queima.");
            Assert.AreEqual(0.8f, damage.weaponDamageMultiplier);
            Assert.AreEqual(DamageType.Fire, damage.damageType);
            Assert.AreEqual(4f, burn.damagePerSecond);
            Assert.AreEqual(5f, burn.seconds);
        }

        /// <summary>A linha do escudo no docs/03 secao 8 (tarefa 1.18e): 25 de vigor, 6 s, 8 s e 30%.</summary>
        [Test]
        public void O_escudo_tem_os_numeros_do_documento()
        {
            const string WardPath = "Assets/_Project/Data/Combat/Abilities/Sign_Ward.asset";
            var ward = AssetDatabase.LoadAssetAtPath<AbilityDef>(WardPath);
            Assert.IsNotNull(ward, $"Nao achei {WardPath}. Rode 'Lobo Branco/Setup/6. Criar assets de combate'.");

            Assert.AreEqual(25f, ward.staminaCost, "docs/03 secao 8: custo 25.");
            Assert.AreEqual(6f, ward.cooldownSeconds, "docs/03 secao 8: recarga de 6 s.");
            Assert.AreEqual(SignAreaShape.Self, ward.area.shape, "O escudo e em quem conjura.");

            WardEffectDef effect = null;
            if (ward.effects != null)
                foreach (SignEffectDef e in ward.effects)
                    if (e is WardEffectDef found) effect = found;

            Assert.IsNotNull(effect, "O escudo nao ergue nada.");
            Assert.AreEqual(8f, effect.seconds, "docs/03 secao 8: por 8 s.");
            Assert.AreEqual(0.3f, effect.reflectFraction, "docs/03 secao 8: devolve 30%.");
        }

        /// <summary>
        /// A linha da armadilha no docs/03 secao 8 (tarefa 1.18f): 35 de vigor, 8 s de recarga, 4 m
        /// por 12 s e lentidao de 60%. O raio e a lentidao moram no prefab, que e o mesmo em todas as
        /// maquinas, e por isso sao conferidos la.
        /// </summary>
        [Test]
        public void A_armadilha_tem_os_numeros_do_documento()
        {
            const string TrapSignPath = "Assets/_Project/Data/Combat/Abilities/Sign_Trap.asset";
            var sign = AssetDatabase.LoadAssetAtPath<AbilityDef>(TrapSignPath);
            Assert.IsNotNull(sign, $"Nao achei {TrapSignPath}. Rode 'Lobo Branco/Setup/6. Criar assets de combate'.");

            Assert.AreEqual(35f, sign.staminaCost, "docs/03 secao 8: custo 35.");
            Assert.AreEqual(8f, sign.cooldownSeconds, "docs/03 secao 8: recarga de 8 s.");
            Assert.AreEqual(SignAreaShape.Self, sign.area.shape, "A armadilha nasce onde o bruxo esta.");

            TrapEffectDef trap = null;
            if (sign.effects != null)
                foreach (SignEffectDef e in sign.effects)
                    if (e is TrapEffectDef found) trap = found;

            Assert.IsNotNull(trap, "O sinal nao deixa armadilha nenhuma.");
            Assert.AreEqual(12f, trap.seconds, "docs/03 secao 8: por 12 s.");
            Assert.IsNotNull(trap.trapPrefab,
                "A armadilha nao tem prefab. Rode 'Lobo Branco/Setup/5. Montar sandbox de combate'.");

            var body = trap.trapPrefab.GetComponent<SignTrap>();
            Assert.IsNotNull(body);
            Assert.AreEqual(4f, body.Radius, "docs/03 secao 8: 4 m.");
            Assert.AreEqual(0.6f, body.SlowFraction, "docs/03 secao 8: lentidao de 60%.");
        }

        [Test]
        public void O_Lobo_tem_o_fogo_numa_vaga()
        {
            var wolf = AssetDatabase.LoadAssetAtPath<LoboBranco.Player.SchoolDef>(WolfPath);
            var fire = AssetDatabase.LoadAssetAtPath<AbilityDef>(FirePath);

            Assert.IsNotNull(wolf);
            Assert.IsNotNull(fire);
            CollectionAssert.Contains(wolf.abilities, fire, "Todas as escolas tem os cinco sinais (docs/13 secao 5).");
        }

        /// <summary>A forma e o alcance sao do docs/03 secao 8. A abertura nao esta la, e nao e conferida.</summary>
        [Test]
        public void O_abridor_alcanca_o_cone_do_documento()
        {
            var knockback = AssetDatabase.LoadAssetAtPath<AbilityDef>(KnockbackPath);

            Assert.IsNotNull(knockback, $"Nao achei {KnockbackPath}.");
            Assert.AreEqual(SignAreaShape.Cone, knockback.area.shape, "docs/03 secao 8: cone.");
            Assert.AreEqual(6f, knockback.area.range, "docs/03 secao 8: 6 m.");
        }

        /// <summary>
        /// "Derruba leves, atordoa medios 1,5 s", do docs/03 secao 8 (tarefa 1.18c). A duracao da
        /// derrubada nao esta no documento e nao e conferida; o tipo de controle de cada porte e.
        /// </summary>
        [Test]
        public void O_abridor_derruba_leves_e_atordoa_medios_como_o_documento()
        {
            var knockback = AssetDatabase.LoadAssetAtPath<AbilityDef>(KnockbackPath);
            Assert.IsNotNull(knockback, $"Nao achei {KnockbackPath}.");

            ControlEffectDef control = null;
            if (knockback.effects != null)
                foreach (SignEffectDef effect in knockback.effects)
                    if (effect is ControlEffectDef found)
                        control = found;

            Assert.IsNotNull(control, "O abridor nao tem efeito de controle. Rode o setup de assets de combate.");
            Assert.AreEqual(ControlKind.KnockedDown, control.light.control);
            Assert.AreEqual(ControlKind.Stunned, control.medium.control);
            Assert.AreEqual(1.5f, control.medium.seconds);
            Assert.IsFalse(control.heavy.DoesSomething);
        }
    }
}
