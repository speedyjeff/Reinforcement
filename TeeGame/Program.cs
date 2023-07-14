using System;
using System.Reflection.Metadata.Ecma335;

namespace TeeGame
{
    internal class TeeGame
    {
        static int Main(string[] args)
        {
            var options = Options.Parse(args);

            if (options.Help)
            {
                Options.DisplayHelp();
                return 1;
            }

            // process data into a format consumable by a Sankey diagram
            // eg. nuget package SankeyDiagram
            // paste this output into the sample
            if (!string.IsNullOrWhiteSpace(options.SankeyInput) && File.Exists(options.SankeyInput))
            {
                Console.WriteLine("            var datas = new List<SankeyData>()\r\n            {");
                Sankey.Parse(options.SankeyInput, options.SankeyLimit, (from,to,weight) => { return $"                 new SankeyData(\"{from}\", \"{to}\", {weight}),"; } );
                Console.WriteLine("            };");
                return 0;
            }

            // context
            var game = new TeeGame();
            var total = 0;
            var wins = 0;
            IPlayer player = null;
            switch (options.PlayerType)
            {
                case PlayerType.Human:
                    player = new Human();
                    break;
                case PlayerType.QLearn:
                    player = new QComputer();
                    break;
                case PlayerType.NeuralNetwork:
                    player = new NComputer(options.Iterations);
                    break;
                default:
                    throw new Exception("unknown player type");
            }

            // play iteration rounds
            for (int i = options.Iterations-1; i >= 0; i--)
            {
                if (options.IsQuiet && i % 10000 == 0) Console.Write('.');

                var win = game.Play(
                    player,
                    quiet: options.IsQuiet && (i != 0 && options.Iterations > 1));

                // stats
                total++;
                if (win) wins++;
            }

            // display the stats
            Console.WriteLine();
            Console.WriteLine($"total games: {total}");
            Console.WriteLine($"wins       : {wins}");

            return 0;
        }

        public bool Play(IPlayer player, bool quiet)
        {
            var board = new TeeBoard();
            var teeCount = 0;
            Move move = new Move();
            var steps = new List<Move>();

            while (true)
            {
                // display the board
                if (!quiet)
                {
                    Display(board);
                    if (player is QComputer comp)
                    {
                        // display the q values
                        DisplayQValues(board, comp);
                    }
                }

                // check if done
                teeCount = board.Count();
                if (teeCount == 1) break;
                var moves = board.GetAvailableMoves();
                if (moves.Count == 0) break;

                // get a move from the player
                move = player.ChooseAction(board);

                // apply the move
                if (!board.TryApplyMove(move)) throw new Exception("invalid move");

                // retain the move for displaying
                if (!quiet) steps.Add(new Move() { From = move.From, Jumped = move.Jumped, To = move.To});
            }

            // give player feedback
            player.Finish(board, teeCount, move);

            // show the steps taken
            if (!quiet && steps.Count > 0)
            {
                Console.Write("moves:");
                foreach(var m in steps)
                {
                    Console.Write($" {m.Source}->{m.Destination}");
                }
                Console.WriteLine();
            }

            // check if this is a win
            if (teeCount == 1)
            {
                if (!quiet) Console.WriteLine("winner");
                return true;
            }
            else
            {
                if (!quiet) Console.WriteLine($"try again [{teeCount}]");
                return false;
            }
        }

        #region private
        private void Display(TeeBoard board)
        {
            Console.WriteLine("            ^");
            Console.WriteLine("           / \\ ");
            Console.WriteLine($"          / {(board.IsSet('a') ? "A" : "a")} \\ ");
            Console.WriteLine($"         / {(board.IsSet('b') ? "B" : "b")} {(board.IsSet('c') ? "C" : "c")} \\ ");
            Console.WriteLine($"        / {(board.IsSet('d') ? "D" : "d")} {(board.IsSet('e') ? "E" : "e")} {(board.IsSet('f') ? "F" : "f")} \\ ");
            Console.WriteLine($"       / {(board.IsSet('g') ? "G" : "g")} {(board.IsSet('h') ? "H" : "h")} {(board.IsSet('i') ? "I" : "i")} {(board.IsSet('j') ? "J" : "j")} \\ ");
            Console.WriteLine($"      / {(board.IsSet('k') ? "K" : "k")} {(board.IsSet('l') ? "L" : "l")} {(board.IsSet('m') ? "M" : "m")} {(board.IsSet('n') ? "N" : "n")} {(board.IsSet('o') ? "O" : "o")} \\ ");
            Console.WriteLine("     ---------------");
        }

        private void DisplayQValues(TeeBoard board, QComputer comp)
        {
            if (!comp.Model.TryGetActions(board.BoardHash(), out var actions))
            {
                Console.WriteLine("failed to get actions");
                return;
            }

            foreach (var act in actions)
            {
                Console.WriteLine($"{act.Key} {act.Value}");
            }
        }
        #endregion
    }
}