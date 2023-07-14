using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe
{
    class Program
    {
        static void Main(string[] args)
        {
            int dimension = 3;
            var players = new IPlayer[2];
            var currentRound = 0;
            var wins = new int[2];

            // ask how many rounds
            Console.Write("how many rounds: ");
            var input = Console.ReadLine();
            if (!Int32.TryParse(input, out int rounds)) rounds = 0;

            // choose the players
            players[(int)Piece.X] = ChoosePlayer(Piece.X, dimension, rounds);
            players[(int)Piece.O] = ChoosePlayer(Piece.O, dimension, rounds);

            // play in a loop
            while (true)
            {
                // setup
                var board = new TicTacToeBoard(dimension);
                var winningPiece = Piece.Empty;
                var playerIndex = 0;
                var lastCoordinate = default(Coordinate);

                // play until there is a winner
                do
                {
                    // current player and piece
                    var player = players[playerIndex];
                    var piece = (Piece)playerIndex;

                    // display the board
                    if ((rounds - currentRound) <= 0) Console.WriteLine(board.ToString());

                    // display context if computer
                    if ((rounds - currentRound) <= 0 && (player as Computer) is { } cplayer)
                    {
                        Console.WriteLine(cplayer.GetContext(board));
                    }

                    // make a choice
                    var coord = player.ChooseAction(board);

                    // try to place the piece
                    if (board.TryPutPiece(coord, piece))
                    {
                        // store the last success move
                        lastCoordinate = coord;

                        // advance
                        playerIndex = (playerIndex + 1) % 2;
                    }
                    else
                    {
                        Console.WriteLine("error: not a valid move, please try again");

                        if (player is Computer) throw new Exception("invalid choice by the computer");
                    }
                }
                while (!board.IsDone(out winningPiece));

                // sum win
                if (winningPiece != Piece.Empty)
                {
                    wins[(int)winningPiece]++;
                }

                // display the win
                if ((rounds - currentRound) <= 0)
                {
                    Console.WriteLine(board.ToString());
                    if (winningPiece == Piece.Empty) Console.WriteLine("Cats game!");
                    else Console.WriteLine($"Congratulations {winningPiece}, you win!");
                }
                else
                {
                    var sbcontext = new StringBuilder();
                    if ((players[(int)Piece.O] as Computer) is Computer wocplayer) sbcontext.Append($"{wocplayer.ContextCount}\t{wocplayer.ContextActionCount}");
                    else sbcontext.Append('\t');
                    sbcontext.Append('\t');
                    if ((players[(int)Piece.X] as Computer) is Computer wxcplayer) sbcontext.Append($"{wxcplayer.ContextCount}\t{wxcplayer.ContextActionCount}");
                    else sbcontext.Append('\t');

                    // display a compact version
                    Console.WriteLine($"{currentRound}\t{winningPiece}\t{wins[(int)Piece.O]}\t{wins[(int)Piece.X]}\t{sbcontext}");
                }

                // incidate to the computer the winning state
                players[(int)Piece.O].Finish(board, winningPiece, lastCoordinate);
                players[(int)Piece.X].Finish(board, winningPiece, lastCoordinate);

                // check if they want to play again
                currentRound++;
                if ((rounds - currentRound) <= 0)
                {
                    // display the win counts
                    Console.WriteLine();
                    Console.WriteLine($"Wins - {Piece.X} : {wins[(int)Piece.X]}");
                    Console.WriteLine($"Wins - {Piece.O} : {wins[(int)Piece.O]}");

                    Console.WriteLine();
                    var again = "";
                    while (string.IsNullOrWhiteSpace(again) || 
                        (!again.Equals("y", StringComparison.OrdinalIgnoreCase) 
                        && !again.Equals("n", StringComparison.OrdinalIgnoreCase)))
                    {
                        Console.Write("Would you like to play again [y,n]: ");
                        again = Console.ReadLine();
                    }
                    if (again.Equals("n", StringComparison.Ordinal)) break;
                }
            }

            // save the computer model
            if ((players[(int)Piece.O] as Computer) is Computer ocplayer) ocplayer.SaveModel();
            if ((players[(int)Piece.X] as Computer) is Computer xcplayer) xcplayer.SaveModel();
        }

        #region private
        [return: NotNull]
        private static IPlayer ChoosePlayer(Piece piece, int dimension, int rounds)
        {
            while(true)
            {
                Console.Write($"who is playing {piece} [(c)omputer | (h)uman | (r)andom | (s)mart | (n)eural]: ");
                var choice = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(choice)) continue;
                else if (choice.StartsWith("c", StringComparison.OrdinalIgnoreCase)) return new Computer(dimension, piece);
                else if (choice.StartsWith("h", StringComparison.OrdinalIgnoreCase)) return new Human();
                else if (choice.StartsWith("r", StringComparison.OrdinalIgnoreCase)) return new RandomComputer();
                else if (choice.StartsWith("s", StringComparison.OrdinalIgnoreCase)) return new SmartRandomComputer(piece);
                else if (choice.StartsWith("n", StringComparison.OrdinalIgnoreCase)) return new NeuralComputer(piece, dimension, rounds);
                else Console.WriteLine("** incorrect selection **");
            }
        }
        #endregion
    }
}
