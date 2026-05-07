using System;

namespace QLearningConsole
{
    public enum Algorithm { QLearning, Sarsa }

    /// Agente de aprendizaje por refuerzo tabular.
    /// El alumno debe completar los métodos marcados con TODO.
    public class LearningAgent
    {
        public double[,] Q { get; }
        public Algorithm Algo { get; }
        public double Alpha { get; set; }
        public double Gamma { get; set; }
        public double Epsilon { get; set; }

        private readonly int _numStates;
        private readonly int _numActions;
        private readonly Random _rng;

        public LearningAgent(int numStates, int numActions, Algorithm algo,
                             double alpha, double gamma, double epsilon, int seed = 0)
        {
            _numStates = numStates;
            _numActions = numActions;
            Algo = algo;
            Alpha = alpha;
            Gamma = gamma;
            Epsilon = epsilon;
            _rng = new Random(seed);
            Q = new double[numStates, numActions];
        }

        /// Selección ε-greedy: con probabilidad ε elige acción aleatoria,
        /// en caso contrario la de mayor valor Q.
        public int SelectAction(int state)
        {
            // TODO: implementar ε-greedy
            if (_rng.NextDouble() < Epsilon)
            {
                return _rng.Next(_numActions);
            }
            return ArgMaxAction(state);
        }

        public int ArgMaxAction(int state)
        {
            // TODO: devolver el índice de acción con mayor Q[state, a]
            int bestAction = 0;
            double bestValue = Q[state, 0];

            for (int i = 1; i < _numActions; i++)
            {
                if (Q[state, i] > bestValue)
                {
                    bestValue = Q[state, i];
                    bestAction = i;
                }
            }

            return bestAction;
        }

        public double MaxQ(int state)
        {
            // TODO: devolver max_a Q[state, a]
            return Q[state, ArgMaxAction(state)];
        }

        /// Regla Q-Learning (off-policy):
        ///   Q(s,a) ← Q(s,a) + α · [ r + γ · max_a' Q(s',a') − Q(s,a) ]
        public void UpdateQLearning(int s, int a, double r, int sNext, bool done)
        {
            // TODO: implementar la actualización (cuidado con el caso terminal)
            double maxNextQ = 0;
            if (!done) maxNextQ = MaxQ(sNext);
            Q[s, a] = Q[s, a] + Alpha * (r + Gamma * maxNextQ - Q[s, a]);
        }

        /// Regla SARSA (on-policy):
        ///   Q(s,a) ← Q(s,a) + α · [ r + γ · Q(s',a') − Q(s,a) ]
        /// donde a' es la acción realmente elegida en s' por la política ε-greedy.
        public void UpdateSarsa(int s, int a, double r, int sNext, int aNext, bool done)
        {
            // TODO: implementar la actualización (cuidado con el caso terminal)
            double nextQ = 0;
            if (!done) nextQ = Q[sNext, aNext];
            Q[s, a] = Q[s, a] + Alpha * (r + Gamma * nextQ - Q[s, a]);
        }

        /// Ejecuta un episodio completo desde env.StartState hasta el final (terminal o maxSteps).
        /// Devuelve pasos, recompensa total y si se alcanzó la meta.
        public EpisodeResult RunEpisode(Maze env, int maxSteps = 500)
        {
            // TODO: bucle de episodio
            //  1) resetear state = env.StartState
            int state = env.StartState;
            //  2) elegir acción inicial con SelectAction
            int action = SelectAction(state);

            //  3) repetir:
            //       - llamar env.Step para obtener (next, r, done)
            //       - actualizar Q con UpdateQLearning o UpdateSarsa según Algo
            //       - si done: salir
            //       - sincronizar state / action para la siguiente iteración

            int steps = 0;
            double totalReward = 0.0;
            bool done = false;

            while (!done && steps < maxSteps)
            {
                var (nextState, r, isDone) = env.Step(state, (Action)action, _rng);
                totalReward += r;

                int nextAction = 0;
                if (!isDone)
                {
                    nextAction = SelectAction(nextState);
                }

                if (Algo == Algorithm.QLearning)
                {
                    UpdateQLearning(state, action, r, nextState, isDone);
                }
                else
                {
                    UpdateSarsa(state, action, r, nextState, nextAction, isDone);
                }

                state = nextState;
                action = nextAction;
                done = isDone;
                steps++;
            }

            return new EpisodeResult(steps, totalReward, done && env.KindOf(state) == CellKind.Goal);
        }
    }

    public readonly record struct EpisodeResult(int Steps, double Reward, bool Reached);
}
