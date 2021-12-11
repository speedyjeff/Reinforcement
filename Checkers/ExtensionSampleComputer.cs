using Learning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    internal class ExtensionSampleComputer : IPlayer
    {
        // reward     : cost of taking an action (default -0.04)
        // learning   : 0 [no learning/rely on prior knowledge] ... [1] only most recent info (default 0.5)
        // discount   : 0 [short term rewards] ... 1 [long term rewards] (default 1)
        public ExtensionSampleComputer(Side side, double reward, double learning, double discount)
        {
            Side = side;

            // attempt to load the model from disk
            Model = QLearner.Load(filename: "model.tsv");

            if (Model == null)
            {
                Model = new QLearner(reward, learning, discount);
            }
        }

        //
        // change the signature to match the local types
        //
        public Move ChooseAction(CheckersBoard board)
        {
            //
            // convert the board and available acionts into strings (for the model)
            //
            var context = BoardToContext(board);

            // encode all the possible moves
            var actions = new List<string>();
            foreach (var move in board.GetAvailableMoves())
            {
                // translate into action
                var laction = MoveToAction(move);
                actions.Add(laction);
            }

            if (actions.Count == 0) throw new Exception("must have at least 1 valid move");

            // call the model to make a decison
            var action = Model.ChooseAction(context, actions);

            //
            // convert the string action back into the local type
            //
            // return the action
            return ActionToMove(action);
        }

        //
        // change the signature to match the local types
        //
        public void Finish(CheckersBoard board, Side winner, Move lastMove)
        {
            //
            // convert the board and available acionts into strings (for the model)
            //
            // get the last action that lead to the end
            var lastAction = MoveToAction(lastMove);

            // get context
            var context = BoardToContext(board);

            var value = (winner == Side.None ? -1d : (winner == Side ? 1d : -2d));

            // apply terminal reward
            Model.Finish(
                context,
                lastAction,
                value);
        }

        public void Save()
        {
            Model.Save(filename: "model.tsv");
        }

        #region private
        private QLearner Model;
        private Side Side;

        //
        // Conversions
        //

        private Move ActionToMove(string action)
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

        private string MoveToAction(Move move)
        {
            // encode as: RowColumnDirection (eg. 000)
            return $"{move.Coordinate.Row}{move.Coordinate.Column}{(int)move.Direction}";
        }

        private string BoardToContext(CheckersBoard board)
        {
            return MinimalBoard.Create(board).AsString();
        }
        #endregion
    }
}
