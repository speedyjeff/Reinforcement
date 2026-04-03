using System;
using System.Collections.Generic;
using System.Linq;
using blocks.engine;

public class ComputerClassicRLTrainer
{
    public ComputerClassicRL Train(int totalGames = 10000, int reportInterval = 100)
    {
        Console.WriteLine($"Starting classic RL training: {totalGames:N0} games");
        Console.WriteLine($"Network: 139 inputs → [192, 128, 64] → 192 outputs");
        Console.WriteLine($"Strategy: neural-only policy-gradient learning with curriculum and successful-episode replay\n");

        var computer = new ComputerClassicRL();
        var bestScore = 0;
        var bestPieces = 0;
        var recentScores = new List<int>();
        var recentPieces = new List<int>();
        var startTime = DateTime.Now;

        for (var gameNum = 1; gameNum <= totalGames; gameNum++)
        {
            var generator = CreateTrainingGenerator(gameNum, totalGames);
            var (score, piecesPlayed) = PlayGame(computer, generator);
            recentScores.Add(score);
            recentPieces.Add(piecesPlayed);

            if (recentScores.Count > 100)
            {
                recentScores.RemoveAt(0);
                recentPieces.RemoveAt(0);
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPieces = piecesPlayed;
                Console.WriteLine($"\n*** NEW BEST at game {gameNum:N0}: Score {bestScore}, Pieces {bestPieces} ***\n");
            }

            if (gameNum % reportInterval == 0)
            {
                var avgScore = recentScores.Count > 0 ? recentScores.Average() : 0d;
                var avgPieces = recentPieces.Count > 0 ? recentPieces.Average() : 0d;
                var elapsed = DateTime.Now - startTime;
                var gamesPerSec = gameNum / Math.Max(elapsed.TotalSeconds, 0.001);
                var eta = TimeSpan.FromSeconds((totalGames - gameNum) / gamesPerSec);

                Console.WriteLine($"Game {gameNum:N0}/{totalGames:N0} | " +
                    $"Recent avg: {avgScore:F0} score, {avgPieces:F1} pieces | " +
                    $"Best: {bestScore} score, {bestPieces} pieces | " +
                    $"Speed: {gamesPerSec:F1} games/sec | " +
                    $"ETA: {eta:hh\\:mm\\:ss}");
                Console.WriteLine($"    {computer.GetStats()}");
            }
        }

        Console.WriteLine("\nClassic RL training complete.");
        return computer;
    }

    public void SaveNetwork(ComputerClassicRL computer, string filename)
    {
        if (computer == null) throw new ArgumentNullException(nameof(computer));
        computer.SaveNetwork(filename);
        Console.WriteLine($"Network saved to {filename}");
    }

    public static FitnessResult Evaluate(ComputerClassicRL computer, int gameCount = 100)
    {
        if (computer == null) throw new ArgumentNullException(nameof(computer));
        if (gameCount <= 0) throw new ArgumentOutOfRangeException(nameof(gameCount));

        var evaluator = computer.CreateEvaluationClone();
        var totalFitness = 0d;
        var totalPiecesPlayed = 0;
        var maxFitness = 0d;
        var maxPiecesPlayed = 0;

        for (var game = 0; game < gameCount; game++)
        {
            var (score, piecesPlayed) = PlayGame(evaluator, new BlockGenerator());
            totalFitness += score;
            totalPiecesPlayed += piecesPlayed;
            maxFitness = Math.Max(maxFitness, score);
            maxPiecesPlayed = Math.Max(maxPiecesPlayed, piecesPlayed);
        }

        return new FitnessResult()
        {
            AvgFitness = totalFitness / gameCount,
            MaxFitness = maxFitness,
            AvgPiecesPlayed = totalPiecesPlayed / gameCount,
            MaxPiecesPlayed = maxPiecesPlayed
        };
    }

    private static (int Score, int PiecesPlayed) PlayGame(ComputerClassicRL computer, BlockGenerator generator)
    {
        var blocks = new Blocks();
        var pieces = new List<PieceType>();
        var gameFinished = false;

        while (blocks.PiecesPlayed < 1000)
        {
            if (pieces.Count == 0)
            {
                for (var i = 0; i < Blocks.NumPiecesInSet; i++)
                {
                    pieces.Add(generator.GetNextPiece());
                }
            }

            if (blocks.IsGameOver(pieces))
            {
                computer.FinalizeGame(blocks.Score);
                gameFinished = true;
                break;
            }

            var scoreBefore = blocks.Score;
            var move = computer.ChooseMove(blocks, pieces);

            if (!blocks.PlacePiece(move.Piece, move.Row, move.Col))
            {
                throw new Exception("Classic RL computer selected an invalid move.");
            }

            pieces.Remove(move.Piece);

            if (pieces.Count == 0)
            {
                for (var i = 0; i < Blocks.NumPiecesInSet; i++)
                {
                    if (generator.TryGetNextPiece(out var nextPiece))
                    {
                        pieces.Add(nextPiece);
                    }
                }
            }

            var endedAfterMove = pieces.Count == 0 || blocks.IsGameOver(pieces);
            computer.ObserveMoveOutcome(blocks, pieces, scoreBefore, blocks.Score, endedAfterMove, move.Piece);

            if (endedAfterMove)
            {
                gameFinished = true;
                break;
            }
        }

        if (!gameFinished)
        {
            computer.FinalizeGame(blocks.Score);
        }

        return (blocks.Score, blocks.PiecesPlayed);
    }

    private static BlockGenerator CreateTrainingGenerator(int gameNum, int totalGames)
    {
        var progress = gameNum / (double)Math.Max(totalGames, 1);
        if (progress < 0.20d)
        {
            return new BlockGenerator(maxBoardSize: 3);
        }

        if (progress < 0.50d)
        {
            return new BlockGenerator(maxBoardSize: 4);
        }

        return new BlockGenerator();
    }
}
