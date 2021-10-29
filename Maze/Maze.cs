using Learning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Maze
{
    class Maze
    {
        public Maze()
        {
            // setup model
            Rows = 6;
            Columns = 8;
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
            Model = new Q<GridLocation, Direction>(
                    -0.04, // reward
                    0.5, // learning rate
                    1.0 // discount factor
                    );
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
            var act = Model.ChooseAction(
                current, 
                actions, 
                applyActionFunc: (loc, action) => { return loc.ApplyDirection(action); }
                );

            // apply action to current - getting a new current
            var next = current.ApplyDirection(act);

            if (!IsValid(next)) throw new Exception("ended up in an invalid location");

            // check if we are now in a terminal case
            var termValue = 0d;
            if (TryGetTerminal(next, out termValue))
            {
                // inform the model that we have hit a terminal state
                System.Diagnostics.Debug.WriteLine(termValue);
                Model.ApplyTerminalCondition(next, Direction.Center, termValue);
            }

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
            return Model.TryGetQValue(location, direction, out qvalue);
        }

        #region private
        private Q<GridLocation, Direction> Model;
        private HashSet<GridLocation> Invalid;
        private Dictionary<GridLocation, double> Terminal;
        #endregion
    }
}
