using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    internal class RandomComputer : IPlayer
    {
        public RandomComputer()
        {
            Rand = new System.Random();
        }

        public Move ChooseAction(CheckersBoard board)
        {
            // get the available moves
            var moves = board.GetAvailableMoves();

            if (moves == null || moves.Count == 0) throw new Exception("no moves to choose from");

            return moves[ Rand.Next() % moves.Count ];
        }

        public void Finish(CheckersBoard board, Side winner, Move lastMove)
        {
        }

        #region private
        private System.Random Rand;
        #endregion
    }
}
