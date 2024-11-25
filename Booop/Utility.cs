using System;

namespace Booop
{
    class Utility
    {
        public static void Print(Board board)
        {
            // get seam information
            board.TryGetSeam(PlayerType.Orange, out Coordinate orangeSeamCoordAfter, out Coordinate orangeSeamCoordInc);
            board.TryGetSeam(PlayerType.Purple, out Coordinate purpleSeamCoordAfter, out Coordinate purpleSeamCoordInc);

            // display the column header
            Console.Write("   ");
            for (int c = 0; c < board.Columns; c++)
            {
                Console.Write((char)('a' + c));
                Console.Write("  ");
            }
            Console.WriteLine();

            // display the board
            Console.WriteLine("  --------------------");
            for (int r = 0; r < board.Rows; r++)
            {
                Console.Write(r);

                // display the spaces
                Console.Write(" |");
                for (int c = 0; c < board.Columns; c++)
                {
                    if (!board.TryGetCell(new Coordinate() { Row = r, Column = c }, out PieceType piece, out PlayerType player)) throw new Exception("failed to get cell");

                    // display the piece
                    Console.Write(" ");
                    if (player == PlayerType.Orange)
                    {
                        if (piece == PieceType.Small) Console.Write("o");
                        else if (piece == PieceType.Large) Console.Write("O");
                    }
                    else if (player == PlayerType.Purple)
                    {
                        if (piece == PieceType.Small) Console.Write("p");
                        else if (piece == PieceType.Large) Console.Write("P");
                    }
                    else Console.Write(" ");

                    // check for seam
                    if (orangeSeamCoordAfter.Row == r && orangeSeamCoordAfter.Column == c && Math.Abs(orangeSeamCoordInc.Row) > 0)
                    {
                        if (orangeSeamCoordInc.Row > 0) Console.Write("v");
                        else if (orangeSeamCoordInc.Row < 0) Console.Write("^");
                    }
                    else if (purpleSeamCoordAfter.Row == r && purpleSeamCoordAfter.Column == c && Math.Abs(purpleSeamCoordInc.Row) > 0)
                    {
                        if (purpleSeamCoordInc.Row > 0) Console.Write("v");
                        else if (purpleSeamCoordInc.Row < 0) Console.Write("^");
                    }
                    else Console.Write("|");
                }
                Console.WriteLine();

                // display the seams
                if (r < board.Rows - 1)
                {
                    Console.Write("   ");
                    for (int c = 0; c < board.Columns; c++)
                    {
                        Console.Write("-");
                        if (orangeSeamCoordAfter.Row == r && orangeSeamCoordAfter.Column == c && Math.Abs(orangeSeamCoordInc.Column) > 0)
                        {
                            if (orangeSeamCoordInc.Column > 0) Console.Write(">");
                            else if (orangeSeamCoordInc.Column < 0) Console.Write("<");
                        }
                        else if (purpleSeamCoordAfter.Row == r && purpleSeamCoordAfter.Column == c && Math.Abs(orangeSeamCoordInc.Column) > 0)
                        {
                            if (purpleSeamCoordInc.Column > 0) Console.Write("<");
                            else if (purpleSeamCoordInc.Column < 0) Console.Write(">");
                        }
                        else Console.Write("-");
                        Console.Write("-");
                    }
                    Console.WriteLine();
                }
            }
            Console.WriteLine("  --------------------");
        }
    }
}
