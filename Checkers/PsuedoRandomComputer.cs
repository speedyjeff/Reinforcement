using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    internal class PsuedoRandomComputer : IPlayer
    {
        public PsuedoRandomComputer()
        {
            Memory = new Dictionary<string, Dictionary<string, int>>();
        }

        public Move ChooseAction(CheckersBoard board)
        {
            // get the available moves
            var moves = board.GetAvailableMoves();

            if (moves == null || moves.Count == 0) throw new Exception("no moves to choose from");

            // round robin through all the choices
            var context = AsString(board);
            if (!Memory.TryGetValue(context, out Dictionary<string, int> results))
            {
                results = new Dictionary<string, int>();
                foreach(var move in moves)
                {
                    var action = AsString(move);
                    results.Add(action, 0);
                }
                Memory.Add(context, results);
            }

            if (moves.Count != results.Count) throw new Exception("invalid set of moves");

            // choose the right move (the one with the lowest value)
            var min = Int32.MaxValue;
            var minAction = "";
            foreach(var kvp in results)
            {
                if (kvp.Value < min)
                {
                    min = kvp.Value;
                    minAction = kvp.Key;
                }
            }

            if (string.IsNullOrWhiteSpace(minAction)) throw new Exception("failed to get an action");

            // increment
            results[minAction]++;

            return AsMove(minAction);
        }

        public void Finish(CheckersBoard board, Side winner, Move lastMove)
        {
        }

        public void Save()
        {
        }

        #region private
        private Dictionary<string, Dictionary<string, int>> Memory;

        private string AsString(Move move)
        {
            return $"{move.Coordinate.Row}{move.Coordinate.Column}{(int)move.Direction}";
        }

        private string AsString(CheckersBoard board)
        {
            // encoded as: RowColumnDirection (eg. 000)
            return MinimalBoard.Create(board).AsString();
        }

        private Move AsMove(string action)
        {
            // encoded as: RowColumnDirection (eg. 000)
            if (string.IsNullOrWhiteSpace(action) || action.Length != 3) throw new Exception("failed to get a valid action");

            var parts = action.ToCharArray();
            var move = new Move()
            {
                Coordinate = new Coordinate()
                {
                    Row = (int)Char.GetNumericValue(parts[0]),
                    Column = (int)Char.GetNumericValue(parts[1])
                },
                Direction = (Direction)(int)Char.GetNumericValue(parts[2])
            };

            return move;
        }
        #endregion
    }
}
