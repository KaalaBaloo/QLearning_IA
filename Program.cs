using System;
using System.Globalization;

namespace QLearningConsole
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("======================================================");
            Console.WriteLine("  Práctica 3 - Aprendizaje Reforzado (SARSA/Q-Learning)");
            Console.WriteLine("======================================================");
            Console.WriteLine();

            // Entorno (Maze) ya implementado en Maze.cs
            //Maze env = Maze.BuildDefault5x5();
            Maze env = Maze.LoadFromFile("mapa_peligroso.txt");
            Console.WriteLine("Mapa:");
            Visualizer.PrintMaze(env);

            Console.WriteLine("======================================================");
            Console.WriteLine("Q-LEARNING (off-policy)");
            Console.WriteLine("======================================================");
            Console.WriteLine();
            // create and run experiment
            var expConfig = new ExperimentConfig { };
            ExperimentResult qlResult = RunExp(env, expConfig);

            Console.WriteLine("======================================================");
            Console.WriteLine("SARSA (on-policy)");
            Console.WriteLine("======================================================");
            Console.WriteLine();
            // change experiment and log data
            expConfig.Algo = Algorithm.Sarsa;
            ExperimentResult sarsaResult = RunExp(env, expConfig);

            // compare final results
            PrintComparison(qlResult, sarsaResult);
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

        private static ExperimentResult RunExp(Maze env, ExperimentConfig expConfig)
        {
            // log data
            LogExp(env, expConfig);

            // run experiment and get results 
            Console.WriteLine("\nComenzando entrenamiento");
            ExperimentResult result = Experiment.Run(env, expConfig);

            // show summary, qtable, heatmap, policy
            Experiment.PrintSummary(result);
            if (result.Agent != null)
            {
                Visualizer.PrintHeatmap(result.Agent, env);
                Console.WriteLine();
                Visualizer.PrintPolicy(result.Agent, env);
                Console.WriteLine();
                Visualizer.PrintQTable(result.Agent, env);
            }

            // export learning curve
            Experiment.ExportCsv(result, "learning_curve.csv");
            Console.WriteLine("\nResultados exportados a 'learning_curve.csv'");

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
        }
    }
}
