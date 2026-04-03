using System;
using System.Collections.Generic;
using System.Linq;
using blocks.engine;
using Learning;

/// <summary>
/// A neural-only Blocks player that learns with classic deep Q-learning.
///
/// Unlike the main Computer class, this agent does not use any handcrafted
/// heuristics or search. It learns purely from:
/// 1. board + current pieces as neural-network input
/// 2. rewards from score changes and terminal outcomes
/// 3. temporal-difference updates with replay memory
/// </summary>
public class ComputerClassicRL : IPlayer
{
    private readonly NeuralNetwork _policyNetwork;
    private readonly Random _random;
    private readonly bool _trainingEnabled;
    private readonly List<EpisodeStep> _currentEpisode;
    private readonly List<List<EpisodeStep>> _successfulEpisodes;

    private float _bestEpisodeReward;

    private float _epsilon;
    private readonly float _epsilonDecay;
    private readonly float _epsilonMin;
    private float _temperature;
    private readonly float _temperatureDecay;
    private readonly float _temperatureMin;
    private readonly float _gamma;

    private const int MaxPieceSize = 5;
    private const int PieceEncodingSize = MaxPieceSize * MaxPieceSize;
    private const int NumPieces = 3;
    private const int BoardInputSize = Blocks.GridSize * Blocks.GridSize; // 64
    private const int PiecesInputSize = NumPieces * PieceEncodingSize; // 75
    private const int TotalInputSize = BoardInputSize + PiecesInputSize; // 139
    private const int ActionSpaceSize = BoardInputSize * NumPieces; // 192
    private const int RecentScoreWindow = 100;
    private const int MaxSuccessfulEpisodes = 40;
    private const int ReplaySampleCount = 3;
    private const int ReplayCount = 2;
    private const int MaxReplayMovesPerEpisode = 12;

    public ComputerClassicRL()
    {
        _random = new Random();
        _trainingEnabled = true;
        _currentEpisode = new List<EpisodeStep>();
        _successfulEpisodes = new List<List<EpisodeStep>>();
        _gamma = 0.99f;
        _epsilon = 0.20f;
        _epsilonDecay = 0.9996f;
        _epsilonMin = 0.02f;
        _temperature = 0.45f;
        _temperatureDecay = 0.9997f;
        _temperatureMin = 0.10f;

        var options = new NeuralOptions()
        {
            InputNumber = TotalInputSize,
            OutputNumber = ActionSpaceSize,
            HiddenLayerNumber = new int[] { 192, 128, 64 },
            LearningRate = 0.0005f,
            MinibatchCount = 1,
            WeightInitialization = NeuralWeightInitialization.Xavier,
            BiasInitialization = NeuralBiasInitialization.Zero
        };

        _policyNetwork = new NeuralNetwork(options);
    }

    public ComputerClassicRL(NeuralNetwork network, bool enableLearning = false)
    {
        _policyNetwork = network ?? throw new ArgumentNullException(nameof(network));
        _random = new Random();
        _trainingEnabled = enableLearning;
        _currentEpisode = new List<EpisodeStep>();
        _successfulEpisodes = new List<List<EpisodeStep>>();
        _gamma = 0.99f;
        _epsilon = enableLearning ? 0.05f : 0f;
        _epsilonDecay = enableLearning ? 0.9998f : 1f;
        _epsilonMin = enableLearning ? 0.01f : 0f;
        _temperature = enableLearning ? 0.25f : 0f;
        _temperatureDecay = enableLearning ? 0.9999f : 1f;
        _temperatureMin = enableLearning ? 0.08f : 0f;
    }

    public int GamesPlayed { get; private set; }
    public float BestScore { get; private set; }
    public float RecentAverageScore { get; private set; }

    public Move ChooseMove(Blocks blocks, List<PieceType> pieces)
    {
        var state = CreateNetworkInput(blocks, pieces);
        var output = _policyNetwork.Evaluate(state);
        var candidates = GetValidMoves(blocks, pieces, output.Probabilities)
            .OrderByDescending(candidate => candidate.Value)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No valid moves available for the classic RL computer player.");
        }

        CandidateMove selected;
        if (!_trainingEnabled || candidates.Count == 1)
        {
            selected = candidates[0];
        }
        else if (_random.NextDouble() < _epsilon)
        {
            var explorationPoolSize = Math.Min(Math.Max(4, candidates.Count / 3), candidates.Count);
            selected = candidates[_random.Next(explorationPoolSize)];
        }
        else
        {
            var scaledLogits = candidates
                .Select(candidate => candidate.Value / Math.Max(_temperature, 0.05f))
                .ToArray();
            var probabilities = Softmax(scaledLogits);
            selected = candidates[SampleFromDistribution(probabilities)];
        }

