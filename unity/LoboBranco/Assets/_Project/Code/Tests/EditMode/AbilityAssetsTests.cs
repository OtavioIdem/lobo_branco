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

        /// <summary>A forma e o alcance sao do docs/03 secao 8. A abertura nao esta la, e nao e conferida.</summary>
        [Test]
        public void O_abridor_alcanca_o_cone_do_documento()
        {
            var knockback = AssetDatabase.LoadAssetAtPath<AbilityDef>(KnockbackPath);

            Assert.IsNotNull(knockback, $"Nao achei {KnockbackPath}.");
            Assert.AreEqual(SignAreaShape.Cone, knockback.area.shape, "docs/03 secao 8: cone.");
            Assert.AreEqual(6f, knockback.area.range, "docs/03 secao 8: 6 m.");
        }
    }
}
