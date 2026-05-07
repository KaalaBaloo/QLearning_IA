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
            Maze env = Maze.BuildDefault5x5();
            Console.WriteLine("Mapa:");
            Visualizer.PrintMaze(env);

            // TODO:
            //  1. Leer del usuario: algoritmo (SARSA/Q-Learning), α, γ, recompensas,
            //     nº de episodios, semilla.
            //  2. Llamar Experiment.Run.
            //  3. Mostrar la política y la Q-Table final.
            //  4. (Opcional) Exportar curva de aprendizaje a CSV para graficar.

            // create experiment and log data
            var expConfig = new ExperimentConfig { };
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

            void LogExp(Maze env, ExperimentConfig expConfig)
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
        }
    }
}
