using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe
{
    class RandomComputer : IPlayer
    {
        public RandomComputer()
        {
            Rand = new Random();
        }

        public Coordinate ChooseAction(TicTacToeBoard board)
        {
            var avialableMoves = board.GetAvailble().ToList();
            return avialableMoves[Rand.Next() % avialableMoves.Count()];
        }

        public void Finish(TicTacToeBoard board, Piece winningPiece, Coordinate lastCoordinate)
        {
            // nothing
        }

        #region private
        private Random Rand;
        #endregion
    }
}
