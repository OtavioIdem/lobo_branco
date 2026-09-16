using System.Text;
using LoboBranco.Combat;
using LoboBranco.Stats;
using NUnit.Framework;
using UnityEditor;

namespace LoboBranco.Tests
{
    /// <summary>
    /// As criaturas que o projeto de fato entrega, lidas do disco.
    ///
    /// A lentidao do Yrden e um multiplicador sobre o <c>MoveSpeed</c> da folha (tarefa 1.18a), e
    /// um multiplicador sobre zero continua zero. O <c>EnemyAgent</c> trata zero como neutro para
    /// a criatura andar, entao uma especie sem o atributo anda normal e ignora toda lentidao, sem
    /// erro nenhum. O sintoma apareceria no playtest como "o Yrden nao pega nesse bicho".
    /// </summary>
    public sealed class MonsterAssetsTests
    {
        static readonly string[] DataFolders = { "Assets/_Project/Data" };

        [Test]
        public void Toda_criatura_do_projeto_tem_velocidade_na_folha()
        {
            var report = new StringBuilder();

            foreach (string guid in AssetDatabase.FindAssets("t:MonsterDef", DataFolders))
            {
                var monster = AssetDatabase.LoadAssetAtPath<MonsterDef>(AssetDatabase.GUIDToAssetPath(guid));

                // O perfil do bruxo e um MonsterDef sem bloco: os atributos dele vem da escola.
                if (monster == null || monster.statBlock == null) continue;

                if (monster.statBlock.CreateSheet().GetBase(StatType.MoveSpeed) <= 0f)
                    report.Append(monster.name).Append(": sem MoveSpeed em ").Append(monster.statBlock.name).Append('\n');
            }

            Assert.AreEqual(0, report.Length,
                report + "Rode 'Lobo Branco/Setup/6. Criar assets de combate', que preenche o que falta.");
        }

        /// <summary>
        /// O barghest e leve, e o abridor o derruba (tarefa 1.18c). O docs/03 nao diz o porte dele; a
        /// composicao Matilha da secao 10 existe para ensinar o abridor, e um barghest medio so seria
        /// atordoado. O zero do enum e medio, entao um asset que perdeu o campo cai la sem erro.
        /// </summary>
        [Test]
        public void O_barghest_e_leve()
        {
            const string BarghestPath = "Assets/_Project/Data/Monsters/Monster_Barghest.asset";
            var barghest = AssetDatabase.LoadAssetAtPath<MonsterDef>(BarghestPath);

            Assert.IsNotNull(barghest, $"Nao achei {BarghestPath}.");
            Assert.AreEqual(BodyWeight.Light, barghest.bodyWeight);
        }
    }
}
