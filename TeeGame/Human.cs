using System;

namespace TeeGame
{
    class Human : IPlayer
    {
        public Move ChooseAction(TeeBoard board)
        {
            // get available moves and ask the user to choose one
            var moves = board.GetAvailableMoves();

            if (moves == null || moves.Count == 0) throw new Exception("invalid set of available moves");

            // display the moves
            for(int i=0; i<moves.Count; i++)
            {
                Console.WriteLine($"{i} : {char.ToUpper(moves[i].Source)} -> {moves[i].Destination}");
            }

            // ask for input until given
            while (true)
            {
                Console.WriteLine($"Choose a move (0..{moves.Count - 1}):");
                var line = Console.ReadLine();
                if (Int32.TryParse(line, out int index))
                {
                    if (index >= 0 && index < moves.Count) return moves[index];
                }
            }
        }

        public void Finish(TeeBoard board, int teesRemaining, Move lastMove)
        {
            // nothing
        }
    }
}
