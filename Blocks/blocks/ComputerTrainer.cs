using System;
using System.Collections.Generic;
using System.Net;
using blocks.engine;
using Learning;

public class FitnessResult
{
    public double AvgFitness { get; set; }
    public double MaxFitness { get; set; }
    public double AvgPiecesPlayed { get; set; }
    public int MaxPiecesPlayed { get; set; }
}

public class ComputerTrainer
{
    public ComputerTrainer()
    {
        _random = new Random();
    }

    public static FitnessResult Evaluate(Computer computer, int gameCount = 100)
    {
        if (computer == null) throw new ArgumentNullException(nameof(computer));
        if (gameCount <= 0) throw new ArgumentOutOfRangeException(nameof(gameCount));

        return SimulateGame(computer, gameCount);
    }

    /// <summary>
    /// Trains a single neural network through extensive gameplay
    /// Focus on volume: One network plays thousands of games, learning continuously via policy gradients
    /// </summary>
    /// <param name="totalGames">Total number of games to play for training</param>
    /// <param name="reportInterval">How often to report progress (in games)</param>
    /// <returns>The trained neural network</returns>
    public Computer Train(int totalGames = 10000, int reportInterval = 100)
    {
        Console.WriteLine($"Starting policy gradient training: {totalGames:N0} games");
        Console.WriteLine($"Network: 139 inputs → [128, 64] → 192 outputs");
        Console.WriteLine($"Inputs: board(64) + pieces(75 from 3×5×5)");
        Console.WriteLine($"Strategy: policy prior + short-horizon lookahead\n");

        // Create single network that will learn continuously
        var computer = new Computer();
        
        // Tracking variables
        var bestScore = 0;
        var bestPieces = 0;
        var recentScores = new List<int>();
        var recentPieces = new List<int>();
        var startTime = DateTime.Now;
        
        // Track learning curve: average performance over time buckets
        var bucketSize = 500; // Average every 500 games
        var bucketScores = new List<int>();
        var bucketPieces = new List<int>();
        Console.WriteLine("\nLearning Curve (avg per 500 games):");
        Console.WriteLine("Games     | Avg Score | Avg Pieces | Best Score");
        Console.WriteLine("----------|-----------|------------|------------");
        
        // Training loop - play many games
        for (var gameNum = 1; gameNum <= totalGames; gameNum++)
        {
            // Play one game and let the network learn from it
            var result = SimulateGame(computer, 1);
            
            // Track performance
            recentScores.Add((int)result.MaxFitness);
            recentPieces.Add(result.MaxPiecesPlayed);
            bucketScores.Add((int)result.MaxFitness);
            bucketPieces.Add(result.MaxPiecesPlayed);
            
            // Keep only last 100 games for rolling average
            if (recentScores.Count > 100)
            {
                recentScores.RemoveAt(0);
                recentPieces.RemoveAt(0);
            }
            
            // Display bucket average to track learning curve
            if (gameNum % bucketSize == 0)
            {
                var bucketAvgScore = bucketScores.Average();
                var bucketAvgPieces = bucketPieces.Average();
                Console.WriteLine($"{gameNum,9:N0} | {bucketAvgScore,9:F1} | {bucketAvgPieces,10:F1} | {bestScore,10}");
                bucketScores.Clear();
                bucketPieces.Clear();
                
                // Save checkpoint every 2000 games
                if (gameNum % 2000 == 0)
                {
                    SaveNetwork(computer, $"checkpoint_{gameNum}.txt");
                }
            }
            
            // Track best performance
            if (result.MaxFitness > bestScore)
            {
                bestScore = (int)result.MaxFitness;
                bestPieces = result.MaxPiecesPlayed;
                Console.WriteLine($"\n*** NEW BEST at game {gameNum:N0}: Score {bestScore}, Pieces {bestPieces} ***\n");
            }
            
            // Periodic progress report
            if (gameNum % reportInterval == 0)
            {
                var avgScore = recentScores.Average();
                var avgPieces = recentPieces.Average();
                var elapsed = DateTime.Now - startTime;
                var gamesPerSec = gameNum / elapsed.TotalSeconds;
                var eta = TimeSpan.FromSeconds((totalGames - gameNum) / gamesPerSec);
                
                Console.WriteLine($"Game {gameNum:N0}/{totalGames:N0} | " +
                    $"Recent avg: {avgScore:F0} score, {avgPieces:F1} pieces | " +
                    $"Best: {bestScore} score, {bestPieces} pieces | " +
                    $"Speed: {gamesPerSec:F1} games/sec | " +
                    $"ETA: {eta:hh\\:mm\\:ss}");
            }
        }

        var totalTime = DateTime.Now - startTime;
        Console.WriteLine($"\n=== Training Complete ===");
        Console.WriteLine($"Total time: {totalTime:hh\\:mm\\:ss}");
        Console.WriteLine($"Games played: {totalGames:N0}");
        Console.WriteLine($"Best performance: {bestScore} score, {bestPieces} pieces");
        Console.WriteLine($"Final 100-game avg: {recentScores.Average():F0} score, {recentPieces.Average():F1} pieces");
        Console.WriteLine($"\nLook at the learning curve above:");
        Console.WriteLine($"- If avg score is INCREASING over time → Learning is working!");
        Console.WriteLine($"- If avg score is FLAT or DECREASING → Need to adjust approach");
        
        return computer;
    }

