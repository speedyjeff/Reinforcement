using System;
using System.Collections.Generic;
using System.Linq;
using blocks.engine;
using Learning;

/// <summary>
/// Hybrid Blocks agent that combines a learned policy prior with explicit move planning.
///
/// Methodology:
/// 1. Encode the current board plus the three available piece shapes into a 139-value input vector.
/// 2. Use the neural network to produce a prior score over the 192 possible piece-slot/board-position actions.
/// 3. Enumerate only legal moves, then rescore them with domain-aware signals such as immediate score gain,
///    line-clear potential, board health, dead-end avoidance, and short-horizon lookahead.
/// 4. Choose among the strongest candidates with limited exploration during training and near-deterministic play
///    for evaluation / saved models.
/// 5. Learn from full-game trajectories, reinforcing moves that appear in higher-return episodes.
///
/// In practice this is intentionally not a pure end-to-end RL player. The network supplies a useful policy prior,
/// but the final strength comes from combining that learned signal with search and board-quality heuristics.
/// </summary>
public class HybridComputerPlayer : IPlayer
{
    private readonly Learning.NeuralNetwork _neuralNetwork;
    private readonly Random _random;
    private readonly bool _learningEnabled;

    // Policy Gradient hyperparameters
    private readonly float _gamma; // Discount factor for future rewards
    private float _temperature; // Temperature for exploration (higher = more random)
    private readonly float _temperatureDecay;
    private readonly float _temperatureMin;
    
    // ε-greedy exploration
    private float _epsilon; // Probability of random action
    private readonly float _epsilonDecay;
    private readonly float _epsilonMin;

    // Episode memory for current game
    private readonly List<(float[] state, int action, float reward)> _currentEpisode = new();
    private const int MAX_EPISODE_LENGTH = 1000;
    
    // Experience replay: store successful trajectories and replay them
    private readonly List<List<(float[] state, int action, float reward)>> _successfulEpisodes = new();
    private const int MAX_SUCCESSFUL_EPISODES = 50; // Keep top 50 successful games for longer episodes
    private const int REPLAY_COUNT = 3; // Replay each successful game 3 times (less overfitting)
    private float _bestEpisodeScore = 0;

    // Piece encoding constants
    private const int MaxPieceSize = 5; // Maximum dimension of any piece (5x5)
    private const int PieceEncodingSize = MaxPieceSize * MaxPieceSize; // 25 values per piece
    private const int NumPieces = 3;
    private const int BoardInputSize = Blocks.GridSize * Blocks.GridSize; // 64 for 8x8
    private const int PiecesInputSize = NumPieces * PieceEncodingSize; // 3 * 25 = 75
    private const int TotalInputSize = BoardInputSize + PiecesInputSize; // 64 + 75 = 139
    private const int ActionSpaceSize = BoardInputSize * NumPieces; // 64 * 3 = 192
    private const int SearchDepth = 2;
    private const int SearchBeamWidth = 10;
    private const float TerminalPenalty = 12f;
    private const float DeadEndPenalty = 90f;
    private static readonly (PieceType piece, float weight)[] FutureMobilityPieces =
    {
        (PieceType.Square3x3, 6.0f),
        (PieceType.Horizontal5, 5.0f),
        (PieceType.Vertical5, 5.0f),
        (PieceType.Horizontal2x3, 4.5f),
        (PieceType.Vertical2x3, 4.5f),
        (PieceType.Horizontal4, 4.0f),
        (PieceType.Vertical4, 4.0f),
        (PieceType.Square2x2, 3.5f),
        (PieceType.BigTUp, 3.0f),
        (PieceType.BigLTopLeft, 3.0f),
        (PieceType.BigCornerTopLeft, 3.0f),
        (PieceType.TUp, 2.5f),
        (PieceType.ZUp, 2.5f),
        (PieceType.CornerTopLeft, 2.0f),
        (PieceType.Horizontal3, 1.5f),
        (PieceType.Vertical3, 1.5f),
        (PieceType.Single, 0.5f)
    };

    private readonly struct CandidateMove
    {
        public CandidateMove(Move move, int actionIndex, float score)
        {
            Move = move;
            ActionIndex = actionIndex;
            Score = score;
        }

        public Move Move { get; }
        public int ActionIndex { get; }
        public float Score { get; }
    }

    public HybridComputerPlayer()
    {
        _random = new Random();
        _learningEnabled = true;
        
        // Policy Gradient Neural Network Architecture (MINIMAL):
        // Input: Board state + Piece shapes only
        // - Board state: 64 values (8×8 grid, binary occupied/empty)
        // - Piece shapes: 3 pieces × 5×5 grid = 75 values (spatial encoding)
        // Total Input: 64 + 75 = 139
        // 
        // Key design: NO valid action mask!
        // - Validity is mechanical (geometry), not strategic
        // - We filter invalid actions in code before sampling
        // - Network learns pure strategy: "which valid move is best?"
        // - Smaller network = faster learning, less overfitting
        // 
        // Network must discover:
        // - Clearing lines is valuable
        // - Piece placement order matters
        // - Nesting pieces efficiently
        // - Avoiding fragmentation
        // 
        // Output: 192 action logits (8×8×3 = one logit per possible action)
        // - 64 positions × 3 pieces = 192 possible moves
        // - Invalid actions masked to -1000 before softmax
        var options = new NeuralOptions()
        {
            InputNumber = TotalInputSize, // Board (64) + piece shapes (75)
            OutputNumber = ActionSpaceSize, // Action logits for policy (8x8x3)
            HiddenLayerNumber = new int[] { 128, 64 }, // Two hidden layers for more capacity
            LearningRate = 0.0005f, // Moderate learning rate for stable convergence
            MinibatchCount = 1,
            WeightInitialization = NeuralWeightInitialization.Xavier,
            BiasInitialization = NeuralBiasInitialization.Zero
        };
        _neuralNetwork = new Learning.NeuralNetwork(options);
        
        // Policy Gradient hyperparameters
        _gamma = 0.99f; // Discount factor for future rewards
        _epsilon = 0.08f; // Explore, but bias exploration toward strong candidate moves
        _epsilonDecay = 0.9998f;
        _epsilonMin = 0.01f;
        _temperature = 0.22f;
        _temperatureDecay = 0.9998f;
        _temperatureMin = 0.05f;
    }

