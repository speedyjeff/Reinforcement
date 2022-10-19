using Checkers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

public class CheckersGame
{
    public static int Main(string[] args)
    {
        // parse options
        var options = Options.Parse(args);

        if (options.Help)
        {
            Options.ShowHelp();
            return 1;
        }

        // initalize
        var players = new IPlayer[2];
        var winCount = new int[3];
        var turnHistogram = new int[options.MaxTurns + 1];
        var iterations = 0;
        List<Coordinate> initialBoard = null;
        var uniqueMoves = options.RetainUniqueGames ? new HashSet<string>() : null;
        var currentMoves = options.RetainUniqueGames ? new StringBuilder() : null;

        // choose how many iterations
        if (!options.Quiet || options.Iterations <= 0) Console.WriteLine("How many iterations:");
        if (options.Iterations > 0)
        {
            iterations = options.Iterations;
            if (!options.Quiet) Console.WriteLine(iterations);
        }
        else
        {
            var input = Console.ReadLine();
            if (!Int32.TryParse(input, out iterations)) iterations = 1;
        }

        // choose players
        players[0] = ChoosePlayer(Side.White, iterations, options.PlayerWhite, options);
        players[1] = ChoosePlayer(Side.Black, iterations, options.PlayerBlack, options);

        // check if there is an initial board configuration
        if (!string.IsNullOrWhiteSpace(options.InitBoardFile) && File.Exists(options.InitBoardFile))
        {
            initialBoard = ReadBoard(options.InitBoardFile);
        }

        for (int i = 0; i < iterations; i++)
        {
            // create the board
            var board = new CheckersBoard(options.Dimension, initialBoard);
            Move move = null;
            var playCount = 0;

            // subscribe to the side change notification
            board.OnSideChange += () => { playCount++; };

            // decide if learning values should be displayed
            if (options.ShowBoards || i == (iterations - 1))
            {
                if (!options.Quiet)
                {
                    if (players[0] is SmartComputer) (players[0] as SmartComputer).IsDebug = true;
                    if (players[1] is SmartComputer) (players[1] as SmartComputer).IsDebug = true;
                }
            }

            // play
            do
            {
                // check if we have exceeded the play count
                if (playCount >= options.MaxTurns) break;

                // choose player
                int index;
                if (board.Turn == Side.White) index = 0;
                else if (board.Turn == Side.Black) index = 1;
                else throw new Exception($"unknown side {board.Turn}");

                // display

                if (players[index] is Human || options.ShowBoards || i == (iterations - 1))
                {
                    if (!options.Quiet) Display(board);
                }

                // get move
                move = players[index].ChooseAction(board);

                // keep the move for the unique string
                if (currentMoves != null)
                {
                    currentMoves.Append($" {move.Coordinate.Row},{move.Coordinate.Column}:{move.Direction}");
                }

                // make the move
                if (!board.TryMakeMove(move)) 
                    if (!options.Quiet) Console.WriteLine("move failed!");
            }
            while (!board.IsDone);

            // display
            if (players[0] is Human ||
                players[1] is Human ||
                options.ShowBoards ||
                i == (iterations - 1))
            {
                if (!options.Quiet)
                {
                    Console.WriteLine($"Outcome: {(board.Winner == Side.None ? "cats" : board.Winner)}");
                    Display(board);
                }
            }

            // keep count of wins
            if (board.Winner == Side.White) winCount[0]++;
            else if (board.Winner == Side.Black) winCount[1]++;
            else if (board.Winner == Side.None) winCount[2]++;
            else throw new Exception("Unknown winner");

            if (!options.Quiet) Console.WriteLine($"wins : White [{winCount[0]}] Black [{winCount[1]}] Cats [{winCount[2]}]");

            // keep a histogram of how many times it takes to win
            if (playCount < 0 || playCount >= turnHistogram.Length) throw new Exception("invalid playcount");
            turnHistogram[playCount]++;

            // notify the players on the end
            players[0].Finish(board, board.Winner, move);
            players[1].Finish(board, board.Winner, move);

            // retain the unique series of moves
            if (uniqueMoves != null)
            {
                uniqueMoves.Add($"{board.Winner} {currentMoves.ToString()}");
                currentMoves.Clear();
            }
        }

        // display play histogram
        if (!options.Quiet)
        {
            Console.WriteLine();
            Console.WriteLine("Play histogram:");
            for (int i = 0; i < turnHistogram.Length; i++)
            {
                if (turnHistogram[i] > 0)
                {
                    Console.WriteLine($"  {i} {turnHistogram[i]} {((double)turnHistogram[i] * 100) / (double)(iterations > 0 ? iterations : 1):f2}%");
                }
            }
        }

        // display the final results even if quiet
        if (options.Quiet) Console.WriteLine($"{winCount[0]}\t{winCount[1]}\t{winCount[2]}");

        // save the models at the end
        if (!options.Quiet) Console.WriteLine("Saving models to disk...");
        players[0].Save();
        players[1].Save();

        // display all the unique moves that lead to an outcome
        if (uniqueMoves != null && !options.Quiet)
        {
            Console.WriteLine("List of all unique combinations that lead to an end result:");
            foreach (var moves in uniqueMoves) Console.WriteLine(moves);
        }

        return 0;
    }

