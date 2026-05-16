using System.Diagnostics;

namespace QLearningConsole
{
    /// Configuración de un experimento de entrenamiento.
    public class ExperimentConfig
    {
        public Algorithm Algo { get; set; } = Algorithm.QLearning;
        public double Alpha { get; set; } = 0.1;
        public double Gamma { get; set; } = 0.95;
        public double EpsilonStart { get; set; } = 1.0;
        public double EpsilonEnd { get; set; } = 0.05;
        public int Episodes { get; set; } = 500;
        public int MaxStepsPerEpisode { get; set; } = 500;
        public int Seed { get; set; } = 42;
    }

    /// Resultados agregados de un experimento.
    public class ExperimentResult
    {
        public ExperimentConfig Config { get; set; } = new();
        public List<EpisodeResult> Episodes { get; } = new();
        public long ElapsedMs { get; set; }
        public int TotalUpdates { get; set; }
        public LearningAgent? Agent { get; set; }
        public int resumeIterations { get; set; } = 50;

        // TODO: añadir métricas útiles (SuccessRateLast, AverageStepsLast, AverageRewardLast)
        public double SuccessRateLast
        {
            get 
            {
                //get last x iterations
                int episodesCount = Math.Min(resumeIterations, Episodes.Count);
                if (episodesCount == 0) return 0;

                // get episodes and get success
                var lastEpisodes = Episodes.Skip(Episodes.Count - episodesCount);
                int success = lastEpisodes.Count(e => e.Reached);

                // calculate success rate
                return (double)success / episodesCount * 100;

            }
        }

        public double AverageStepsLast
        {
            get
            {
                int episodesCount = Math.Min(resumeIterations, Episodes.Count);
                if (episodesCount == 0) return 0;

                // get episodes and get average steps
                var lastEpisodes = Episodes.Skip(Episodes.Count - episodesCount);
                double averageSteps = lastEpisodes.Average(e => e.Steps);
                return averageSteps;
            }
        }

        public double AverageRewardLast
        {
            get
            {
                int episodesCount = Math.Min(resumeIterations, Episodes.Count);
                if (episodesCount == 0) return 0;

                // get episodes and get average reward
                var lastEpisodes = Episodes.Skip(Episodes.Count - episodesCount);
                double averageSteps = lastEpisodes.Average(e => e.Reward);
                return averageSteps;
            }
        }
    }

    public static class Experiment
    {
        /// Entrena un agente durante N episodios. Opcional: decaer ε linealmente de EpsilonStart a EpsilonEnd.
        public static ExperimentResult Run(Maze env, ExperimentConfig cfg)
        {
            // TODO: crear LearningAgent con (env.NumStates, env.NumActions, algo, alpha, gamma, epsilon, seed)    
            var agent = new LearningAgent(
              env.NumStates, env.NumActions, cfg.Algo,
              cfg.Alpha, cfg.Gamma, cfg.EpsilonStart, cfg.Seed);

            var result = new ExperimentResult
            {
                Config = cfg,
                Agent = agent
            };

            double epsilonDecay = (cfg.EpsilonStart - cfg.EpsilonEnd) / Math.Max(1, cfg.Episodes - 1);
            var timer = Stopwatch.StartNew();

            // TODO: bucle de episodios, decaer epsilon, acumular EpisodeResult, cronometrar con Stopwatch
            for (int i = 0; i < cfg.Episodes; i++)
            {
                // run episode
                EpisodeResult epResult = agent.RunEpisode(env, cfg.MaxStepsPerEpisode);
                result.Episodes.Add(epResult);
                result.TotalUpdates += epResult.Steps;

                // epsilon decay
                agent.Epsilon = Math.Max(cfg.EpsilonEnd, agent.Epsilon - epsilonDecay);
            }

            timer.Stop();
            result.ElapsedMs = timer.ElapsedMilliseconds;

            return result;
        }

        /// Volcar curva de aprendizaje a CSV (episode,steps,reward,reached) para graficar en Excel.
        public static void ExportCsv(ExperimentResult r, string path)
        {
            // TODO: escribir cabecera y una fila por episodio
            using var file = new StreamWriter(path);
            file.WriteLine("Episode,Steps,Reward,Reached");

            for (int i = 0; i < r.Episodes.Count; i++)
            {
                var ep = r.Episodes[i];
                file.WriteLine($"{i},{ep.Steps},{ep.Reward},{(ep.Reached ? 1 : 0)}");
            }
        }

        /// Resumen por consola: algoritmo, episodios, tiempo, pasos/reward/éxito en ventana final.
        public static void PrintSummary(ExperimentResult r)
        {
            // TODO
            Console.WriteLine("Resumen del Entrenamiento");
            Console.WriteLine($"Algoritmo: {r.Config.Algo}");
            Console.WriteLine($"Episodios: {r.Config.Episodes}");
            Console.WriteLine($"Tiempo (ms): {r.ElapsedMs} ms");
            Console.WriteLine($"Total Updates: {r.TotalUpdates}");
            Console.WriteLine($"Métricas ultimas {r.resumeIterations} iteraciones");
            Console.WriteLine($"Éxito (%): {r.SuccessRateLast:F2}%");
            Console.WriteLine($"Pasos medios: {r.AverageStepsLast:F2}");
            Console.WriteLine($"Recompensa: {r.AverageRewardLast:F2}\n");
        }
    }
}