    public HybridComputerPlayer(Learning.NeuralNetwork neuralNetwork)
    {
        _neuralNetwork = neuralNetwork ?? throw new ArgumentNullException(nameof(neuralNetwork));
        _random = new Random();
        _learningEnabled = false;
        _gamma = 0.99f;
        _epsilon = 0.0f; // Pre-trained play should be deterministic
        _epsilonDecay = 1.0f;
        _epsilonMin = 0.0f;
        _temperature = 0.0f;
        _temperatureDecay = 1.0f;
        _temperatureMin = 0.0f;
    }

    public Move ChooseMove(Blocks blocks, List<PieceType> pieces)
    {
        // STEP 1: Encode the current state
        var state = CreateNetworkInput(blocks, pieces);
        
        // STEP 2: Get policy (action probabilities) from network
        var output = _neuralNetwork.Evaluate(state);
        var actionLogits = output.Probabilities; // Raw network outputs (logits)
        
        // STEP 3: Build list of valid moves with their action indices
        var candidates = new List<CandidateMove>();
        
        for (int pieceIdx = 0; pieceIdx < pieces.Count && pieceIdx < 3; pieceIdx++)
        {
            var piece = pieces[pieceIdx];
            for (var row = 0; row < Blocks.GridSize; row++)
            {
                for (var col = 0; col < Blocks.GridSize; col++)
                {
                    if (blocks.CanPlacePiece(piece, row, col))
                    {
                        // Action index: pieceIdx * BoardInputSize + row * GridSize + col
                        int actionIndex = pieceIdx * BoardInputSize + row * Blocks.GridSize + col;
                        var move = new Move(piece, row, col);
                        var candidateScore = EvaluateCandidateScore(blocks, pieces, pieceIdx, move, actionLogits[actionIndex]);
                        candidates.Add(new CandidateMove(move, actionIndex, candidateScore));
                    }
                }
            }
        }

        if (candidates.Count == 0)
        {
            throw new Exception("No valid moves available for the computer player.");
        }

        var rankedCandidates = candidates
            .OrderByDescending(c => c.Score)
            .ToList();

        CandidateMove selectedCandidate;

        // ε-GREEDY: explore within the strongest few moves rather than blindly anywhere
        if (_random.NextDouble() < _epsilon)
        {
            var explorationPool = Math.Min(SearchBeamWidth, rankedCandidates.Count);
            selectedCandidate = rankedCandidates[_random.Next(explorationPool)];
        }
        else
        {
            // Trained networks should be mostly deterministic; during training we keep
            // some stochasticity to continue exploring promising alternatives.
            if (_temperature <= 0.10f || rankedCandidates.Count == 1)
            {
                selectedCandidate = rankedCandidates[0];
            }
            else
            {
                var logits = rankedCandidates
                    .Select(c => c.Score / _temperature)
                    .ToArray();
                var probabilities = Softmax(logits);
                selectedCandidate = rankedCandidates[SampleFromDistribution(probabilities)];
            }
        }
        
        // STEP 6: Store (state, action) in current episode - reward will be added later
        _currentEpisode.Add((state, selectedCandidate.ActionIndex, 0f)); // Reward added in LearnFromMove
        
        // Decay exploration parameters for less randomness over time
        _epsilon = Math.Max(_epsilonMin, _epsilon * _epsilonDecay);
        _temperature = Math.Max(_temperatureMin, _temperature * _temperatureDecay);
        
        return selectedCandidate.Move;
    }

    // Call this after a move is made to record the reward (learning happens at episode end)
    public void LearnFromMove(int scoreBefore, int scoreAfter, bool gameEnded, PieceType? piece = null)
    {
        if (!_learningEnabled || _currentEpisode.Count == 0) return;

        // Calculate immediate reward for the last action
        float reward = CalculateReward(scoreBefore, scoreAfter, gameEnded, piece);
        
        // Rewards can arrive in phases:
        // 1. immediate score change after the move
        // 2. terminal penalty if that move leaves the board stuck on the next loop
        var lastStep = _currentEpisode[_currentEpisode.Count - 1];
        _currentEpisode[_currentEpisode.Count - 1] = (lastStep.state, lastStep.action, lastStep.reward + reward);
        
        // DON'T learn yet - wait until episode ends to calculate discounted returns
    }

    private float CalculateReward(int scoreBefore, int scoreAfter, bool gameEnded, PieceType? piece)
    {
        int scoreGain = scoreAfter - scoreBefore;
        int placementPoints = piece.HasValue ? piece.Value.GetShape().Count : 0;
        int lineClearBonus = Math.Max(0, scoreGain - placementPoints);

        // Reward actual clears strongly, keep a small positive signal for making progress,
        // and penalize moves that immediately end the run.
        float reward = (placementPoints * 0.15f) + (lineClearBonus * 1.25f);

        if (gameEnded)
        {
            reward -= TerminalPenalty;
        }

        return reward;
    }

