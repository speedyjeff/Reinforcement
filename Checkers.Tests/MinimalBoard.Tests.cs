using System;
using Checkers;

namespace Checkers.Tests
{
    static class MinimalBoardTests
    {
        public static void Indexes()
        {
            // fill completly and let the internal checks ensure that it is correct
            FillBoard(new Coordinate() { Piece = new Piece() { IsKing = false, Side = Side.White } });
            FillBoard(new Coordinate() { Piece = new Piece() { IsKing = false, Side = Side.Black }});

            FillBoard(new Coordinate() { Piece = new Piece() { IsKing = true, Side = Side.White } });
            FillBoard(new Coordinate() { Piece = new Piece() { IsKing = true, Side = Side.Black } });
        }

        #region private
        private static void FillBoard(Coordinate coord)
        {
            var board = new MinimalBoard();
            var clrBoard = new MinimalBoard();

            for (coord.Row = 0; coord.Row < 8; coord.Row++)
            {
                for (coord.Column = 0; coord.Column < 8; coord.Column++)
                {
                    // make the checker board
                    if (coord.Row % 2 == 0 && coord.Column % 2 == 0) continue;
                    if (coord.Row % 2 != 0 && coord.Column % 2 != 0) continue;

                    board.Put(coord);
                    clrBoard.Put(coord);
                    clrBoard.Clear(coord);

                    Console.WriteLine($"{coord.Row},{coord.Column} : {board.ToString()}");

                    if (!clrBoard.ToString().Equals("0 0 0 0 0 0 0 0 | 0 0 0 0")) throw new Exception("invalid clear board");
                }
            }
        }
        #endregion
    }
}
