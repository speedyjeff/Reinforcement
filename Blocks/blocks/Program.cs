using System;
using System.Collections.Generic;
using blocks.engine;
using Learning;

class Program
{
    static int Main(string[] args)
    {
        IPlayer player;
        
        // Choose mode
        var choice = 0;
        var str = "";
        do 
        {
            // menu
            Console.WriteLine("Welcome to Block Game!");
            Console.WriteLine("Choose mode:");
            Console.WriteLine("1. Human Player");
            Console.WriteLine("2. Computer Player (AI)");
            Console.WriteLine("3. Computer Player (based on past training)");
            Console.WriteLine("4. Train AI (Evolutionary Algorithm)");
            Console.WriteLine("5. Test Scenarios (verify learning on puzzles)");
            Console.Write("Enter choice (1-5): ");
            
            str = Console.ReadLine();
        } while (Int32.TryParse(str, out choice) == false || choice < 1 || choice > 5);
        
        // training AI mode
        if (choice == 4)
        {
            Console.WriteLine("\nStarting AI training with policy gradients...");
            Console.WriteLine("This will train a single network over many games.\n");
            
            var trainer = new ComputerTrainer();
            
            // Train with simplified immediate learning: 5,000 games for testing
            var trainedNetwork = trainer.Train(totalGames: 5000, reportInterval: 100);
            
            trainer.SaveNetwork(trainedNetwork, "trained_network.txt");
            Console.WriteLine("\nTraining completed! Network saved to 'trained_network.txt'");
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
        else if (choice == 5)
        {
            RunScenarioTests();
        }
        else 
        {
            // set up player
            if (choice == 2)
            {
                Console.WriteLine("Computer player selected. Watch the AI play!");
                Console.WriteLine("Press any key between moves to continue...");
                player = new Computer();
            }
            else if (choice == 3)
            {
                Console.WriteLine("Trained AI player loaded.");
                Console.WriteLine("Press any key between moves to continue...");
                var trainedNetwork = NeuralNetwork.Load("trained_network.txt");
                player = new Computer(trainedNetwork);
            }
            else
            {
                Console.WriteLine("Human player selected.");
                player = new Human();
            }

            PlaySingleGame(player);
        }

        return 0;
    }

    #region private

    /// <summary>
    /// Runs test scenarios to verify the AI can learn optimal solutions for simple puzzles.
    /// Stops after N attempts OR after achieving optimal score M times in a row.
    /// </summary>
    private static void RunScenarioTests()
    {
        const int MaxAttempts = 100000; // 100K iterations for complex scenarios
        const int CheckpointInterval = 10000; // Save progress every 10K attempts

        // Define scenarios with their expected optimal scores
        // Progress from simple to complex
        var scenarios = new (TestScenario scenario, int optimalScore, string description)[]
        {
            //(TestScenario.OneMoveColumnClear, 13, "V5 pre-placed, place V3 to clear column (ONE DECISION)"),
            //(TestScenario.ColumnClear, 18, "Vertical5 + Vertical3 = clear 1 column (TWO DECISIONS)"),
            (TestScenario.EasySingles, 50, "32 Singles - can clear multiple lines (MANY DECISIONS)"),
        };

        Console.WriteLine("\n=== SCENARIO TEST MODE ===");
        Console.WriteLine($"Testing AI on predefined puzzles");
        Console.WriteLine($"Max attempts per scenario: {MaxAttempts}");

        // Ask if user wants to use a trained network
        Console.Write("Use trained network? (y/n): ");
        var useTrained = Console.ReadLine()?.ToLower() == "y";
        
        Computer computer;
        if (useTrained)
        {
            try
            {
                var network = Learning.NeuralNetwork.Load("trained_network.txt");
                computer = new Computer(network);
                Console.WriteLine("Loaded trained_network.txt\n");
            }
            catch
            {
                Console.WriteLine("Could not load trained_network.txt, using fresh network\n");
                computer = new Computer();
            }
        }
        else
        {
            computer = new Computer();
            Console.WriteLine("Using fresh (untrained) network\n");
        }

        // Test each scenario
        foreach (var (scenario, optimalScore, description) in scenarios)
        {
            Console.WriteLine($"--- Testing: {scenario} ---");
            Console.WriteLine($"    {description}");
            Console.WriteLine($"    Optimal score: {optimalScore}");

            var attempts = 0;
            var totalScore = 0;
            var bestScore = 0;
            var optimalCount = 0;

            while (attempts < MaxAttempts)
            {
                attempts++;
                var (score, finalBoard) = PlayScenario(computer, scenario);
                totalScore += score;
                bestScore = Math.Max(bestScore, score);

                if (score >= optimalScore)
                {
                    optimalCount++;
                }

                // Progress update every 100 attempts - show board to visualize placement
                if (attempts % 100 == 0)
                {
                    Console.WriteLine($"    Attempt {attempts}: avg={totalScore / attempts:F1}, best={bestScore}, optimal={optimalCount}/{attempts} ({100.0 * optimalCount / attempts:F1}%)");
                    Console.WriteLine($"    Last game score: {score}");
                    PrintGrid(finalBoard, indent: "    ");
                    
                    // Show network output distribution to verify learning
                    var stats = computer.GetOutputStats();
                    Console.WriteLine($"    Network outputs - max: {stats.max:F4}, min: {stats.min:F4}, spread: {stats.spread:F4}");
                    Console.WriteLine();
                }
                
                // Save checkpoint periodically
                if (attempts % CheckpointInterval == 0)
                {
                    var checkpointFile = $"checkpoint_{scenario}_{attempts}.txt";
                    computer.SaveNetwork(checkpointFile);
                    Console.WriteLine($"    >>> Checkpoint saved: {checkpointFile}");
                }
            }

            // Final result
            var avgScore = (float)totalScore / attempts;
            
            Console.WriteLine($"    Result: Complete");
            Console.WriteLine($"    Attempts: {attempts}, Avg: {avgScore:F1}, Best: {bestScore}, Optimal: {optimalCount}/{attempts} ({100.0 * optimalCount / attempts:F1}%)");
            Console.WriteLine();
        }

        Console.WriteLine("Scenario tests complete. Press any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Plays a single scenario and returns the final score and board state
    /// </summary>
    private static (int score, int[][] board) PlayScenario(Computer computer, TestScenario scenario)
    {
        var blocks = new Blocks();
        var generator = new BlockGenerator(scenario);
        var pieces = new List<PieceType>();
        
        // Pre-fill board for scenarios that require it
        var preFill = BlockGenerator.GetScenarioPreFill(scenario);
        blocks.PreFillCells(preFill);

        while (true)
        {
            // Refill pieces if needed
            if (pieces.Count == 0)
            {
                for (int i = 0; i < Blocks.NumPiecesInSet; i++)
                {
                    if (generator.TryGetNextPiece(out var piece))
                    {
                        pieces.Add(piece);
                    }
                    else
                    {
                        break; // No more pieces in puzzle mode
                    }
                }
            }

            // Check for puzzle completion (all pieces used, hand empty)
            if (pieces.Count == 0)
            {
                // Puzzle complete - just flush, don't overwrite the last reward!
                computer.FlushExperienceBuffer();
                return (blocks.Score, blocks.GetGrid());
            }

            // Check if game is stuck (no valid moves)
            if (blocks.IsGameOver(pieces))
            {
                // Failed - just flush
                computer.FlushExperienceBuffer();
                return (blocks.Score, blocks.GetGrid());
            }

            // Get AI move
            var scoreBefore = blocks.Score;
            var move = computer.ChooseMove(blocks, pieces);

            // Place the piece
            if (!blocks.PlacePiece(move.Piece, move.Row, move.Col))
            {
                // Should not happen - AI selected invalid move
                throw new Exception("Computer selected an invalid move during scenario test.");
            }
            pieces.Remove(move.Piece);
            
            // Provide learning feedback for this move
            computer.LearnFromMove(scoreBefore, blocks.Score, false, move.Piece);
        }
    }

    private static void PlaySingleGame(IPlayer player)
    {
        var blocks = new Blocks();
        var generator = new BlockGenerator();
        var pieces = new List<PieceType>();

        // game loop
        while (true)
        {
            // display the board
            PrintGrid(blocks.GetGrid());
            Console.WriteLine($"Score: {blocks.Score} (pieces played {blocks.PiecesPlayed})");

            // check if we need more pieces
            if (pieces.Count == 0)
            {
                for (int i = 0; i < Blocks.NumPiecesInSet; i++)
                {
                    pieces.Add(generator.GetNextPiece());
                }
            }

            // check if game is over (no valid moves left)
            if (blocks.IsGameOver(pieces))
            {
                Console.WriteLine();
                Console.WriteLine("*** GAME OVER ***");
                Console.WriteLine("No more valid moves available!");

                // display the pieces
                for (int i = 0; i < pieces.Count; i++)
                {
                    var shape = pieces[i].GetString(prefix: "   ", suffix: "");
                    Console.WriteLine($"{i}: {pieces[i]}");
                    Console.WriteLine($"{shape}");
                }

                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                break;
            }

            // filter out pieces that are not placeable
            var placeablePieces = new List<PieceType>();
            foreach (var piece in pieces)
            {
                var found = false;
                for (var y = 0; y < Blocks.GridSize && !found; y++)
                {
                    for (var x = 0; x < Blocks.GridSize && !found; x++)
                    {
                        if (blocks.CanPlacePiece(piece, y, x))
                        {
                            placeablePieces.Add(piece);
                            found = true;
                        }
                    }
                }
            }

            // get a move
            var move = player.ChooseMove(blocks, placeablePieces);

            // try to place this move
            if (!blocks.PlacePiece(move.Piece, move.Row, move.Col))
            {
                Console.WriteLine("Invalid placement. Try again.");
                continue;
            }

            // If computer is playing, show the move and pause
            if (player is Computer)
            {
                Console.WriteLine($"Computer placed {move.Piece} at ({move.Row}, {move.Col})");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }

            // remove this piece from the list
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] == move.Piece)
                {
                    pieces.RemoveAt(i);
                    break;
                }
            }
        }
    }

    static void PrintGrid(int[][] grid)
    {
        PrintGrid(grid, indent: "");
    }

    static void PrintGrid(int[][] grid, string indent)
    {
        int size = grid.Length;
        Console.WriteLine(indent + "  " + string.Join(" ", Enumerable.Range(0, size)));
        for (int r = 0; r < size; r++)
        {
            Console.Write(indent + r + " ");
            for (int c = 0; c < size; c++)
            {
                Console.Write(grid[r][c] == 0 ? ". " : "# ");
            }
            Console.WriteLine();
        }
    }
    #endregion
}

