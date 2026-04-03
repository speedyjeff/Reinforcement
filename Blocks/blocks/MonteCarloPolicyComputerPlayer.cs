using System;
using System.Collections.Generic;
using System.Linq;
using blocks.engine;
using Learning;

/// <summary>
/// MonteCarloPolicyComputerPlayer: an experimental reinforcement learning agent using Monte Carlo policy gradient.
/// 
/// Key Design Principles:
/// 1. PIECE-AGNOSTIC INPUTS: Only sees board occupancy (64 binary values)
/// 2. POSITION-BASED OUTPUTS: Outputs 64 position preferences (where to place something)
/// 3. END-OF-EPISODE LEARNING: No per-move rewards - only final score matters
/// 4. MONTE CARLO RETURNS: Each state in trajectory gets credit based on final outcome
/// 
/// The network learns general board heuristics like:
/// - "Nearly complete rows/columns are valuable"
/// - "Corners and edges can be traps"
/// - "Keep the board balanced"
/// 
/// Without needing to understand specific piece shapes.
/// </summary>
public class MonteCarloPolicyComputerPlayer : IPlayer
{
    private readonly NeuralNetwork _network;
    private readonly Random _random;

    // Hyperparameters
    private float _epsilon;              // Exploration rate
    private readonly float _epsilonDecay;
    private readonly float _epsilonMin;
    private float _temperature;          // Softmax temperature
    private readonly float _temperatureDecay;
    private readonly float _temperatureMin;

    // Episode trajectory storage
    // Each entry: (boardState, actionIndex, pieceIndex)
    // actionIndex = row * GridSize + col (0-63)
    private readonly List<(float[] state, int positionIndex, int pieceIndex)> _trajectory = new();

    // Network dimensions
    private const int BoardSize = Blocks.GridSize * Blocks.GridSize; // 64
    private const int InputSize = BoardSize;  // Just the board
    private const int OutputSize = BoardSize; // 64 position preferences

    // Statistics
    public int GamesPlayed { get; private set; }
    public float BestScore { get; private set; }
    public float RecentAverageScore { get; private set; }
    private readonly Queue<float> _recentScores = new();
    private const int RecentScoreWindow = 100;

    public MonteCarloPolicyComputerPlayer()
    {
        _random = new Random();

        // Simple network: board state -> position preferences
        var options = new NeuralOptions()
        {
            InputNumber = InputSize,    // 64 board cells
            OutputNumber = OutputSize,  // 64 position preferences
            HiddenLayerNumber = new int[] { 128, 64 }, // Two hidden layers
            LearningRate = 0.001f,      // Standard learning rate
            MinibatchCount = 1,
            WeightInitialization = NeuralWeightInitialization.Xavier,
            BiasInitialization = NeuralBiasInitialization.Zero
        };
        _network = new NeuralNetwork(options);

        // Exploration parameters
        _epsilon = 0.3f;           // Start with 30% random exploration
        _epsilonDecay = 0.9995f;
        _epsilonMin = 0.05f;
        _temperature = 1.0f;       // Start with high temperature
        _temperatureDecay = 0.9998f;
        _temperatureMin = 0.3f;
    }

    public MonteCarloPolicyComputerPlayer(NeuralNetwork network)
    {
        _network = network ?? throw new ArgumentNullException(nameof(network));
        _random = new Random();

        // Pre-trained: less exploration
        _epsilon = 0.1f;
        _epsilonDecay = 0.9999f;
        _epsilonMin = 0.02f;
        _temperature = 0.5f;
        _temperatureDecay = 0.9999f;
        _temperatureMin = 0.2f;
    }

