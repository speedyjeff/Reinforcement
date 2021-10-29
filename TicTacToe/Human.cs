using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe
{
    class Human : IPlayer
    {
        public Coordinate ChooseAction(TicTacToeBoard board)
        {
            while (true)
            {
                Console.Write($"Enter a row,column pair to play (or enter for computer to play): ");
                var input = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(input))
                {
                    // parse out the row and column
                    var parts = input.Replace(" ", "").Split(',');
                    if (parts.Length >= 2)
                    {
                        return new Coordinate()
                        {
                            Row = Convert.ToInt32(parts[0]),
                            Column = Convert.ToInt32(parts[1])
                        };
                    }
                }

                Console.WriteLine(" ** invalid input **");
            }
        }

        public void Finish(TicTacToeBoard board, Piece winningPiece, Coordinate lastCoordinate)
        {
            // nothing
        }
    }
}
