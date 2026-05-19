using System;
using System.Globalization;
using System.Text;

namespace QLearningConsole
{
    public class Program
    {
        // Configs para exps (3 configs distintas de α, γ, ε)
        private static readonly ExperimentConfig[] configs =
        [
            new() { Alpha = 0.1,  Gamma = 0.95, EpsilonStart = 1.0, EpsilonEnd = 0.05, Episodes = 500 }, 
            new() { Alpha = 0.3,  Gamma = 0.85, EpsilonStart = 0.8, EpsilonEnd = 0.05, Episodes = 500 }, 
            new() { Alpha = 0.2,  Gamma = 0.65,  EpsilonStart = 1.0, EpsilonEnd = 0.1, Episodes = 500 },
        ];

        // 6 semillas distintas para cada config (total 18 exps)
        private static readonly int[] seeds = [42, 345, 123, 67, 839, 999];

        public static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("======================================================");
            Console.WriteLine("  Práctica 3 - Aprendizaje Reforzado (SARSA/Q-Learning)");
            Console.WriteLine("======================================================");
            Console.WriteLine();

            //var config = ReadConfig();

            // Entorno (Maze) ya implementado en Maze.cs
            //Maze env = Maze.BuildDefault5x5();
            Maze env = Maze.LoadFromFile("mapa_peligroso.txt");
            Console.WriteLine("Mapa:");
            Visualizer.PrintMaze(env);

            var expConfig = new ExperimentConfig { };
            List<ExperimentResult> qlResults = new List<ExperimentResult>(seeds.Length);
            List<ExperimentResult> sarsaResults = new List<ExperimentResult>(seeds.Length);

            for (int i = 0; i < configs.Length; i++)
            {
                for (int j = 0; j < seeds.Length; j++)
                {
                    int expNum = i * seeds.Length + j;

                    // actualizar config con nueva semilla
                    expConfig = configs[i];
                    expConfig.Seed = seeds[j];

                    //Console.WriteLine("======================================================");
                    //Console.WriteLine("Q-LEARNING (off-policy)");
                    //Console.WriteLine("======================================================");
                    //Console.WriteLine();
                    // run experiment
                    expConfig.Algo = Algorithm.QLearning;
                    var qlResult = RunExp(env, expConfig, expNum * 2);
                    qlResults.Add(qlResult);

                    //Console.WriteLine("======================================================");
                    //Console.WriteLine("SARSA (on-policy)");
                    //Console.WriteLine("======================================================");
                    //Console.WriteLine();
                    // change experiment and log data
                    expConfig.Algo = Algorithm.Sarsa;
                    var sarsaResult = RunExp(env, expConfig, expNum * 2 + 1);
                    sarsaResults.Add(sarsaResult);                
                }

                // name config
                Console.WriteLine($"Resultados promedio Config {i+1}: α={configs[i].Alpha}, γ={configs[i].Gamma}, ε={configs[i].EpsilonStart}→{configs[i].EpsilonEnd}");

                // export average reward and steps for this config all seeds
                Console.WriteLine("Exportando resultados promedio de steps y reward QLearning y Sarsa");
                ExportAverageRewardSteps(qlResults, $"avg_ql_config{i}.csv");
                ExportAverageRewardSteps(sarsaResults, $"avg_sarsa_config{i}.csv");

                // compare final results for this config
                PrintAverageComparison(qlResults, sarsaResults);

                // print averaged policy for config last seeds
                Console.WriteLine("Politica final QLearning");
                Visualizer.PrintAveragedPolicy(qlResults, env);
                Console.WriteLine("Politica final Sarsa");
                Visualizer.PrintAveragedPolicy(sarsaResults, env);

                // clear results for next config
                qlResults.Clear();
                sarsaResults.Clear();
            }

            //// Entorno (Maze) ya implementado en Maze.cs
            ////Maze env = Maze.BuildDefault5x5();
            //Maze env = Maze.LoadFromFile("mapa_peligroso.txt");
            //Console.WriteLine("Mapa:");
            //Visualizer.PrintMaze(env);

            //Console.WriteLine("======================================================");
            //Console.WriteLine("Q-LEARNING (off-policy)");
            //Console.WriteLine("======================================================");
            //Console.WriteLine();
            //// create and run experiment
            //var expConfig = new ExperimentConfig { };
            //ExperimentResult qlResult = RunExp(env, expConfig);

            //Console.WriteLine("======================================================");
            //Console.WriteLine("SARSA (on-policy)");
            //Console.WriteLine("======================================================");
            //Console.WriteLine();
            //// change experiment and log data
            //expConfig.Algo = Algorithm.Sarsa;
            //ExperimentResult sarsaResult = RunExp(env, expConfig);

            //// compare final results
            //PrintComparison(qlResult, sarsaResult);
        }

        private static void ExportAverageRewardSteps(List<ExperimentResult> results, string path)
        {
            int episodes = results[0].Episodes.Count;
            using var file = new StreamWriter(path);
            file.WriteLine("Episode,AvgReward,AvgSteps");

            for (int ep = 0; ep < episodes; ep++)
            {
                double avgReward = results.Average(r => r.Episodes[ep].Reward);
                double avgSteps = results.Average(r => r.Episodes[ep].Steps);
                file.WriteLine($"{ep},{avgReward},{avgSteps}");
            }
        }

        private static ExperimentConfig ReadConfig()
        {
            var config = new ExperimentConfig();

            config.Algo = ReadConfig("Algoritmo (Q-Learning | SARSA)", Algorithm.QLearning);
            config.Alpha = ReadConfig("Learning rate α", 0.1);
            config.Gamma = ReadConfig("Discount rate γ", 0.95);
            config.EpsilonStart = ReadConfig("Epsilon inicial", 1.0);
            config.EpsilonEnd = ReadConfig("Epsilon final", 0.05);
            config.Episodes = ReadConfig("Número de episodios", 500);
            config.Seed = ReadConfig("Semilla aleatoria", 42);

            return config;
        }