    /// <summary>
    /// Choose a move by:
    /// 1. Encoding current board state
    /// 2. Getting position preferences from network
    /// 3. Finding valid moves and selecting based on position preference
    /// </summary>
    public Move ChooseMove(Blocks blocks, List<PieceType> pieces)
    {
        // Step 1: Encode board state (just occupancy)
        float[] boardState = EncodeBoardState(blocks);

        // Step 2: Get position preferences from network
        var output = _network.Evaluate(boardState);
        float[] positionLogits = output.Probabilities;

        // Step 3: Find all valid moves
        var validMoves = new List<(Move move, int positionIndex, int pieceIndex)>();
        
        for (int pieceIdx = 0; pieceIdx < pieces.Count; pieceIdx++)
        {
            var piece = pieces[pieceIdx];
            for (int row = 0; row < Blocks.GridSize; row++)
            {
                for (int col = 0; col < Blocks.GridSize; col++)
                {
                    if (blocks.CanPlacePiece(piece, row, col))
                    {
                        int positionIndex = row * Blocks.GridSize + col;
                        validMoves.Add((new Move(piece, row, col), positionIndex, pieceIdx));
                    }
                }
            }
        }

        if (validMoves.Count == 0)
        {
            throw new InvalidOperationException("No valid moves available");
        }

        // Step 4: Select move
        Move selectedMove;
        int selectedPositionIndex;
        int selectedPieceIndex;

        if (_random.NextDouble() < _epsilon)
        {
            // Random exploration
            int idx = _random.Next(validMoves.Count);
            selectedMove = validMoves[idx].move;
            selectedPositionIndex = validMoves[idx].positionIndex;
            selectedPieceIndex = validMoves[idx].pieceIndex;
        }
        else
        {
            // Use network preferences
            // For each valid move, get the position preference
            // If multiple pieces can go to same position, we just use position preference
            // (piece-agnostic: we don't care which piece, just where)
            
            // Build masked logits for valid positions
            float[] maskedLogits = new float[OutputSize];
            for (int i = 0; i < OutputSize; i++)
            {
                maskedLogits[i] = float.MinValue; // Invalid positions
            }

            // Set valid position logits (take max if multiple pieces can use same position)
            foreach (var (move, posIndex, pieceIdx) in validMoves)
            {
                maskedLogits[posIndex] = Math.Max(maskedLogits[posIndex], 
                    positionLogits[posIndex] / _temperature);
            }

            // Softmax over positions
            float[] positionProbs = Softmax(maskedLogits);

            // Group valid moves by position
            var movesByPosition = validMoves
                .GroupBy(m => m.positionIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Sample a position based on probabilities
            var validPositions = movesByPosition.Keys.ToList();
            float[] validProbs = validPositions.Select(p => positionProbs[p]).ToArray();
            
            // Normalize
            float sum = validProbs.Sum();
            if (sum > 0)
            {
                for (int i = 0; i < validProbs.Length; i++)
                    validProbs[i] /= sum;
            }
            else
            {
                // Uniform if all zero
                for (int i = 0; i < validProbs.Length; i++)
                    validProbs[i] = 1f / validProbs.Length;
            }

            int posIdx = SampleFromDistribution(validProbs);
            int chosenPosition = validPositions[posIdx];

            // From the moves at this position, pick one (could be random or first)
            var movesAtPosition = movesByPosition[chosenPosition];
            int moveIdx = _random.Next(movesAtPosition.Count);
            
            selectedMove = movesAtPosition[moveIdx].move;
            selectedPositionIndex = movesAtPosition[moveIdx].positionIndex;
            selectedPieceIndex = movesAtPosition[moveIdx].pieceIndex;
        }

        // Step 5: Store in trajectory (reward computed at episode end)
        _trajectory.Add((boardState, selectedPositionIndex, selectedPieceIndex));

        // Decay exploration
        _epsilon = Math.Max(_epsilonMin, _epsilon * _epsilonDecay);
        _temperature = Math.Max(_temperatureMin, _temperature * _temperatureDecay);

        return selectedMove;
    }

    /// <summary>
    /// Encode the board as 64 binary values (1 = occupied, 0 = empty)
    /// </summary>
    private float[] EncodeBoardState(Blocks blocks)
    {
        float[] state = new float[BoardSize];
        var grid = blocks.GetGrid();
        for (int row = 0; row < Blocks.GridSize; row++)
        {
            for (int col = 0; col < Blocks.GridSize; col++)
            {
                int idx = row * Blocks.GridSize + col;
                state[idx] = grid[row][col] != 0 ? 1f : 0f;
            }
        }
        return state;
    }

    /// <summary>
    /// Called at game end. Applies Monte Carlo policy gradient update.
    /// Each state in the trajectory is reinforced based on the final score.
    /// 
    /// The key insight: if the game ended with a high score, ALL moves
    /// that led there were "good" moves, even if they didn't immediately
    /// score points.
    /// </summary>
    public void LearnFromGame(int finalScore)
    {
        if (_trajectory.Count == 0) return;

        GamesPlayed++;
        
        // Track statistics
        _recentScores.Enqueue(finalScore);
        if (_recentScores.Count > RecentScoreWindow)
            _recentScores.Dequeue();
        RecentAverageScore = _recentScores.Average();
        BestScore = Math.Max(BestScore, finalScore);

        // Normalize the score to create a learning signal
        // We use advantage-style normalization: how much better/worse than average?
        float normalizedScore = NormalizeScore(finalScore);

        // Monte Carlo Policy Gradient:
        // For each (state, action) pair in the trajectory, reinforce with:
        //   ∇θ J(θ) ≈ Σ ∇θ log π(a|s) * G
        // Where G is the return (normalized final score)
        
        foreach (var (state, positionIndex, _) in _trajectory)
        {
            // Create target: encourage the chosen position
            // The network output is position logits; we want to increase
            // logit for chosen position proportional to how good the outcome was
            
            float[] target = new float[OutputSize];
            
            // Soft target: chosen position gets the normalized score as signal
            // Positive score = reinforce this choice
            // Negative score = discourage this choice
            target[positionIndex] = normalizedScore;

            // Train the network: evaluate first, then learn toward target
            var output = _network.Evaluate(state);
            _network.Learn(output, target);
        }

        // Clear trajectory for next game
        _trajectory.Clear();
    }

    /// <summary>
    /// Normalize score for learning signal.
    /// Returns positive values for above-average games, negative for below.
    /// </summary>
    private float NormalizeScore(int finalScore)
    {
        if (_recentScores.Count < 10)
        {
            // Early in training: just use raw score scaled down
            return finalScore / 100f;
        }

        // Advantage-style: how much better than recent average?
        float advantage = finalScore - RecentAverageScore;
        
        // Scale to reasonable range (-1 to 1 ish)
        float stdDev = CalculateStdDev();
        if (stdDev > 0)
        {
            return advantage / (stdDev + 1f);
        }
        return advantage / 50f; // Fallback scaling
    }

    private float CalculateStdDev()
    {
        if (_recentScores.Count < 2) return 0;
        float avg = _recentScores.Average();
        float variance = _recentScores.Sum(s => (s - avg) * (s - avg)) / _recentScores.Count;
        return (float)Math.Sqrt(variance);
    }

    /// <summary>
    /// Clear the current trajectory without learning (e.g., for aborted games)
    /// </summary>
    public void ResetTrajectory()
    {
        _trajectory.Clear();
    }

    private float[] Softmax(float[] logits)
    {
        float max = logits.Max();
        float[] exp = logits.Select(x => (float)Math.Exp(x - max)).ToArray();
        float sum = exp.Sum();
        return exp.Select(x => x / sum).ToArray();
    }

    private int SampleFromDistribution(float[] probabilities)
    {
        float r = (float)_random.NextDouble();
        float cumulative = 0f;
        for (int i = 0; i < probabilities.Length; i++)
        {
            cumulative += probabilities[i];
            if (r <= cumulative)
                return i;
        }
        return probabilities.Length - 1;
    }

    /// <summary>
    /// Get the underlying network for saving/loading
    /// </summary>
    public NeuralNetwork GetNetwork() => _network;

    /// <summary>
    /// Get current exploration parameters
    /// </summary>
    public (float epsilon, float temperature) GetExplorationParams() => (_epsilon, _temperature);

    /// <summary>
    /// Get training statistics
    /// </summary>
    public string GetStats()
    {
        return $"Games: {GamesPlayed}, Best: {BestScore:F0}, Recent Avg: {RecentAverageScore:F1}, " +
               $"ε: {_epsilon:F3}, τ: {_temperature:F3}";
    }
}

/// <summary>
/// Backward-compatible alias for older code. Prefer <see cref="MonteCarloPolicyComputerPlayer"/>.
/// </summary>
public class Computer2 : MonteCarloPolicyComputerPlayer
{
    public Computer2()
    {
    }

    public Computer2(NeuralNetwork network)
        : base(network)
    {
    }
}