    /// <summary>
    /// REINFORCE: At episode end, calculate discounted returns and train on entire trajectory.
    /// This propagates rewards from line clears back to all moves that contributed.
    /// G_t = R_t + γ*R_{t+1} + γ²*R_{t+2} + ...
    /// </summary>
    public void FlushExperienceBuffer()
    {
        if (!_learningEnabled || _currentEpisode.Count == 0) return;
        
        // Calculate total episode score for experience replay decisions
        float episodeScore = _currentEpisode.Sum(e => e.reward);
        
        // EXPERIENCE REPLAY: Save episodes that got line clear bonuses
        if (episodeScore > 0 || episodeScore > _bestEpisodeScore * 0.9f)
        {
            // Copy the episode for storage
            var episodeCopy = _currentEpisode.Select(e => (e.state.ToArray(), e.action, e.reward)).ToList();
            _successfulEpisodes.Add(episodeCopy);
            
            // Keep only the best episodes
            if (_successfulEpisodes.Count > MAX_SUCCESSFUL_EPISODES)
            {
                // Remove the lowest-scoring episode
                var minScore = _successfulEpisodes.Min(ep => ep.Sum(e => e.reward));
                var toRemove = _successfulEpisodes.First(ep => ep.Sum(e => e.reward) == minScore);
                _successfulEpisodes.Remove(toRemove);
            }
            
            if (episodeScore > _bestEpisodeScore)
            {
                _bestEpisodeScore = episodeScore;
            }
        }
        
        // Learn from current episode
        LearnFromEpisode(_currentEpisode, 1);
        
        // REPLAY: Also learn from successful past episodes
        foreach (var successfulEpisode in _successfulEpisodes)
        {
            LearnFromEpisode(successfulEpisode, REPLAY_COUNT);
        }
        
        _currentEpisode.Clear();
    }
    
    private void LearnFromEpisode(List<(float[] state, int action, float reward)> episode, int repeatCount)
    {
        if (episode.Count == 0) return;

        // Calculate discounted returns so setup moves that enable later clears
        // also receive credit.
        var returns = new float[episode.Count];
        float runningReturn = 0f;
        for (int t = episode.Count - 1; t >= 0; t--)
        {
            runningReturn = episode[t].reward + (_gamma * runningReturn);
            returns[t] = runningReturn;
        }

        var averageReturn = returns.Average();
        var promisingMoves = Enumerable.Range(0, episode.Count)
            .Where(t => returns[t] >= averageReturn || episode[t].reward > 0.5f)
            .OrderByDescending(t => returns[t])
            .ToList();

        if (promisingMoves.Count == 0)
        {
            promisingMoves.Add(Array.IndexOf(returns, returns.Max()));
        }

        for (int repeat = 0; repeat < repeatCount; repeat++)
        {
            foreach (var moveIdx in promisingMoves)
            {
                TrainOnMove(episode, moveIdx);
            }
        }
    }
    
    private void TrainOnMove(List<(float[] state, int action, float reward)> episode, int moveIdx)
    {
        var (state, action, reward) = episode[moveIdx];
        
        // Get current network output for this state
        var output = _neuralNetwork.Evaluate(state);
        
        // ONE-HOT target: This specific action led to success
        var targetProbabilities = new float[ActionSpaceSize];
        targetProbabilities[action] = 1.0f;
        
        // Learn toward target
        _neuralNetwork.Learn(output, targetProbabilities);
    }
    
    private float[] Softmax(float[] logits)
    {
        var max = logits.Max();
        var exps = logits.Select(x => Math.Exp(x - max)).ToArray();
        var sum = exps.Sum();
        return exps.Select(x => (float)(x / sum)).ToArray();
    }
    
    private int SampleFromDistribution(float[] probabilities)
    {
        var rand = (float)_random.NextDouble();
        var cumulative = 0f;
        
        for (int i = 0; i < probabilities.Length; i++)
        {
            cumulative += probabilities[i];
            if (rand < cumulative)
            {
                return i;
            }
        }
        
        return probabilities.Length - 1; // Fallback
    }



    private float[] CreateNetworkInput(Blocks blocks, List<PieceType> pieces)
    {
        // MINIMAL encoding: TotalInputSize inputs total
        // - Board state: BoardInputSize values (0 or 1)
        // - Piece shapes: NumPieces pieces × PieceEncodingSize grid = PiecesInputSize values
        var input = new float[TotalInputSize];
        var index = 0;
        var grid = blocks.GetGrid();

        // ============================================================================
        // BOARD STATE: BoardInputSize binary values (occupied = 1, empty = 0)
        // ============================================================================
        for (var r = 0; r < Blocks.GridSize; r++)
        {
            for (var c = 0; c < Blocks.GridSize; c++)
            {
                input[index++] = grid[r][c] != 0 ? 1.0f : 0.0f;
            }
        }

        // ============================================================================
        // PIECE SHAPES: NumPieces pieces × MaxPieceSize×MaxPieceSize grid = PiecesInputSize values (spatial footprint)
        // Using 5×5 to capture all pieces (max dimension is 5)
        // ============================================================================
        for (int pieceIdx = 0; pieceIdx < NumPieces; pieceIdx++)
        {
            if (pieceIdx < pieces.Count)
            {
                var shape = pieces[pieceIdx].GetShape();
                if (shape != null && shape.Count > 0)
                {
                    // Create MaxPieceSize×MaxPieceSize grid for this piece
                    var pieceGrid = new float[MaxPieceSize, MaxPieceSize];
                    
                    // Mark cells that are part of the piece
                    foreach (var (dr, dc) in shape)
                    {
                        if (dr >= 0 && dr < MaxPieceSize && dc >= 0 && dc < MaxPieceSize)
                        {
                            pieceGrid[dr, dc] = 1.0f;
                        }
                    }
                    
                    // Flatten to input array
                    for (int r = 0; r < MaxPieceSize; r++)
                    {
                        for (int c = 0; c < MaxPieceSize; c++)
                        {
                            input[index++] = pieceGrid[r, c];
                        }
                    }
                }
                else
                {
                    // No valid shape - fill with zeros
                    for (int i = 0; i < PieceEncodingSize; i++)
                    {
                        input[index++] = 0.0f;
                    }
                }
            }
            else
            {
                // No piece available - fill with zeros
                for (int i = 0; i < PieceEncodingSize; i++)
                {
                    input[index++] = 0.0f;
                }
            }
        }

        return input;
    }

