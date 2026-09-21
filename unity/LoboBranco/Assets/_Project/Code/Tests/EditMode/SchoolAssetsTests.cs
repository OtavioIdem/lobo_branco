using System.Collections.Generic;
using System.Text;
using LoboBranco.Combat;
using LoboBranco.Player;
using NUnit.Framework;
using UnityEditor;

namespace LoboBranco.Tests
{
    /// <summary>
    /// As escolas que o projeto de fato entrega, lidas do disco.
    ///
    /// Os testes do <c>SchoolDefTests</c> verificam a regra com assets de mentira. Estes
    /// verificam os assets de verdade, porque e neles que alguem vai errar: arrastar o golpe
    /// errado para uma vaga no Inspector e um erro de um clique, e o sintoma so aparece em
    /// rede, com duas pessoas.
    ///
    /// O ultimo e um teste de escopo, e esta aqui de proposito. O CLAUDE.md trava o slice em
    /// duas escolas, Lobo e Grifo, ate o portao M1 passar. Escopo crescendo e o risco X1 do
    /// docs/10, e uma terceira escola e a porta mais facil por onde ele entra: ela e so um
    /// asset. Um teste que falha e o freio que nao depende de alguem lembrar da regra.
    /// </summary>
    public sealed class SchoolAssetsTests
    {
        static readonly string[] DataFolders = { "Assets/_Project/Data" };

        /// <summary>O docs/13 secao 5 e o CLAUDE.md: Lobo e Grifo, e so.</summary>
        const int SliceSchoolLimit = 2;

        static List<SchoolDef> LoadSchools()
        {
            var schools = new List<SchoolDef>();

            foreach (string guid in AssetDatabase.FindAssets("t:SchoolDef", DataFolders))
            {
                var school = AssetDatabase.LoadAssetAtPath<SchoolDef>(AssetDatabase.GUIDToAssetPath(guid));
                if (school != null) schools.Add(school);
            }

            return schools;
        }

        [Test]
        public void O_projeto_tem_ao_menos_uma_escola()
        {
            Assert.IsNotEmpty(LoadSchools(),
                "Nenhum SchoolDef em Assets/_Project/Data. Rode 'Lobo Branco/Setup/6. Criar assets de combate'.");
        }

        [Test]
        public void Toda_escola_do_projeto_pode_ser_jogada()
        {
            var problems = new List<string>();
            var report = new StringBuilder();

            foreach (SchoolDef school in LoadSchools())
            {
                problems.Clear();
                school.CollectProblems(problems);

                for (int i = 0; i < problems.Count; i++)
                    report.Append(school.name).Append(": ").Append(problems[i]).Append('\n');
            }

            Assert.AreEqual(0, report.Length, report.ToString());
        }

        /// <summary>
        /// Intensidade zero nao da erro: todo sinal da escola cobra, acerta e sai com potencia
        /// zero (tarefa 1.18b). O bloco de atributos precisa de Inteligencia e do multiplicador.
        /// </summary>
        [Test]
        public void Toda_escola_do_projeto_conjura_com_intensidade()
        {
            CombatTuningDef tuning = LoadTuning();
            var report = new StringBuilder();

            foreach (SchoolDef school in LoadSchools())
            {
                if (school.statBlock == null) continue;

                float intensity = tuning.SignIntensity(school.statBlock.CreateSheet());

                if (intensity <= 0f)
                    report.Append(school.name).Append(": intensidade de sinal ").Append(intensity)
                        .Append(". Falta Intelligence ou SignIntensity no bloco de atributos.\n");
            }

            Assert.AreEqual(0, report.Length, report.ToString());
        }

        /// <summary>
        /// O Lobo e a referencia do docs/13 secao 5, o kit que ja existia. Os sinais dele saem com
        /// os numeros do asset, e e contra ele que a intensidade do Grifo vai ser medida na 1.33.
        /// </summary>
        [Test]
        public void O_Lobo_conjura_com_os_numeros_do_asset()
        {
            var wolf = AssetDatabase.LoadAssetAtPath<SchoolDef>(WolfPath);
            Assert.IsNotNull(wolf, $"Nao achei {WolfPath}.");

            Assert.AreEqual(1f, LoadTuning().SignIntensity(wolf.statBlock.CreateSheet()), 0.0001f);
        }

        const string WolfPath = "Assets/_Project/Data/Player/School_Wolf.asset";
        const string TuningPath = "Assets/_Project/Data/Combat/CombatTuning.asset";

        static CombatTuningDef LoadTuning()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<CombatTuningDef>(TuningPath);
            Assert.IsNotNull(tuning, $"Nao achei {TuningPath}.");

            return tuning;
        }

        [Test]
        public void O_slice_tem_no_maximo_duas_escolas()
        {
            int count = LoadSchools().Count;

            Assert.LessOrEqual(count, SliceSchoolLimit,
                $"Existem {count} escolas. O slice tem Lobo e Grifo, e a terceira so entra depois " +
                "do portao M1 (CLAUDE.md, Escopo travado; docs/13 secao 5). Se o portao passou, " +
                "atualize o limite deste teste junto com o documento.");
        }
    }
}
