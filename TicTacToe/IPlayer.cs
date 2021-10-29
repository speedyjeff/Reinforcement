using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe
{
    interface IPlayer
    {
        Coordinate ChooseAction(TicTacToeBoard board);
        void Finish(TicTacToeBoard board, Piece winningPiece, Coordinate lastCoordinate);
    }
}