    public Learning.NeuralNetwork GetNeuralNetwork()
    {
        return _neuralNetwork;
    }
    
    /// <summary>
    /// Diagnostic: Get statistics about network outputs for an empty board state
    /// to see if the network is learning (outputs should become more differentiated)
    /// </summary>
    public (float max, float min, float spread) GetOutputStats()
    {
        // Create a test state: empty board with typical pieces
        var testInput = new float[TotalInputSize];
        // Leave board empty (all zeros) 
        // Add a simple piece pattern in piece slot 0
        testInput[BoardInputSize] = 1.0f; // First cell of piece 0
        
        var output = _neuralNetwork.Evaluate(testInput);
        var probs = output.Probabilities;
        
        float max = probs.Max();
        float min = probs.Min();
        float spread = max - min;
        
        return (max, min, spread);
    }
    
    /// <summary>
    /// Save the neural network to a file for checkpointing during long training runs
    /// </summary>
    public void SaveNetwork(string filePath)
    {
        _neuralNetwork.Save(filePath);
    }

    private float EvaluateCandidateScore(Blocks blocks, List<PieceType> pieces, int pieceIndex, Move move, float policyPrior)
    {
        var nextBoard = blocks.Clone();
        var scoreBefore = nextBoard.Score;

        if (!nextBoard.PlacePiece(move.Piece, move.Row, move.Col))
        {
            return float.NegativeInfinity;
        }

        var remainingPieces = new List<PieceType>(pieces);
        remainingPieces.RemoveAt(pieceIndex);

        var immediateGain = nextBoard.Score - scoreBefore;
        var placementSize = move.Piece.GetShape().Count;
        var lineClearBonus = Math.Max(0, immediateGain - placementSize);

        if (remainingPieces.Count > 0 && nextBoard.IsGameOver(remainingPieces))
        {
            return (immediateGain * 1.5f) + (lineClearBonus * 4.0f) - DeadEndPenalty;
        }

        var boardQuality = EvaluateBoardState(nextBoard, remainingPieces);
        var projectedSequenceScore = remainingPieces.Count > 0
            ? SearchBestSequence(nextBoard, remainingPieces, SearchDepth - 1)
            : 0f;
        var normalizedPolicyPrior = MathF.Tanh(policyPrior);

        return (immediateGain * 1.75f)
            + (lineClearBonus * 5.0f)
            + (boardQuality * 1.35f)
            + (projectedSequenceScore * 0.90f)
            + (normalizedPolicyPrior * 0.10f);
    }

    private float SearchBestSequence(Blocks blocks, List<PieceType> pieces, int depth)
    {
        if (pieces.Count == 0)
        {
            return EvaluateBoardState(blocks, pieces);
        }

        var candidates = new List<(Blocks board, List<PieceType> remainingPieces, float score)>();

        for (int pieceIdx = 0; pieceIdx < pieces.Count; pieceIdx++)
        {
            var piece = pieces[pieceIdx];
            for (int row = 0; row < Blocks.GridSize; row++)
            {
                for (int col = 0; col < Blocks.GridSize; col++)
                {
                    if (!blocks.CanPlacePiece(piece, row, col))
                    {
                        continue;
                    }

                    var nextBoard = blocks.Clone();
                    var scoreBefore = nextBoard.Score;
                    nextBoard.PlacePiece(piece, row, col);

                    var remainingPieces = new List<PieceType>(pieces);
                    remainingPieces.RemoveAt(pieceIdx);

                    var immediateGain = nextBoard.Score - scoreBefore;
                    var score = (immediateGain * 3.0f) + EvaluateBoardState(nextBoard, remainingPieces);
                    candidates.Add((nextBoard, remainingPieces, score));
                }
            }
        }

        if (candidates.Count == 0)
        {
            return -DeadEndPenalty - (pieces.Count * 8f);
        }

        float bestScore = float.NegativeInfinity;
        foreach (var candidate in candidates.OrderByDescending(c => c.score).Take(SearchBeamWidth))
        {
            var totalScore = candidate.score;
            if (depth > 0 && candidate.remainingPieces.Count > 0)
            {
                totalScore += 0.55f * SearchBestSequence(candidate.board, candidate.remainingPieces, depth - 1);
            }

            bestScore = Math.Max(bestScore, totalScore);
        }

        return bestScore;
    }

    private float EvaluateBoardState(Blocks blocks, IReadOnlyList<PieceType> remainingPieces)
    {
        var grid = blocks.GetGrid();
        var emptyCells = 0;
        var isolatedEmptyCells = 0;
        float linePotential = 0f;

        for (int row = 0; row < Blocks.GridSize; row++)
        {
            int filled = 0;
            for (int col = 0; col < Blocks.GridSize; col++)
            {
                if (grid[row][col] != 0)
                {
                    filled++;
                }
            }

            linePotential += GetLinePotential(filled);
        }

        for (int col = 0; col < Blocks.GridSize; col++)
        {
            int filled = 0;
            for (int row = 0; row < Blocks.GridSize; row++)
            {
                if (grid[row][col] != 0)
                {
                    filled++;
                }
            }

            linePotential += GetLinePotential(filled);
        }

        for (int row = 0; row < Blocks.GridSize; row++)
        {
            for (int col = 0; col < Blocks.GridSize; col++)
            {
                if (grid[row][col] == 0)
                {
                    emptyCells++;
                    if (GetOpenNeighborCount(grid, row, col) <= 1)
                    {
                        isolatedEmptyCells++;
                    }
                }
            }
        }

        var (regionCount, largestRegion, smallPocketCells) = GetEmptyRegionMetrics(grid);
        var openSquares2x2 = CountOpenSquares(grid, 2);
        var openSquares3x3 = CountOpenSquares(grid, 3);
        var futureMobility = EstimateFutureMobility(blocks);

        float mobilityScore = 0f;
        bool canPlaceAllRemaining = true;
        foreach (var piece in remainingPieces)
        {
            var placements = CountPlacements(blocks, piece);
            mobilityScore += Math.Min(placements, 12);
            if (placements == 0)
            {
                canPlaceAllRemaining = false;
            }
        }

        return (emptyCells * 0.08f)
            + (largestRegion * 0.80f)
            + (openSquares2x2 * 0.35f)
            + (openSquares3x3 * 0.90f)
            + (futureMobility * 0.55f)
            + (mobilityScore * 0.45f)
            + (linePotential * 1.20f)
            - (isolatedEmptyCells * 1.20f)
            - (smallPocketCells * 1.40f)
            - ((regionCount - 1) * 4.5f)
            + (canPlaceAllRemaining ? 8f : -18f);
    }

