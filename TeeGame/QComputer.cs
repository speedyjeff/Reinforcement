using Learning;
using System;

namespace TeeGame
{
    class QComputer : IPlayer
    {
        public QComputer()
        {
            Model = new Q<short, string>(
                reward: -0.04,
                learning: 0.5,
                discount: 1.0);
        }

        public Q<short, string> Model { get; private set; }

        public Move ChooseAction(TeeBoard board)
        {
            // get the available moves
            var moves = board.GetAvailableMoves();
            if (moves.Count == 0) throw new Exception("invalid moves");

            // encode the moves
            var actions = new List<string>();
            foreach (var move in moves) actions.Add($"{move.Source}{move.Destination}");

            // set the context
            var context = board.BoardHash();

            // use the model to determine the move
            var action = Model.ChooseAction(
                context, 
                actions, 
                applyActionFunc: (lcontext, laction) => 
                {
                    // apply this action to this context, and return the resulting context
                    var fboard = new TeeBoard((Tees)lcontext);

                    // convert the action back into a move
                    short fcontext = 0;
                    foreach(var move in moves)
                    {
                        if (laction.StartsWith(move.Source) && laction.EndsWith(move.Destination))
                        {
                            if (!fboard.TryApplyMove(move)) throw new Exception("invalid move");
                            fcontext = fboard.BoardHash();
                            break;
                        }
                    }

                    if (fcontext == 0) throw new Exception("failed to find an action match");

                    return fcontext; 
                });

            // return the move
            foreach (var move in moves)
            {
                if (action.StartsWith(move.Source) && action.EndsWith(move.Destination)) return move;
            }

            throw new Exception("failed to find a move");
        }

        public void Finish(TeeBoard board, int teesRemaining, Move lastMove)
        {
            var context = board.BoardHash();
            var action = $"{lastMove.Source}{lastMove.Destination}";

            Model.ApplyTerminalCondition(
                context,
                action,
                teesRemaining == 1 ? 1d : 0d);
        }
    }
}
