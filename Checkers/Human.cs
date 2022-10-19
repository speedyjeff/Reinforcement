using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    internal class Human : IPlayer
    {
        public Move ChooseAction(CheckersBoard board)
        {
            // get the available moves
            var moves = board.GetAvailableMoves();

            if (moves == null || moves.Count == 0) throw new Exception("no moves to choose from");

            // get the input from the console
            Console.WriteLine($"Choose an available move [{board.Turn}]:");
            for(int i=0; i<moves.Count; i++)
            {
                Console.WriteLine($" {i} : {moves[i].Coordinate.Row},{moves[i].Coordinate.Column} {moves[i].Direction}");
            }

            do
            {
                var input = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(input) && Int32.TryParse(input, out int index))
                {
                    if (index >= 0 && index < moves.Count) return moves[index];
                }

                Console.WriteLine("invalid selection, try again:");
            }
            while (true);
        }

        public void Finish(CheckersBoard board, Side winner, Move lastMove)
        {
        }

        public void Save()
        {
        }

        #region private
        private static Direction TextToDirection(char chr)
        {
            switch(chr)
            {
                case '0': return Direction.DownLeft;
                case '1': return Direction.DownRight;
                case '2': return Direction.UpLeft;
                case '3': return Direction.UpRight;
                default: return Direction.None;
            }
        }

        private static int TextToRow(char chr)
        {
            switch (chr)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                default: return -1;
            }
        }

        private static int TextToColumn(char chr)
        {
            switch(char.ToLower(chr))
            {
                case 'a': return 0;
                case 'b': return 1;
                case 'c': return 2;
                case 'd': return 3;
                case 'e': return 4;
                case 'f': return 5;
                case 'g': return 6;
                case 'h': return 7;
                default: return -1;
            }
        }
        #endregion
    }
}