    private int CountPlacements(Blocks blocks, PieceType piece)
    {
        int placements = 0;
        for (int row = 0; row < Blocks.GridSize; row++)
        {
            for (int col = 0; col < Blocks.GridSize; col++)
            {
                if (blocks.CanPlacePiece(piece, row, col))
                {
                    placements++;
                }
            }
        }

        return placements;
    }

    private float EstimateFutureMobility(Blocks blocks)
    {
        float weightedPlacements = 0f;
        float fitVariety = 0f;

        foreach (var (piece, weight) in FutureMobilityPieces)
        {
            var placements = CountPlacements(blocks, piece);
            weightedPlacements += Math.Min(placements, 8) * weight;

            if (placements > 0)
            {
                fitVariety += weight;
            }
            else
            {
                weightedPlacements -= weight * 2.0f;
            }
        }

        return weightedPlacements + (fitVariety * 2.5f);
    }

    private static float GetLinePotential(int filled)
    {
        var gaps = Blocks.GridSize - filled;
        return gaps switch
        {
            0 => 20f,
            1 => 12f,
            2 => 6f,
            3 => 2f,
            _ => filled * 0.20f
        };
    }

    private (int regionCount, int largestRegion, int smallPocketCells) GetEmptyRegionMetrics(int[][] grid)
    {
        var visited = new bool[Blocks.GridSize, Blocks.GridSize];
        var regionCount = 0;
        var largestRegion = 0;
        var smallPocketCells = 0;
        var directions = new (int dr, int dc)[] { (-1, 0), (1, 0), (0, -1), (0, 1) };

        for (int row = 0; row < Blocks.GridSize; row++)
        {
            for (int col = 0; col < Blocks.GridSize; col++)
            {
                if (grid[row][col] != 0 || visited[row, col])
                {
                    continue;
                }

                regionCount++;
                var regionSize = 0;
                var queue = new Queue<(int row, int col)>();
                queue.Enqueue((row, col));
                visited[row, col] = true;

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    regionSize++;

                    foreach (var (dr, dc) in directions)
                    {
                        var nextRow = current.row + dr;
                        var nextCol = current.col + dc;

                        if (nextRow < 0 ||
                            nextRow >= Blocks.GridSize ||
                            nextCol < 0 ||
                            nextCol >= Blocks.GridSize ||
                            visited[nextRow, nextCol] ||
                            grid[nextRow][nextCol] != 0)
                        {
                            continue;
                        }

                        visited[nextRow, nextCol] = true;
                        queue.Enqueue((nextRow, nextCol));
                    }
                }

                largestRegion = Math.Max(largestRegion, regionSize);
                if (regionSize <= 3)
                {
                    smallPocketCells += regionSize;
                }
            }
        }