        /// <summary>
    /// Saves a computer's neural network using the built-in Save method
    /// </summary>
    public void SaveNetwork(Computer computer, string filename)
    {
        try
        {
            computer.GetNeuralNetwork().Save(filename);
            Console.WriteLine($"Network saved to {filename}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving network: {ex.Message}");
        }
    }

    #region private
    private readonly Random _random;
    private const int MaxPiecePlacement = 1000;

    private static FitnessResult SimulateGame(Computer computer, int gameCount)
    {
        var totalFitness = 0d;
        var totalPiecesPlayed = 0;
        var maxPiecesPlaced = 0;
        var maxFitness = 0d;
        
        // play all the games
        for (var game = 0; game < gameCount; game++)
        {
            var blocks = new Blocks();
            var generator = new BlockGenerator();
            var pieces = new List<PieceType>();

            // artifical limit to avoid playing for ever
            while (blocks.PiecesPlayed < MaxPiecePlacement)
            {
                // add new pieces
                if (pieces.Count == 0)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        pieces.Add(generator.GetNextPiece());
                    }
                }

                // check if game is over 
                if (blocks.IsGameOver(pieces))
                {
                    // game ended - provide final learning feedback
                    computer.LearnFromMove(blocks.Score, blocks.Score, true);
                    // flush any remaining experiences for learning
                    computer.FlushExperienceBuffer();
                    break;
                }
                
                // choose a piece to play
                var scoreBefore = blocks.Score;
                var move = computer.ChooseMove(blocks, pieces);

                // make the move
                if (blocks.PlacePiece(move.Piece, move.Row, move.Col))
                {
                    var scoreAfter = blocks.Score;
                    pieces.Remove(move.Piece);
                    
                    // Provide learning feedback for this move
                    computer.LearnFromMove(scoreBefore, scoreAfter, false, move.Piece);
                }
                else
                {
                    throw new Exception("Computer selected an invalid move during fitness evaluation.");
                }
            }

            // update the tracking stats
            totalFitness += blocks.Score;
            totalPiecesPlayed += blocks.PiecesPlayed;
            maxFitness = Math.Max(maxFitness, blocks.Score);
            maxPiecesPlaced = Math.Max(maxPiecesPlaced, blocks.PiecesPlayed);
        }
        
        // return fitness
        return new FitnessResult
        {
            AvgFitness = totalFitness / gameCount,
            AvgPiecesPlayed = totalPiecesPlayed / gameCount,
            MaxFitness = maxFitness,
            MaxPiecesPlayed = maxPiecesPlaced
        };
    }
    #endregion
}