    #region private
    private static IPlayer ChoosePlayer(Side side, int iterations, string defaultChoice, Options options)
    {
        while (true)
        {
            if (!options.Quiet || string.IsNullOrWhiteSpace(defaultChoice)) Console.Write($"who is playing {side} [(c)omputer | (h)uman | (r)andom | (p)suedo random | (n)eural]: ");
            var choice = "";
            if (!string.IsNullOrWhiteSpace(defaultChoice))
            {
                if (!options.Quiet) Console.WriteLine(defaultChoice);
                choice = defaultChoice;

                // clear default choice (only use once)
                defaultChoice = "";
            }
            else choice = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(choice)) continue;
            else if (choice.StartsWith("r", StringComparison.OrdinalIgnoreCase)) return new RandomComputer(options.Seed);
            else if (choice.StartsWith("h", StringComparison.OrdinalIgnoreCase)) return new Human();
            else if (choice.StartsWith("c", StringComparison.OrdinalIgnoreCase)) return new SmartComputer(side, options.Dimension, options.Reward, options.Learning, options.Discount);
            else if (choice.StartsWith("p", StringComparison.OrdinalIgnoreCase)) return new PsuedoRandomComputer();
            else if (choice.StartsWith("e", StringComparison.OrdinalIgnoreCase)) return new ExtensionSampleComputer(side, options.Reward, options.Learning, options.Discount);
            else if (choice.StartsWith("n", StringComparison.OrdinalIgnoreCase)) return new NeuralComputer(side, options.Dimension, iterations, 
                                                                                                                options: new NeuralComputerOptions()
                                                                                                                {
                                                                                                                    DumpStats = options.DumpStats,
                                                                                                                    LSituations = (LearningSituations)options.LearningSituations[side == Side.White ? 0 : 1],
                                                                                                                    LStyle = (LearningStyle)options.LearningStyle[side == Side.White ? 0 : 1],
                                                                                                                    TStyle = (TrainingStyle)options.TrainingStyle,
                                                                                                                    MaxTrainingPct = options.MaxTrainingPct,
                                                                                                                    NoLearning = options.NoLearning,
                                                                                                                    Seed = options.Seed
                                                                                                                });
            else Console.WriteLine("** incorrect selection **");
        }
    }

    private static void Display(CheckersBoard board)
    {
        Console.WriteLine("--------------------------------------");
        Console.Write("  ");
        for (int c = 0; c < board.Dimension; c++) Console.Write($" {c} ");
        Console.WriteLine();

        for (int r=0;r< board.Dimension; r++)
        {
            Console.Write($"{r} ");
            for(int c=0;c< board.Dimension; c++)
            {
                var p = board[r, c];
                var chr = ' ';
                if (p.Side == Side.Black) chr = 'b';
                else if (p.Side == Side.White) chr = 'w';
                else if (p.Side == Side.None) chr = '-';
                if (p.IsKing) chr = char.ToUpper(chr);

                Console.Write($" {chr} ");
            }
            Console.WriteLine();
        }
    }

    private static List<Coordinate> ReadBoard(string filename)
    {
        // read in a square board from a file that has newline seperated view of the board
        // eg.
        // -w-w-w-w
        // w-w-w-w-
        // -w-w-w-w
        // --------
        // --------
        // b-b-b-b-
        // -b-b-b-b
        // b-b-b-b-
        // 
        // acceptable input: '-', 'w', 'W', 'b', 'B'

        var row = 0;
        var count = 0;
        var coords = new List<Coordinate>();

        using (var reader = File.OpenText(filename))
        {
            while(!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var col = 0;
                foreach (var chr in line.ToCharArray())
                {
                    var coord = new Coordinate()
                    {
                        Row = row,
                        Column = col,
                        Piece = new Piece()
                        { 
                            IsInvalid = false,
                            IsKing = Char.IsUpper(chr), // upper case indicates a king
                            Side = Side.None
                        }
                    };

                    switch(chr)
                    {
                        case '-': 
                            break;
                        case 'w':
                        case 'W':
                            coord.Piece.Side = Side.White;
                            count++;
                            break;
                        case 'b':
                        case 'B':
                            coord.Piece.Side = Side.Black;
                            count++;
                            break;
                        default: 
                            Console.WriteLine($"unknow board part : {chr}");
                            break;
                    }

                    coords.Add(coord);

                    // advance col
                    col++;
                }

                // advance row
                row++;
            }   
        }

        if (count == 0) throw new Exception("must provide a board that has at least 1 piece placed");

        return coords;
    }
    #endregion
}