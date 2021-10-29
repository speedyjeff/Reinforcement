using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe
{
    public class _MyBoard
    {
        public _MyBoard(int dim)
        {
            Dimension = dim;
            Board = new byte[dim * dim];
        }

        public _MyBoard(Piece piece, TicTacToeBoard board) : this(board.Dimension)
        {
            // encode the current board
            var coord = new Coordinate();
            for (coord.Row = 0; coord.Row < board.Dimension; coord.Row++)
            {
                for (coord.Column = 0; coord.Column < board.Dimension; coord.Column++)
                {
                    Piece cell;
                    if (board.TryGetPiece(coord, out cell))
                    {
                        if (cell != Piece.Empty) Set(coord, cell == piece /* mine */);
                    }
                }
            }
        }

        public byte[] Board { get; set; }
        public int Dimension { get; set; }

        public _MyBoard Apply(int index)
        {
            // clone the sbyte array and set the index (with my piece)
            var myboard = new _MyBoard(Dimension);
            for (int i = 0; i < Dimension; i++) myboard.Board[i] = Board[i];
            var coord = new Coordinate() { Row = index / Dimension, Column = index % Dimension };
            myboard.Set(coord, true /* mine */);
            return myboard;
        }

        public void Set(Coordinate coord, bool mine)
        {
            var index = (coord.Row * Dimension + coord.Column);
            Set(index, mine);
        }

        public void Set(int index, bool mine)
        {
            Board[index] = mine ? (byte)1 : (byte)128;
        }

        public void Clear(Coordinate coord)
        {
            var index = (coord.Row * Dimension + coord.Column);
            Clear(index);
        }

        public void Clear(int index)
        {
            Board[index] = 0;
        }

        public override bool Equals(object? obj)
        {
            var other = obj as _MyBoard;
            if (other == null) return false;
            if (Board.Length != other.Board.Length) return false;

            for (int i = 0; i < Board.Length; i++)
            {
                if (Board[i] != other.Board[i]) return false;
            }

            // they are equal
            return true;
        }

        public override int GetHashCode()
        {
            var hash = 17;
            foreach (var b in Board)
            {
                hash = hash * 31 + b;
            }
            return hash;
        }
    }
}
