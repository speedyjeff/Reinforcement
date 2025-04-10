using Learning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Maze
{
    public enum ModelType { Q, DeepQ, None };

    class Maze
    {
        public Maze(ModelType type, int rows, int columns)
        {
            // setup model
            TerminalCount = 0;
            Rows = rows;
            Columns = columns;
            Invalid = new HashSet<GridLocation>() // invalid regions
                        {
                            { new GridLocation() { Row = 1, Column = 1} },
                            { new GridLocation() { Row = 2, Column = 2} },
                            { new GridLocation() { Row = 3, Column = 3} },
                            { new GridLocation() { Row = 4, Column = 4} }
                        };
            Terminal = new Dictionary<GridLocation, double>() // terminal regions
                        {
                        { new GridLocation() { Row = 5, Column = 6}, 1}
                        };

            // choose a model to use
            if (type == ModelType.Q)
            {
                QModel = new Q<GridLocation, Direction>(
                        reward: -0.04, // reward
                        learning: 0.5, // learning rate
                        discount: 1.0 // discount factor
                        );
            }
            else if (type == ModelType.DeepQ)
            {
                DeepQModel = new DeepQ(new DeepQOptions()
                {
                    Reward = -0.04f,
                    Learning = 0.001f,
                    Discount = 1f,
                    Epsilon = 1f,
                    BatchSize = 10,
                    MemoryMaxSize = 100,
                    ContextNum = Rows * Columns,
                    ActionNum = (int)Direction.MAX
                });
            }
            else throw new Exception("invalid model type");
        }

        public int Rows { get; private set; }
        public int Columns { get; private set; }

        public GridLocation MakeMove(GridLocation current)
        {
            // make a list of available actions at this point
            var actions = new List<Direction>();
            foreach (var d in new Direction[] { Direction.Down, Direction.Left, Direction.Right, Direction.Up })
            {
                var loc = current.ApplyDirection(d);
                if (IsValid(loc)) actions.Add(d);
            }

            if (actions.Count == 0) throw new Exception("all invalid moves from here");

            // choose an action
            Direction act = Direction.Up;
            if (QModel != null)
            {
                act = QModel.ChooseAction(
                    current,
                    actions,
                    applyActionFunc: (loc, action) => { return loc.ApplyDirection(action); }
                    );
            }
            else if (DeepQModel != null)
            {
                // convert for DeepQ (all int)
                var actionsInt = new List<int>();
                foreach (var d in actions) actionsInt.Add((int)d);
                act = (Direction)DeepQModel.ChooseAction(current.ToInt32(), actionsInt);
            }
            else throw new Exception("no model to use");

            // apply action to current - getting a new current
            var next = current.ApplyDirection(act);

            if (!IsValid(next)) throw new Exception("ended up in an invalid location");
            if (next == current) throw new Exception("non-move occurred which is illegal");

            // check if we are now in a terminal case
            var termValue = 0d;
            var isTerm = TryGetTerminal(next, out termValue);
            if (isTerm) TerminalCount++;
            if (QModel != null)
            {
                if (isTerm)
                {
                    // inform the model that we have hit a terminal state
                    QModel.ApplyTerminalCondition(next, Direction.Center, termValue);
                }
            }
            else if (DeepQModel != null)
            {
                // deepQ models are subject to overfitting, after TerminalCountMax wins, stop training
                if (TerminalCount < TerminalCountMax)
                {
                    // inform the model of the action taken
                    DeepQModel.Remember(current.ToInt32(), (int)act, next.ToInt32(), isTerm ? (float)termValue : DeepQModel.Reward);
                }
            }
            else throw new Exception("no model to use");

            // return the next
            return next;
        }

        public bool IsValid(GridLocation location)
        {
            if (location.Row < 0 || location.Row >= Rows ||
                location.Column < 0 || location.Column >= Columns) return false;
            return !Invalid.Contains(location);
        }

        public bool TryGetTerminal(GridLocation location, out double termvalue)
        {
            termvalue = 0d;
            if (!IsValid(location)) return false;
            return Terminal.TryGetValue(location, out termvalue);
        }

        public bool TryGetQValue(GridLocation location, Direction direction, out double qvalue)
        {
            qvalue = 0d;
            if (!IsValid(location)) return false;
            if (QModel != null) return QModel.TryGetQValue(location, direction, out qvalue);
            else if (DeepQModel != null)
            {
                // todo performance of calling this so often?
                var probabilities = DeepQModel.GetProbabilities(location.ToInt32());
                qvalue = probabilities[(int)direction];
                return true;
            }
            else throw new Exception("no model to use");
        }

        #region private
        private Q<GridLocation, Direction> QModel;
        private DeepQ DeepQModel;
        private HashSet<GridLocation> Invalid;
        private Dictionary<GridLocation, double> Terminal;
        private int TerminalCount;
        private const int TerminalCountMax = 500;
        #endregion
    }
}
