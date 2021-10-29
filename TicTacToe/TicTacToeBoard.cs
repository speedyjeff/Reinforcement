using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe
{
    public class TicTacToeBoard
    {
        public TicTacToeBoard(int dim)
        {
            Dimension = dim;
            Pieces = new Piece[dim][];
            for (int r = 0; r < dim; r++)
            {
                Pieces[r] = new Piece[dim];
                for (int c = 0; c < dim; c++) Pieces[r][c] = Piece.Empty;
            }
        }

        public TicTacToeBoard(TicTacToeBoard board)
        {
            // make a deep copy
            Dimension = board.Dimension;
            Pieces = new Piece[board.Pieces.Length][];
            for (int r = 0; r < Pieces.Length; r++)
            {
                Pieces[r] = new Piece[board.Pieces[r].Length];
                for (int c = 0; c < Pieces[r].Length; c++) Pieces[r][c] = board.Pieces[r][c];
            }
        }

        public int Dimension { get; private set; }

        public bool IsDone(out Piece winningPiece)
        {
            var count = 0;
            winningPiece = Piece.Empty;

            // columns
            for (int r = 0; r < Pieces.Length; r++)
            {
                count = 1;
                for (int c = 1; c < Pieces[r].Length; c++)
                {
                    if (Pieces[r][c - 1] != Piece.Empty && Pieces[r][c] == Pieces[r][c - 1]) count++;
                }
                if (count == Pieces[r].Length)
                {
                    winningPiece = Pieces[r][0];
                    return true;
                }
            }

            // rows
            for (int c = 0; c < Pieces[0].Length; c++)
            {
                count = 1;
                for (int r = 1; r < Pieces.Length; r++)
                {
                    if (Pieces[r - 1][c] != Piece.Empty && Pieces[r][c] == Pieces[r - 1][c]) count++;
                }
                if (count == Pieces.Length)
                {
                    winningPiece = Pieces[0][c];
                    return true;
                }
            }

            // cross
            count = 1;
            for (int c = 1, r = 1; r < Pieces.Length && c < Pieces[r].Length; c++, r++)
            {
                if (Pieces[r - 1][c - 1] != Piece.Empty && Pieces[r][c] == Pieces[r - 1][c - 1]) count++;
            }
            if (count == Pieces.Length)
            {
                winningPiece = Pieces[0][0];
                return true;
            }

            count = 1;
            for (int r = 1, c = Pieces[Pieces.Length - 1].Length - 2; r < Pieces.Length && c >= 0; c--, r++)
            {
                if (Pieces[r - 1][c + 1] != Piece.Empty && Pieces[r][c] == Pieces[r - 1][c + 1]) count++;
            }
            if (count == Pieces.Length)
            {
                winningPiece = Pieces[Pieces.Length - 1][0];
                return true;
            }

            // cats
            count = 0;
            for (int r = 0; r < Pieces.Length; r++)
            {
                for (int c = 0; c < Pieces[r].Length; c++)
                {
                    if (Pieces[r][c] == Piece.Empty) count++;
                }
            }
            if (count == 0)
            {
                winningPiece = Piece.Empty;
                return true;
            }

            // no winner
            winningPiece = Piece.Empty;
            return false;
        }

        public bool TryPutPiece(Coordinate coord, Piece piece)
        {
            if (coord.Row < 0 || coord.Row >= Pieces.Length) return false;
            if (coord.Column < 0 || coord.Column >= Pieces[coord.Row].Length) return false;
            if (Pieces[coord.Row][coord.Column] != Piece.Empty) return false;
            Pieces[coord.Row][coord.Column] = piece;
            return true;
        }

        public bool TryGetPiece(Coordinate coord, out Piece piece)
        {
            piece = Piece.Empty;
            if (coord.Row < 0 || coord.Row >= Pieces.Length) return false;
            if (coord.Column < 0 || coord.Column >= Pieces[coord.Row].Length) return false;
            piece = Pieces[coord.Row][coord.Column];
            return true;
        }

        public IEnumerable<Coordinate> GetAvailble()
        {
            for (int r = 0; r < Pieces.Length; r++)
            {
                for (int c = 0; c < Pieces[r].Length; c++)
                {
                    if (Pieces[r][c] == Piece.Empty) yield return new Coordinate() { Row = r, Column = c };
                }
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            for (int r = 0; r < Pieces.Length; r++)
            {
                for (int c = 0; c < Pieces[r].Length; c++)
                {
                    if (c > 0) sb.Append("|");
                    switch (Pieces[r][c])
                    {
                        case Piece.O: sb.Append("o"); break;
                        case Piece.X: sb.Append("x"); break;
                        case Piece.Empty: sb.Append(" "); break;
                        default: throw new Exception("Unknown piece : " + Pieces[r][c]);
                    }
                }
                sb.Append(System.Environment.NewLine);
                if (r < Pieces.Length - 1) for (int c = 0; c < Pieces[r].Length; c++) sb.Append("--");
                sb.Append(System.Environment.NewLine);
            }

            return sb.ToString();
        }

        #region private
        private Piece[][] Pieces;
        #endregion private
    }
}
