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
    }
}