        private static T ReadConfig<T>(string prompt, T defaultValue)
        {
            Console.Write($"  {prompt} [{defaultValue}]: ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(input)) return defaultValue;
            try
            {
                return (T)Convert.ChangeType(input, typeof(T));
            }
            catch
            {
                Console.WriteLine($"  Entrada no válida. Usando valor por defecto: {defaultValue}");
                return defaultValue;
            }
        }

        private static void LogExp(Maze env, ExperimentConfig expConfig)
        {
            Console.WriteLine($"Algoritmo (Q-Learning | SARSA): {expConfig.Algo.ToString()}");
            Console.WriteLine($"Learning rate α: {expConfig.Alpha}");
            Console.WriteLine($"Discount rate γ: {expConfig.Gamma}");
            Console.WriteLine($"Epsilon inicial: {expConfig.EpsilonStart}");
            Console.WriteLine($"Epsilon final: {expConfig.EpsilonEnd}");
            Console.WriteLine($"Número de episodios: {expConfig.Episodes}");
            Console.WriteLine($"Recompensa meta: {env.RewardGoal}");
            Console.WriteLine($"Recompensa por movimiento: {env.RewardStep}");
            Console.WriteLine($"Semilla aleatoria: {expConfig.Seed}");
        }

        /// revisarrrrrr
        private static ExperimentResult RunExp(Maze env, ExperimentConfig expConfig, int num)
        {
            // log data
            //LogExp(env, expConfig);

            // run experiment and get results 
            //Console.WriteLine("\nComenzando entrenamiento");
            ExperimentResult result = Experiment.Run(env, expConfig);

            // show summary, qtable, heatmap, policy
            //Experiment.PrintSummary(result);
            //if (result.Agent != null)
            //{
            //    Visualizer.PrintHeatmap(result.Agent, env);
            //    Console.WriteLine();
            //    Visualizer.PrintPolicy(result.Agent, env);
            //    Console.WriteLine();
            //    Visualizer.PrintQTable(result.Agent, env);
            //}

            // export learning curve
            //Experiment.ExportCsv(result, $"learning_curve_{num}.csv");
            //Console.WriteLine($"\nResultados exportados a 'learning_curve_{num}.csv'");

            // return result for later comparison
            return result;
        }

        private static void PrintComparison(ExperimentResult ql, ExperimentResult sarsa)
        {
            Console.WriteLine("======================================================");
            Console.WriteLine("Comparativa Final");
            Console.WriteLine("======================================================");
            Console.WriteLine($"{"Métrica",-28} {"Q-Learning",12} {"SARSA",12}");
            Console.WriteLine(new string('-', 54));
            Console.WriteLine($"{"Éxito últimos N eps (%):",-28} {ql.SuccessRateLast,12:F2} {sarsa.SuccessRateLast,12:F2}");
            Console.WriteLine($"{"Pasos medios últimos N:",-28} {ql.AverageStepsLast,12:F2} {sarsa.AverageStepsLast,12:F2}");
            Console.WriteLine($"{"Recompensa media últimos N:",-28} {ql.AverageRewardLast,12:F2} {sarsa.AverageRewardLast,12:F2}");
            Console.WriteLine($"{"Tiempo entrenamiento (ms):",-28} {ql.ElapsedMs,12} {sarsa.ElapsedMs,12}");
            Console.WriteLine($"{"Total actualizaciones Q:",-28} {ql.TotalUpdates,12} {sarsa.TotalUpdates,12}");
            Console.WriteLine("");
        }
        private static void PrintAverageComparison(List<ExperimentResult> ql, List<ExperimentResult> sarsa)
        {
            double qlSuccessRate = ql.Average(r => r.SuccessRateLast);
            double qlAverageSteps = ql.Average(r => r.AverageStepsLast);
            double qlAverageReward = ql.Average(r => r.AverageRewardLast);
            double qlElapsedMs = ql.Average(r => r.ElapsedMs);
            double qlTotalUpdates = ql.Average(r => r.TotalUpdates);

            double sarsaSuccessRate = sarsa.Average(r => r.SuccessRateLast);
            double sarsaAverageSteps = sarsa.Average(r => r.AverageStepsLast);
            double sarsaAverageReward = sarsa.Average(r => r.AverageRewardLast);
            double sarsaElapsedMs = sarsa.Average(r => r.ElapsedMs);
            double sarsaTotalUpdates = sarsa.Average(r => r.TotalUpdates);

            Console.WriteLine("======================================================");
            Console.WriteLine("Comparativa Final");
            Console.WriteLine("======================================================");
            Console.WriteLine($"{"Métrica",-28} {"Q-Learning",12} {"SARSA",12}");
            Console.WriteLine(new string('-', 54));
            Console.WriteLine($"{"Éxito últimos N eps (%):",-28} {qlSuccessRate,12:F2} {sarsaSuccessRate,12:F2}");
            Console.WriteLine($"{"Pasos medios últimos N:",-28} {qlAverageSteps,12:F2} {sarsaAverageSteps,12:F2}");
            Console.WriteLine($"{"Recompensa media últimos N:",-28} {qlAverageReward,12:F2} {sarsaAverageReward,12:F2}");
            Console.WriteLine($"{"Tiempo entrenamiento (ms):",-28} {qlElapsedMs,12} {sarsaElapsedMs,12}");
            Console.WriteLine($"{"Total actualizaciones Q:",-28} {qlTotalUpdates,12} {sarsaTotalUpdates,12}");
            Console.WriteLine("");
        }
    }
}