        return (regionCount, largestRegion, smallPocketCells);
    }

    private int CountOpenSquares(int[][] grid, int size)
    {
        int openSquares = 0;
        for (int row = 0; row <= Blocks.GridSize - size; row++)
        {
            for (int col = 0; col <= Blocks.GridSize - size; col++)
            {
                bool isOpen = true;
                for (int dr = 0; dr < size && isOpen; dr++)
                {
                    for (int dc = 0; dc < size; dc++)
                    {
                        if (grid[row + dr][col + dc] != 0)
                        {
                            isOpen = false;
                            break;
                        }
                    }
                }

                if (isOpen)
                {
                    openSquares++;
                }
            }
        }

        return openSquares;
    }

    private int GetOpenNeighborCount(int[][] grid, int row, int col)
    {
        int openNeighbors = 0;
        var directions = new (int dr, int dc)[] { (-1, 0), (1, 0), (0, -1), (0, 1) };

        foreach (var (dr, dc) in directions)
        {
            int nextRow = row + dr;
            int nextCol = col + dc;
            if (nextRow >= 0 &&
                nextRow < grid.Length &&
                nextCol >= 0 &&
                nextCol < grid[nextRow].Length &&
                grid[nextRow][nextCol] == 0)
            {
                openNeighbors++;
            }
        }

        return openNeighbors;
    }

    // ============================================================================
    // HELPER METHODS - Piece Features
    // ============================================================================

    private int GetPieceWidth(PieceType piece)
    {
        var shape = piece.GetShape();
        if (shape == null || shape.Count == 0) return 1;
        int minCol = shape.Min(p => p.Item2);
        int maxCol = shape.Max(p => p.Item2);
        return maxCol - minCol + 1;
    }

    private int GetPieceHeight(PieceType piece)
    {
        var shape = piece.GetShape();
        if (shape == null || shape.Count == 0) return 1;
        int minRow = shape.Min(p => p.Item1);
        int maxRow = shape.Max(p => p.Item1);
        return maxRow - minRow + 1;
    }

    private int GetPieceCellCount(PieceType piece)
    {
        var shape = piece.GetShape();
        return shape?.Count ?? 0;
    }

    private float GetPieceCompactness(PieceType piece)
    {
        // Compactness = cellCount / (width * height)
        int width = GetPieceWidth(piece);
        int height = GetPieceHeight(piece);
        int cellCount = GetPieceCellCount(piece);
        return (width * height) > 0 ? cellCount / (float)(width * height) : 1.0f;
    }

    private int CountValidPlacements(Blocks blocks, PieceType piece)
    {
        int count = 0;
        for (int r = 0; r < Blocks.GridSize; r++)
        {
            for (int c = 0; c < Blocks.GridSize; c++)
            {
                if (blocks.CanPlacePiece(piece, r, c)) count++;
            }
        }
        return count;
    }

    private float GetPieceMinValidRow(Blocks blocks, PieceType piece)
    {
        for (int r = 0; r < Blocks.GridSize; r++)
        {
            for (int c = 0; c < Blocks.GridSize; c++)
            {
                if (blocks.CanPlacePiece(piece, r, c)) return r;
            }
        }
        return 0;
    }

    private float GetPieceMaxValidRow(Blocks blocks, PieceType piece)
    {
        for (int r = Blocks.GridSize - 1; r >= 0; r--)
        {
            for (int c = 0; c < Blocks.GridSize; c++)
            {
                if (blocks.CanPlacePiece(piece, r, c)) return r;
            }
        }
        return 0;
    }

    private float GetPieceMinValidCol(Blocks blocks, PieceType piece)
    {
        for (int c = 0; c < Blocks.GridSize; c++)
        {
            for (int r = 0; r < Blocks.GridSize; r++)
            {
                if (blocks.CanPlacePiece(piece, r, c)) return c;
            }
        }
        return 0;
    }

    private float GetPieceMaxValidCol(Blocks blocks, PieceType piece)
    {
        for (int c = Blocks.GridSize - 1; c >= 0; c--)
        {
            for (int r = 0; r < Blocks.GridSize; r++)
            {
                if (blocks.CanPlacePiece(piece, r, c)) return c;
            }
        }
        return 0;
    }

    // ============================================================================
    // HELPER METHODS - Global Features
    // ============================================================================

    private int CountCompleteRows(int[][] grid)
    {
        int count = 0;
        for (int r = 0; r < Blocks.GridSize; r++)
        {
            bool complete = true;
            for (int c = 0; c < Blocks.GridSize; c++)
            {
                if (grid[r][c] == 0)
                {
                    complete = false;
                    break;
                }
            }
            if (complete) count++;
        }
        return count;
    }

    private int CountCompleteCols(int[][] grid)
    {
        int count = 0;
        for (int c = 0; c < Blocks.GridSize; c++)
        {
            bool complete = true;
            for (int r = 0; r < Blocks.GridSize; r++)
            {
                if (grid[r][c] == 0)
                {
                    complete = false;
                    break;
                }
            }
            if (complete) count++;
        }
        return count;
    }

    private int CountNearCompleteRows(int[][] grid, int missing)
    {
        int count = 0;
        for (int r = 0; r < Blocks.GridSize; r++)
        {
            int empty = 0;
            for (int c = 0; c < Blocks.GridSize; c++)
            {
                if (grid[r][c] == 0) empty++;
            }
            if (empty == missing) count++;
        }
        return count;
    }

    private int CountNearCompleteCols(int[][] grid, int missing)
    {
        int count = 0;
        for (int c = 0; c < Blocks.GridSize; c++)
        {
            int empty = 0;
            for (int r = 0; r < Blocks.GridSize; r++)
            {
                if (grid[r][c] == 0) empty++;
            }
            if (empty == missing) count++;
        }
        return count;
    }

    private float CalculateScoreGain(Blocks blocks, PieceType piece, int row, int col)
    {
        if (!blocks.CanPlacePiece(piece, row, col)) return -1000; // Penalty for invalid move
        
        // Create a temporary copy and simulate the move to get actual score gain
        var tempBlocks = new Blocks();
        // Copy the current state (simplified)
        var currentScore = blocks.Score;
        
        // For now, estimate based on piece size and potential clears
        int pieceCells = GetPieceCellCount(piece);
        var grid = blocks.GetGrid();
        
        // Check if this move would complete rows/cols
        int potentialClears = 0;
        
        // Rough estimation: check if placement is in almost-complete row/col
        int rowOccupied = 0, colOccupied = 0;
        for (int c = 0; c < Blocks.GridSize; c++)
            if (grid[row][c] != 0) rowOccupied++;
        for (int r = 0; r < Blocks.GridSize; r++)
            if (grid[r][col] != 0) colOccupied++;
            
        if (rowOccupied >= 7) potentialClears += 8; // Row clear bonus
        if (colOccupied >= 7) potentialClears += 8; // Col clear bonus
        
        return pieceCells + potentialClears * 10; // Base score + clear bonus
    }

    private int CountClearedAfterMove(Blocks blocks, PieceType piece, int row, int col)
    {
        // Estimate how many cells would be cleared by this move
        var grid = blocks.GetGrid();
        int cleared = 0;
        
        // Check if row would be completed
        int rowEmpty = 0;
        for (int c = 0; c < Blocks.GridSize; c++)
            if (grid[row][c] == 0) rowEmpty++;
        if (rowEmpty <= GetPieceCellCount(piece)) cleared += 8; // Row would be cleared
        
        // Check if col would be completed
        int colEmpty = 0;
        for (int r = 0; r < Blocks.GridSize; r++)
            if (grid[r][col] == 0) colEmpty++;
        if (colEmpty <= GetPieceCellCount(piece)) cleared += 8; // Col would be cleared
        
        return cleared;
    }

    private int CountDeadCells(int[][] grid)
    {
        // Count cells that are isolated and can't be easily filled
        int deadCells = 0;
        for (int r = 0; r < Blocks.GridSize; r++)
        {
            for (int c = 0; c < Blocks.GridSize; c++)
            {
                if (grid[r][c] == 0 && IsIsolatedCell(grid, r, c))
                {
                    deadCells++;
                }
            }
        }
        return deadCells;
    }

    private bool IsIsolatedCell(int[][] grid, int row, int col)
    {
        // A cell is isolated if it's surrounded by occupied cells or edges
        int occupiedNeighbors = 0;
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int r = row + dr, c = col + dc;
                if (r < 0 || r >= Blocks.GridSize || c < 0 || c >= Blocks.GridSize || grid[r][c] != 0)
                {
                    occupiedNeighbors++;
                }
            }
        }
        return occupiedNeighbors >= 6; // Mostly surrounded
    }

    private float CanPlaceOtherPieces(Blocks blocks, List<PieceType> pieces, PieceType selectedPiece, int row, int col)
    {
        // After placing selected piece, how many other pieces can still be placed?
        float canPlace = 0;
        foreach (var piece in pieces)
        {
            if (piece == selectedPiece) continue;
            
            // Quick check: can this piece be placed anywhere?
            bool foundSpot = false;
            for (int r = 0; r < Blocks.GridSize && !foundSpot; r++)
            {
                for (int c = 0; c < Blocks.GridSize && !foundSpot; c++)
                {
                    if (blocks.CanPlacePiece(piece, r, c))
                    {
                        foundSpot = true;
                    }
                }
            }
            if (foundSpot) canPlace++;
        }
        return canPlace;
    }

    private float GetPlacementQuality(int[][] grid, PieceType piece, int row, int col)
    {
        // Rate the "quality" of this placement (0-1)
        float quality = 0.5f; // Neutral baseline
        
        // Bonus for filling gaps
        int adjacentOccupied = 0;
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int r = row + dr, c = col + dc;
                if (r >= 0 && r < Blocks.GridSize && c >= 0 && c < Blocks.GridSize && grid[r][c] != 0)
                {
                    adjacentOccupied++;
                }
            }
        }
        quality += adjacentOccupied / 8.0f * 0.3f; // Bonus for connecting to existing pieces
        
        // Penalty for creating isolated holes
        // ... (simplified for now)
        
        return Math.Min(quality, 1.0f);
    }

    private int CountAlmostCompleteRows(int[][] grid)
    {
        int count = 0;
        for (int r = 0; r < Blocks.GridSize; r++)
        {
            int occupied = 0;
            for (int c = 0; c < Blocks.GridSize; c++)
            {
                if (grid[r][c] != 0) occupied++;
            }
            if (occupied >= 6) count++; // Almost complete if 6+ cells
        }
        return count;
    }

    private int CountAlmostCompleteCols(int[][] grid)
    {
        int count = 0;
        for (int c = 0; c < Blocks.GridSize; c++)
        {
            int occupied = 0;
            for (int r = 0; r < Blocks.GridSize; r++)
            {
                if (grid[r][c] != 0) occupied++;
            }
            if (occupied >= 6) count++;
        }
        return count;
    }

    private float CalculateBoardDensity(int[][] grid)
    {
        int occupied = 0;
        for (int r = 0; r < Blocks.GridSize; r++)
        {
            for (int c = 0; c < Blocks.GridSize; c++)
            {
                if (grid[r][c] != 0) occupied++;
            }
        }
        return occupied / (float)BoardInputSize;
    }

    private int CountIsolatedHoles(int[][] grid)
    {
        int holes = 0;
        for (int r = 1; r < Blocks.GridSize - 1; r++)
        {
            for (int c = 1; c < Blocks.GridSize - 1; c++)
            {
                if (grid[r][c] == 0)
                {
                    // Check if surrounded
                    if (grid[r-1][c] != 0 && grid[r+1][c] != 0 && 
                        grid[r][c-1] != 0 && grid[r][c+1] != 0)
                    {
                        holes++;
                    }
                }
            }
        }
        return holes;
    }

    private float CalculateFragmentation(int[][] grid)
    {
        // Measure how scattered the pieces are
        int transitions = 0;
        for (int r = 0; r < Blocks.GridSize; r++)
        {
            for (int c = 0; c < Blocks.GridSize - 1; c++)
            {
                if (grid[r][c] != grid[r][c + 1]) transitions++;
            }
        }
        for (int c = 0; c < Blocks.GridSize; c++)
        {
            for (int r = 0; r < Blocks.GridSize - 1; r++)
            {
                if (grid[r][c] != grid[r + 1][c]) transitions++;
            }
        }
        return transitions / 112.0f; // Max possible transitions
    }

    private int CountLargestEmptyRegion(int[][] grid)
    {
        // Simple approximation - count largest contiguous empty area
        var visited = new bool[Blocks.GridSize, Blocks.GridSize];
        int maxRegion = 0;
        
        for (int r = 0; r < Blocks.GridSize; r++)
        {
            for (int c = 0; c < Blocks.GridSize; c++)
            {
                if (grid[r][c] == 0 && !visited[r, c])
                {
                    int regionSize = FloodFillEmpty(grid, visited, r, c);
                    maxRegion = Math.Max(maxRegion, regionSize);
                }
            }
        }
        return maxRegion;
    }

    private int FloodFillEmpty(int[][] grid, bool[,] visited, int r, int c)
    {
        if (r < 0 || r >= Blocks.GridSize || c < 0 || c >= Blocks.GridSize ||
            visited[r, c] || grid[r][c] != 0)
            return 0;
        
        visited[r, c] = true;
        return 1 + FloodFillEmpty(grid, visited, r - 1, c) +
                   FloodFillEmpty(grid, visited, r + 1, c) +
                   FloodFillEmpty(grid, visited, r, c - 1) +
                   FloodFillEmpty(grid, visited, r, c + 1);
    }

    private int CountEdgeOccupied(int[][] grid)
    {
        int count = 0;
        // Top and bottom rows
        for (int c = 0; c < Blocks.GridSize; c++)
        {
            if (grid[0][c] != 0) count++;
            if (grid[Blocks.GridSize - 1][c] != 0) count++;
        }
        // Left and right columns (excluding corners already counted)
        for (int r = 1; r < Blocks.GridSize - 1; r++)
        {
            if (grid[r][0] != 0) count++;
            if (grid[r][Blocks.GridSize - 1] != 0) count++;
        }
        return count;
    }

    // ============================================================================
    // NEW HELPER METHODS FOR CHANNEL-BASED ENCODING
    // ============================================================================

    private float[][] CreatePlacementMask(PieceType piece, int row, int col)
    {
        // Create a grid showing where the piece would be placed
        var mask = new float[Blocks.GridSize][];
        for (int i = 0; i < Blocks.GridSize; i++)
        {
            mask[i] = new float[Blocks.GridSize];
        }

        // Get piece shape and mark cells
        var shape = piece.GetShape();
        if (shape != null && shape.Count > 0)
        {
            foreach (var (dr, dc) in shape)
            {
                int r = row + dr;
                int c = col + dc;
                if (r >= 0 && r < Blocks.GridSize && c >= 0 && c < Blocks.GridSize)
                {
                    mask[r][c] = 1.0f;
                }
            }
        }
        return mask;
    }

    private float CalculateCornerOccupancy(int[][] grid)
    {
        int count = 0;
        if (grid[0][0] != 0) count++;
        if (grid[0][Blocks.GridSize - 1] != 0) count++;
        if (grid[Blocks.GridSize - 1][0] != 0) count++;
        if (grid[Blocks.GridSize - 1][Blocks.GridSize - 1] != 0) count++;
        return count / 4.0f;
    }

    private float CalculateCenterOccupancy(int[][] grid)
    {
        int centerStart = Blocks.GridSize / 2 - 1;
        int centerEnd = Blocks.GridSize / 2 + 1;
        int count = 0;
        int total = 0;
        
        for (int r = centerStart; r < centerEnd; r++)
        {
            for (int c = centerStart; c < centerEnd; c++)
            {
                total++;
                if (grid[r][c] != 0) count++;
            }
        }
        
        return total > 0 ? count / (float)total : 0;
    }

    private float CalculateBoardFragmentation(int[][] grid)
    {
        int transitions = 0;
        int totalTransitions = 0;
        
        for (int r = 0; r < Blocks.GridSize; r++)
        {
            for (int c = 0; c < Blocks.GridSize - 1; c++)
            {
                totalTransitions++;
                if (grid[r][c] != grid[r][c + 1]) transitions++;
            }
        }
        
        for (int c = 0; c < Blocks.GridSize; c++)
        {
            for (int r = 0; r < Blocks.GridSize - 1; r++)
            {
                totalTransitions++;
                if (grid[r][c] != grid[r + 1][c]) transitions++;
            }
        }
        
        return transitions / (float)totalTransitions;
    }

    private float CountEdgeCells(int[][] grid)
    {
        int occupied = 0;
        
        for (int c = 0; c < Blocks.GridSize; c++)
        {
            if (grid[0][c] != 0) occupied++;
            if (grid[Blocks.GridSize - 1][c] != 0) occupied++;
        }
        
        for (int r = 1; r < Blocks.GridSize - 1; r++)
        {
            if (grid[r][0] != 0) occupied++;
            if (grid[r][Blocks.GridSize - 1] != 0) occupied++;
        }
        
        return occupied;
    }

    private float CalculateRowVariance(int[][] grid)
    {
        var rowCounts = new int[Blocks.GridSize];
        for (int r = 0; r < Blocks.GridSize; r++)
        {
            for (int c = 0; c < Blocks.GridSize; c++)
            {
                if (grid[r][c] != 0) rowCounts[r]++;
            }
        }
        
        var mean = rowCounts.Average();
        var variance = rowCounts.Select(x => (x - mean) * (x - mean)).Average();
        return (float)Math.Sqrt(variance) / Blocks.GridSize;
    }

    private float CalculateColVariance(int[][] grid)
    {
        var colCounts = new int[Blocks.GridSize];
        for (int c = 0; c < Blocks.GridSize; c++)
        {
            for (int r = 0; r < Blocks.GridSize; r++)
            {
                if (grid[r][c] != 0) colCounts[c]++;
            }
        }
        
        var mean = colCounts.Average();
        var variance = colCounts.Select(x => (x - mean) * (x - mean)).Average();
        return (float)Math.Sqrt(variance) / Blocks.GridSize;
    }

    private float CalculateAverageHoleSize(int[][] grid)
    {
        var visited = new bool[Blocks.GridSize, Blocks.GridSize];
        var holeSizes = new List<int>();
        
        for (int r = 0; r < Blocks.GridSize; r++)
        {
            for (int c = 0; c < Blocks.GridSize; c++)
            {
                if (grid[r][c] == 0 && !visited[r, c])
                {
                    int holeSize = FloodFillEmpty(grid, visited, r, c);
                    holeSizes.Add(holeSize);
                }
            }
        }
        
        return holeSizes.Count > 0 ? (float)holeSizes.Average() : 0;
    }

    private int CountTotalValidMoves(Blocks blocks, List<PieceType> pieces)
    {
        int total = 0;
        foreach (var piece in pieces)
        {
            total += CountValidPlacements(blocks, piece);
        }
        return total;
    }

    private float CalculateBoardSymmetry(int[][] grid)
    {
        int matches = 0;
        int total = 0;
        for (int r = 0; r < Blocks.GridSize; r++)
        {
            for (int c = 0; c < Blocks.GridSize / 2; c++)
            {
                if (grid[r][c] == grid[r][Blocks.GridSize - 1 - c]) matches++;
                total++;
            }
        }
        return total > 0 ? matches / (float)total : 0;
    }
}

/// <summary>
/// Backward-compatible alias for older code. Prefer <see cref="HybridComputerPlayer"/>.
/// </summary>
public class Computer : HybridComputerPlayer
{
    public Computer()
    {
    }

    public Computer(Learning.NeuralNetwork neuralNetwork)
        : base(neuralNetwork)
    {
    }
}