        if (_trainingEnabled)
        {
            _currentEpisode.Add(new EpisodeStep()
            {
                State = state,
                Action = selected.ActionIndex,
                ValidActions = candidates.Select(candidate => candidate.ActionIndex).ToArray(),
                Reward = 0f
            });

            _epsilon = Math.Max(_epsilonMin, _epsilon * _epsilonDecay);
            _temperature = Math.Max(_temperatureMin, _temperature * _temperatureDecay);
        }

        return selected.Move;
    }

    public void ObserveMoveOutcome(Blocks nextBlocks, List<PieceType> nextPieces, int scoreBefore, int scoreAfter, bool gameEnded, PieceType playedPiece)
    {
        if (!_trainingEnabled || _currentEpisode.Count == 0)
        {
            if (gameEnded)
            {
                FinalizeGame(scoreAfter);
            }

            return;
        }

        var reward = CalculateReward(nextBlocks, nextPieces, scoreBefore, scoreAfter, gameEnded, playedPiece);
        var lastStep = _currentEpisode[_currentEpisode.Count - 1];
        lastStep.Reward += reward;
        _currentEpisode[_currentEpisode.Count - 1] = lastStep;

        if (gameEnded)
        {
            FinalizeGame(scoreAfter);
        }
    }

    public void FinalizeGame(int finalScore)
    {
        GamesPlayed++;
        BestScore = Math.Max(BestScore, finalScore);

        _recentScores.Enqueue(finalScore);
        if (_recentScores.Count > RecentScoreWindow)
        {
            _recentScores.Dequeue();
        }

        RecentAverageScore = _recentScores.Average();
        if (_trainingEnabled)
        {
            FlushEpisodeAndLearn(finalScore);
        }

        ResetEpisode();
    }

    public void ResetEpisode()
    {
        _currentEpisode.Clear();
    }

    public NeuralNetwork GetNetwork() => _policyNetwork;

    public ComputerClassicRL CreateEvaluationClone()
    {
        return new ComputerClassicRL(NeuralNetwork.Load(_policyNetwork), enableLearning: false);
    }

    public void SaveNetwork(string filePath)
    {
        _policyNetwork.Save(filePath);
    }

    public string GetStats()
    {
        return $"Games: {GamesPlayed}, Best: {BestScore:F0}, Recent Avg: {RecentAverageScore:F1}, ε: {_epsilon:F3}, temp: {_temperature:F2}";
    }

    #region private

    private readonly Queue<float> _recentScores = new();

    private sealed class EpisodeStep
    {
        public float[] State = Array.Empty<float>();
        public int Action;
        public int[] ValidActions = Array.Empty<int>();
        public float Reward;
    }

    private readonly struct CandidateMove
    {
        public CandidateMove(Move move, int actionIndex, float value)
        {
            Move = move;
            ActionIndex = actionIndex;
            Value = value;
        }

        public Move Move { get; }
        public int ActionIndex { get; }
        public float Value { get; }
    }

    private float CalculateReward(Blocks nextBlocks, List<PieceType> nextPieces, int scoreBefore, int scoreAfter, bool gameEnded, PieceType playedPiece)
    {
        var scoreGain = scoreAfter - scoreBefore;
        var placementScore = playedPiece.GetShape().Count;
        var clearBonus = Math.Max(0, scoreGain - placementScore);
        var mobility = GetValidActionIndices(nextBlocks, nextPieces).Count;
        var linePressure = MeasureLinePressure(nextBlocks);

        var reward =
            (placementScore * 0.05f) +
            (scoreGain / 8f) +
            (clearBonus * 0.90f) +
            (Math.Min(mobility, 18) * 0.04f) +
            (linePressure * 0.04f);

        if (gameEnded)
        {
            reward -= 4f;
        }

        return Math.Clamp(reward, -4f, 6f);
    }

    private void FlushEpisodeAndLearn(int finalScore)
    {
        if (_currentEpisode.Count == 0)
        {
            return;
        }

        var episodeCopy = _currentEpisode
            .Select(step => new EpisodeStep()
            {
                State = (float[])step.State.Clone(),
                Action = step.Action,
                ValidActions = (int[])step.ValidActions.Clone(),
                Reward = step.Reward
            })
            .ToList();

        var totalEpisodeReward = episodeCopy.Sum(step => step.Reward);
        if (totalEpisodeReward >= _bestEpisodeReward * 0.85f || finalScore >= RecentAverageScore)
        {
            _successfulEpisodes.Add(episodeCopy);
            _bestEpisodeReward = Math.Max(_bestEpisodeReward, totalEpisodeReward);

            if (_successfulEpisodes.Count > MaxSuccessfulEpisodes)
            {
                var weakestEpisode = _successfulEpisodes
                    .OrderBy(episode => episode.Sum(step => step.Reward))
                    .First();
                _successfulEpisodes.Remove(weakestEpisode);
            }
        }

        LearnFromEpisode(episodeCopy, repeatCount: 1);

        var replayEpisodes = _successfulEpisodes
            .OrderByDescending(_ => _random.Next())
            .Take(Math.Min(ReplaySampleCount, _successfulEpisodes.Count))
            .ToList();

        foreach (var successfulEpisode in replayEpisodes)
        {
            LearnFromEpisode(successfulEpisode, ReplayCount);
        }
    }

    private void LearnFromEpisode(List<EpisodeStep> episode, int repeatCount)
    {
        if (episode.Count == 0)
        {
            return;
        }

        var returns = new float[episode.Count];
        float runningReturn = 0f;
        for (var i = episode.Count - 1; i >= 0; i--)
        {
            runningReturn = episode[i].Reward + (_gamma * runningReturn);
            returns[i] = runningReturn;
        }

        var averageReturn = returns.Average();
        var variance = returns.Select(value => Math.Pow(value - averageReturn, 2)).Average();
        var standardDeviation = (float)Math.Sqrt(Math.Max(variance, 0d));

        var promisingMoves = Enumerable.Range(0, episode.Count)
            .Where(index => returns[index] >= averageReturn || episode[index].Reward > 0.20f)
            .OrderByDescending(index => returns[index])
            .Take(MaxReplayMovesPerEpisode)
            .ToList();

        if (promisingMoves.Count == 0)
        {
            promisingMoves.Add(Array.IndexOf(returns, returns.Max()));
        }

        for (var repeat = 0; repeat < repeatCount; repeat++)
        {
            foreach (var moveIndex in promisingMoves)
            {
                TrainOnStep(episode[moveIndex], returns[moveIndex], averageReturn, standardDeviation);
            }
        }
    }

    private void TrainOnStep(EpisodeStep step, float discountedReturn, float averageReturn, float standardDeviation)
    {
        var output = _policyNetwork.Evaluate(step.State);
        var advantage = standardDeviation > 0.001f
            ? (discountedReturn - averageReturn) / (standardDeviation + 1f)
            : discountedReturn - averageReturn;

        var targetValues = CreatePolicyTarget(output.Probabilities, step.ValidActions, step.Action, advantage);
        _policyNetwork.Learn(output, targetValues);
    }

    private static float[] CreatePolicyTarget(float[] currentProbabilities, int[] validActions, int chosenAction, float advantage)
    {
        var target = new float[currentProbabilities.Length];
        var actions = validActions
            .Where(action => action >= 0 && action < currentProbabilities.Length)
            .Distinct()
            .ToArray();

        if (actions.Length == 0)
        {
            target[chosenAction] = 1f;
            return target;
        }

        var otherActions = actions.Where(action => action != chosenAction).ToArray();
        if (otherActions.Length == 0)
        {
            target[chosenAction] = 1f;
            return target;
        }

        var desiredChosenProbability = advantage >= 0f
            ? Math.Clamp(0.55f + (advantage * 0.12f), 0.55f, 0.92f)
            : Math.Clamp(0.20f + ((advantage + 1.5f) * 0.08f), 0.05f, 0.35f);

        var remainingProbability = 1f - desiredChosenProbability;
        var denominator = otherActions.Sum(action => Math.Max(currentProbabilities[action], 0.0001f));
        if (denominator <= 0f)
        {
            var fallbackProbability = remainingProbability / otherActions.Length;
            foreach (var action in otherActions)
            {
                target[action] = fallbackProbability;
            }
        }
        else
        {
            foreach (var action in otherActions)
            {
                target[action] = remainingProbability * Math.Max(currentProbabilities[action], 0.0001f) / denominator;
            }
        }

        target[chosenAction] = desiredChosenProbability;
        return target;
    }

    private static float[] Softmax(float[] logits)
    {
        var max = logits.Max();
        var exps = logits.Select(value => Math.Exp(value - max)).ToArray();
        var sum = exps.Sum();
        return exps.Select(value => (float)(value / sum)).ToArray();
    }

    private int SampleFromDistribution(float[] probabilities)
    {
        var randomValue = (float)_random.NextDouble();
        var cumulative = 0f;

        for (var i = 0; i < probabilities.Length; i++)
        {
            cumulative += probabilities[i];
            if (randomValue <= cumulative)
            {
                return i;
            }
        }

        return probabilities.Length - 1;
    }

    private static List<CandidateMove> GetValidMoves(Blocks blocks, List<PieceType> pieces, float[] actionValues)
    {
        var validMoves = new List<CandidateMove>();

        for (var pieceIdx = 0; pieceIdx < pieces.Count && pieceIdx < NumPieces; pieceIdx++)
        {
            var piece = pieces[pieceIdx];
            for (var row = 0; row < Blocks.GridSize; row++)
            {
                for (var col = 0; col < Blocks.GridSize; col++)
                {
                    if (blocks.CanPlacePiece(piece, row, col))
                    {
                        var actionIndex = GetActionIndex(pieceIdx, row, col);
                        validMoves.Add(new CandidateMove(new Move(piece, row, col), actionIndex, actionValues[actionIndex]));
                    }
                }
            }
        }

        return validMoves;
    }

    private static List<int> GetValidActionIndices(Blocks blocks, List<PieceType> pieces)
    {
        var actions = new List<int>();

        for (var pieceIdx = 0; pieceIdx < pieces.Count && pieceIdx < NumPieces; pieceIdx++)
        {
            var piece = pieces[pieceIdx];
            for (var row = 0; row < Blocks.GridSize; row++)
            {
                for (var col = 0; col < Blocks.GridSize; col++)
                {
                    if (blocks.CanPlacePiece(piece, row, col))
                    {
                        actions.Add(GetActionIndex(pieceIdx, row, col));
                    }
                }
            }
        }

        return actions;
    }

    private static float MeasureLinePressure(Blocks blocks)
    {
        var grid = blocks.GetGrid();
        float pressure = 0f;

        for (var row = 0; row < Blocks.GridSize; row++)
        {
            var filled = 0;
            for (var col = 0; col < Blocks.GridSize; col++)
            {
                if (grid[row][col] != 0)
                {
                    filled++;
                }
            }

            if (filled >= Blocks.GridSize - 2)
            {
                pressure += filled;
            }
        }

        for (var col = 0; col < Blocks.GridSize; col++)
        {
            var filled = 0;
            for (var row = 0; row < Blocks.GridSize; row++)
            {
                if (grid[row][col] != 0)
                {
                    filled++;
                }
            }

            if (filled >= Blocks.GridSize - 2)
            {
                pressure += filled;
            }
        }

        return pressure;
    }

    private static int GetActionIndex(int pieceIndex, int row, int col)
    {
        return (pieceIndex * BoardInputSize) + (row * Blocks.GridSize) + col;
    }

    private static float[] CreateNetworkInput(Blocks blocks, List<PieceType> pieces)
    {
        var input = new float[TotalInputSize];
        var grid = blocks.GetGrid();
        var index = 0;

        for (var row = 0; row < Blocks.GridSize; row++)
        {
            for (var col = 0; col < Blocks.GridSize; col++)
            {
                input[index++] = grid[row][col] != 0 ? 1f : 0f;
            }
        }

        for (var pieceIndex = 0; pieceIndex < NumPieces; pieceIndex++)
        {
            if (pieceIndex < pieces.Count)
            {
                var shape = pieces[pieceIndex].GetShape();
                var pieceGrid = new float[MaxPieceSize, MaxPieceSize];

                foreach (var (dr, dc) in shape)
                {
                    if (dr >= 0 && dr < MaxPieceSize && dc >= 0 && dc < MaxPieceSize)
                    {
                        pieceGrid[dr, dc] = 1f;
                    }
                }

                for (var row = 0; row < MaxPieceSize; row++)
                {
                    for (var col = 0; col < MaxPieceSize; col++)
                    {
                        input[index++] = pieceGrid[row, col];
                    }
                }
            }
            else
            {
                for (var i = 0; i < PieceEncodingSize; i++)
                {
                    input[index++] = 0f;
                }
            }
        }

        return input;
    }

    #endregion
}
