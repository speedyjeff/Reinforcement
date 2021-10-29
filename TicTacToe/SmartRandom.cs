using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe
{
    class SmartRandomComputer : IPlayer
    {
        public SmartRandomComputer(Piece piece)
        {
            Rand = new Random();
            Piece = piece;
        }

        public Coordinate ChooseAction(TicTacToeBoard board)
        {
            // find a place to win
            if (TryCompleteSeries(board, Piece, out List<Coordinate> wincoords))
            {
                // randomize for variety
                if (wincoords.Count == 0) throw new Exception("failed to get a winning coordinate");
                return wincoords[Rand.Next() % wincoords.Count];
            }

            // find a place to block
            if (TryCompleteSeries(board, (Piece == Piece.O ? Piece.X : Piece.O), out List<Coordinate> blockcoords))
            {
                // randomize for variety
                if (blockcoords.Count == 0) throw new Exception("failed to get a blockking coordinate");
                return blockcoords[Rand.Next() % blockcoords.Count];
            }

            // choose randomly
            var avialableMoves = board.GetAvailble().ToList();
            if (avialableMoves.Count == 0) throw new Exception("failed to get a move");
            return avialableMoves[Rand.Next() % avialableMoves.Count()];
        }

        public void Finish(TicTacToeBoard board, Piece winningPiece, Coordinate lastCoordinate)
        {
            // nothing
        }

        #region private
        private Random Rand;
        private Piece Piece;

        private bool TryFindFirst(TicTacToeBoard board, Piece piece, Func<Coordinate, Coordinate> update, ref Coordinate coord)
        {
            for (int i=0; i<board.Dimension; i++)
            {
                // try to get piece
                if (board.TryGetPiece(coord, out Piece lpiece) && lpiece == piece) return true;

                // advance
                coord = update(coord);
            }

            return false;
        }

        private bool TryCompleteSeries(TicTacToeBoard board, Piece piece, out List<Coordinate> completed)
        {
            // initialize output
            completed = new List<Coordinate>();

            // iterate
            var coord = new Coordinate();
            Coordinate complete;
            var lrdiagcount = 0;
            var rldiagcount = 0;
            for (int i = 0; i < board.Dimension; i++)
            {
                var ccount = 0;
                var rcount = 0;
                var lpiece = Piece.Empty;
                for (int j = 0; j < board.Dimension; j++)
                {
                    // column
                    coord.Row = i;
                    coord.Column = j;
                    if (!board.TryGetPiece(coord, out lpiece)) throw new Exception("invalid coord");
                    if (lpiece == piece) ccount++;

                    // row
                    coord.Row = j;
                    coord.Column = i;
                    if (!board.TryGetPiece(coord, out lpiece)) throw new Exception("invalid coord");
                    if (lpiece == piece) rcount++;
                }

                // found a possible win (column)
                complete = new Coordinate() { Row = i, Column = 0};
                if (ccount == board.Dimension - 1 && TryFindFirst(board, Piece.Empty, (lc) => { lc.Column++; return lc; }, ref complete)) completed.Add(complete);

                // found a possible win (row)
                complete = new Coordinate() { Row = 0, Column = i };
                if (rcount == board.Dimension - 1 && TryFindFirst(board, Piece.Empty, (lc) => { lc.Row++; return lc; }, ref complete)) completed.Add(complete);

                // diagnal
                // left to right
                coord.Row = i;
                coord.Column = i;
                if (!board.TryGetPiece(coord, out lpiece)) throw new Exception("invalid coord");
                if (lpiece == piece) lrdiagcount++;

                // right to left
                coord.Row = board.Dimension - i - 1;
                coord.Column = board.Dimension - i - 1;
                if (!board.TryGetPiece(coord, out lpiece)) throw new Exception("invalid coord");
                if (lpiece == piece) rldiagcount++;
            }

            // found a possible win (right to left diagnal)
            complete = new Coordinate() { Row = board.Dimension - 1, Column = board.Dimension - 1 };
            if (rldiagcount == board.Dimension - 1 && TryFindFirst(board, Piece.Empty, (lc) => { lc.Column--; lc.Row--; return lc; }, ref complete)) completed.Add(complete);

            // found a possible win (left to right diagonal)
            complete = new Coordinate() { Row = 0, Column = 0 };
            if (lrdiagcount == board.Dimension - 1 && TryFindFirst(board, Piece.Empty, (lc) => { lc.Column++; lc.Row++; return lc; }, ref complete)) completed.Add(complete);

            return completed.Count > 0;
        }
        #endregion
    }
}
