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
