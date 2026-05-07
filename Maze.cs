using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QLearningConsole
{
    public enum Action { Up = 0, Right = 1, Down = 2, Left = 3 }

    public enum CellKind
    {
        Empty,
        Goal,
        Pit,
        Coin
    }

    public class Maze
    {
        public int Rows { get; }
        public int Cols { get; }
        public int NumStates => Rows * Cols;
        public int NumActions => 4;

        public int StartState { get; }
        public int GoalState { get; }

        private readonly bool[,] _wall;
        private readonly CellKind[] _kind;
        private readonly double[] _cellBonus;
        private readonly double[] _cellPitProb;

        public double RewardGoal { get; set; } = 100.0;
        public double RewardStep { get; set; } = -1.0;
        public double RewardPit { get; set; } = -100.0;
        public double RewardCoin { get; set; } = 10.0;

        public Maze(int rows, int cols, int startState, int goalState)
        {
            Rows = rows;
            Cols = cols;
            StartState = startState;
            GoalState = goalState;

            _wall = new bool[NumStates, 4];
            _kind = new CellKind[NumStates];
            _cellBonus = new double[NumStates];
            _cellPitProb = new double[NumStates];

            _kind[goalState] = CellKind.Goal;

            BuildOuterWalls();
        }

        private void BuildOuterWalls()
        {
            for (int s = 0; s < NumStates; s++)
            {
                (int r, int c) = ToRowCol(s);
                if (r == 0) _wall[s, (int)Action.Up] = true;
                if (r == Rows - 1) _wall[s, (int)Action.Down] = true;
                if (c == 0) _wall[s, (int)Action.Left] = true;
                if (c == Cols - 1) _wall[s, (int)Action.Right] = true;
            }
        }

        public (int r, int c) ToRowCol(int state) => (state / Cols, state % Cols);
        public int ToState(int r, int c) => r * Cols + c;

        public void AddWall(int state, Action a)
        {
            _wall[state, (int)a] = true;
            (int nr, int nc) = NeighbourRowCol(state, a);
            if (nr >= 0 && nr < Rows && nc >= 0 && nc < Cols)
            {
                int neighbour = ToState(nr, nc);
                _wall[neighbour, (int)Opposite(a)] = true;
            }
        }

        public void SetPit(int state, double probKill = 1.0)
        {
            _kind[state] = CellKind.Pit;
            _cellPitProb[state] = probKill;
        }

        public void SetCoin(int state, double bonus = 10.0)
        {
            _kind[state] = CellKind.Coin;
            _cellBonus[state] = bonus;
        }

        public CellKind KindOf(int state) => _kind[state];
        public bool IsTerminal(int state) => _kind[state] == CellKind.Goal;
        public bool IsWall(int state, Action a) => _wall[state, (int)a];

        public (int nextState, double reward, bool done) Step(int state, Action a, Random rng)
        {
            if (IsWall(state, a))
                return (state, RewardStep, false);

            (int nr, int nc) = NeighbourRowCol(state, a);
            int next = ToState(nr, nc);

            double r = RewardStep;
            bool done = false;

            switch (_kind[next])
            {
                case CellKind.Goal:
                    r = RewardGoal;
                    done = true;
                    break;
                case CellKind.Pit:
                    if (rng.NextDouble() < _cellPitProb[next])
                    {
                        r = RewardPit;
                        done = true;
                    }
                    break;
                case CellKind.Coin:
                    r = RewardCoin + RewardStep;
                    break;
            }

            return (next, r, done);
        }

        private (int r, int c) NeighbourRowCol(int state, Action a)
        {
            (int r, int c) = ToRowCol(state);
            return a switch
            {
                Action.Up => (r - 1, c),
                Action.Right => (r, c + 1),
                Action.Down => (r + 1, c),
                Action.Left => (r, c - 1),
                _ => (r, c)
            };
        }

        private static Action Opposite(Action a) => a switch
        {
            Action.Up => Action.Down,
            Action.Down => Action.Up,
            Action.Left => Action.Right,
            Action.Right => Action.Left,
            _ => a
        };

        public static Maze BuildDefault5x5()
        {
            var m = new Maze(5, 5, startState: 0, goalState: 24);
            m.AddWall(m.ToState(0, 1), Action.Right);
            m.AddWall(m.ToState(1, 0), Action.Right);
            m.AddWall(m.ToState(1, 2), Action.Down);
            m.AddWall(m.ToState(2, 1), Action.Right);
            m.AddWall(m.ToState(2, 3), Action.Down);
            m.AddWall(m.ToState(3, 0), Action.Right);
            m.AddWall(m.ToState(3, 2), Action.Right);
            m.AddWall(m.ToState(4, 1), Action.Up);
            return m;
        }

        public static Maze BuildDefault5x5WithHazards()
        {
            var m = BuildDefault5x5();
            m.SetPit(m.ToState(2, 2), probKill: 0.5);
            m.SetCoin(m.ToState(0, 4), bonus: 15.0);
            m.SetCoin(m.ToState(3, 1), bonus: 5.0);
            return m;
        }

        public static Maze LoadFromFile(string path)
        {
            string[] lines = File.ReadAllLines(path)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#"))
                .ToArray();

            if (lines.Length < 2)
                throw new InvalidDataException("El fichero de mapa debe tener cabecera y al menos una fila.");

            var header = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int rows = int.Parse(header[0]);
            int cols = int.Parse(header[1]);

            int start = 0, goal = rows * cols - 1;
            var pits = new List<(int s, double p)>();
            var coins = new List<(int s, double b)>();

            int gridStart = 1;
            for (int i = 1; i < lines.Length && lines[i].StartsWith("@"); i++)
            {
                var tok = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int r = int.Parse(tok[1]);
                int c = int.Parse(tok[2]);
                int state = r * cols + c;
                switch (tok[0])
                {
                    case "@start": start = state; break;
                    case "@goal": goal = state; break;
                    case "@pit": pits.Add((state, double.Parse(tok[3], System.Globalization.CultureInfo.InvariantCulture))); break;
                    case "@coin": coins.Add((state, double.Parse(tok[3], System.Globalization.CultureInfo.InvariantCulture))); break;
                }
                gridStart = i + 1;
            }

            var maze = new Maze(rows, cols, start, goal);
            int rowIdx = 0;
            for (int i = gridStart; i < lines.Length && rowIdx < rows; i++)
            {
                string line = lines[i].TrimEnd();
                for (int c = 0; c < cols && c < line.Length; c++)
                {
                    char ch = line[c];
                    int s = maze.ToState(rowIdx, c);
                    if (ch == 'U') maze.AddWall(s, Action.Up);
                    if (ch == 'R') maze.AddWall(s, Action.Right);
                    if (ch == 'D') maze.AddWall(s, Action.Down);
                    if (ch == 'L') maze.AddWall(s, Action.Left);
                }
                rowIdx++;
            }

            foreach (var (s, p) in pits) maze.SetPit(s, p);
            foreach (var (s, b) in coins) maze.SetCoin(s, b);

            return maze;
        }
    }
}
