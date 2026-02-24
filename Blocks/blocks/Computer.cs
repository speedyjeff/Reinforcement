using System;
using System.Collections.Generic;
using System.Linq;
using blocks.engine;
using Learning;

public class Computer : IPlayer
{
    private readonly Learning.NeuralNetwork _neuralNetwork;
    private readonly Random _random;

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

    public Computer()
    {
        _random = new Random();
        
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
        _epsilon = 0.05f; // ε-greedy: 5% random exploration
        _epsilonDecay = 0.9999f; // Decay ε slowly
        _epsilonMin = 0.01f; // Minimal exploration (1%)
        _temperature = 0.5f; // Low temp = trust the model
        _temperatureDecay = 0.9999f; // Decay slowly
        _temperatureMin = 0.2f; // Very low for exploitation
    }

    public Computer(Learning.NeuralNetwork neuralNetwork)
    {
        _neuralNetwork = neuralNetwork ?? throw new ArgumentNullException(nameof(neuralNetwork));
        _random = new Random();
        _gamma = 0.99f;
        _epsilon = 0.05f; // Pre-trained: minimal exploration (5%)
        _epsilonDecay = 0.9999f;
        _epsilonMin = 0.01f;
        _temperature = 0.5f; // Pre-trained networks use less exploration
        _temperatureDecay = 0.9999f;
        _temperatureMin = 0.2f;
    }

    public Move ChooseMove(Blocks blocks, List<PieceType> pieces)
    {
        // STEP 1: Encode the current state
        var state = CreateNetworkInput(blocks, pieces);
        
        // STEP 2: Get policy (action probabilities) from network
        var output = _neuralNetwork.Evaluate(state);
        var actionLogits = output.Probabilities; // Raw network outputs (logits)
        
        // STEP 3: Build list of valid moves with their action indices
        var validMoves = new List<(Move move, int actionIndex)>();
        
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
                        validMoves.Add((new Move(piece, row, col), actionIndex));
                    }
                }
            }
        }

        if (validMoves.Count == 0)
        {
            throw new Exception("No valid moves available for the computer player.");
        }

        int selectedIndex;
        int selectedActionIndex;
        Move selectedMove;

        // ε-GREEDY: With probability ε, choose a random valid action
        if (_random.NextDouble() < _epsilon)
        {
            // Random exploration
            selectedIndex = _random.Next(validMoves.Count);
            selectedMove = validMoves[selectedIndex].move;
            selectedActionIndex = validMoves[selectedIndex].actionIndex;
        }
        else
        {
            // STEP 4: Mask invalid actions and apply softmax
            // Set invalid action logits to very negative so they get ~0 probability
            var maskedLogits = new float[192];
            for (int i = 0; i < 192; i++)
            {
                maskedLogits[i] = -1000f; // Very negative for invalid actions
            }
            
            // Set valid action logits (with temperature)
            foreach (var (move, actionIndex) in validMoves)
            {
                maskedLogits[actionIndex] = actionLogits[actionIndex] / _temperature;
            }
            
            // Softmax over all 192 (but invalid ones will have ~0 probability)
            var allProbabilities = Softmax(maskedLogits);
            
            // Extract just the valid action probabilities for sampling
            var probabilities = validMoves.Select(m => allProbabilities[m.actionIndex]).ToArray();
            
            // STEP 5: Sample action from policy distribution
            selectedIndex = SampleFromDistribution(probabilities);
            selectedMove = validMoves[selectedIndex].move;
            selectedActionIndex = validMoves[selectedIndex].actionIndex;
        }
        
        // STEP 6: Store (state, action) in current episode - reward will be added later
        _currentEpisode.Add((state, selectedActionIndex, 0f)); // Reward added in LearnFromMove
        
        // Decay exploration parameters for less randomness over time
        _epsilon = Math.Max(_epsilonMin, _epsilon * _epsilonDecay);
        _temperature = Math.Max(_temperatureMin, _temperature * _temperatureDecay);
        
        return selectedMove;
    }

    // Call this after a move is made to record the reward (learning happens at episode end)
    public void LearnFromMove(int scoreBefore, int scoreAfter, bool gameEnded)
    {
        if (_currentEpisode.Count == 0) return;

        // Calculate immediate reward for the last action
        float reward = CalculateReward(scoreBefore, scoreAfter, gameEnded);
        
        // Update the reward for the last step in the episode
        var lastStep = _currentEpisode[_currentEpisode.Count - 1];
        _currentEpisode[_currentEpisode.Count - 1] = (lastStep.state, lastStep.action, reward);
        
        // DON'T learn yet - wait until episode ends to calculate discounted returns
    }

    private float CalculateReward(int scoreBefore, int scoreAfter, bool gameEnded)
    {
        // SIMPLE IMMEDIATE REWARDS - focus on what just happened
        int scoreGain = scoreAfter - scoreBefore;
        
        // Return just the score gain - simple and direct
        return scoreGain;
    }

    /// <summary>
    /// REINFORCE: At episode end, calculate discounted returns and train on entire trajectory.
    /// This propagates rewards from line clears back to all moves that contributed.
    /// G_t = R_t + γ*R_{t+1} + γ²*R_{t+2} + ...
    /// </summary>
    public void FlushExperienceBuffer()
    {
        if (_currentEpisode.Count == 0) return;
        
        // Calculate total episode score for experience replay decisions
        float episodeScore = _currentEpisode.Sum(e => e.reward);
        float baseScore = _currentEpisode.Count; // Minimum score = 1 point per piece placed
        float bonusScore = episodeScore - baseScore; // Bonus from line clears
        
        // EXPERIENCE REPLAY: Save episodes that got line clear bonuses
        // Any bonus > 0 means a line was cleared - save these!
        if (bonusScore > 0 || episodeScore > _bestEpisodeScore * 0.9f)
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
        
        // STEP 1: Calculate per-step rewards (not discounted - look at immediate effect)
        // Find which moves actually caused line clears (reward > 1)
        var lineClearMoves = new List<int>();
        for (int t = 0; t < episode.Count; t++)
        {
            if (episode[t].reward > 1) // More than just placement = line clear happened
            {
                lineClearMoves.Add(t);
            }
        }
        
        // STEP 2: Determine if this was a "good" episode (had line clears)
        float totalReward = episode.Sum(e => e.reward);
        float baseReward = episode.Count; // 1 point per piece minimum
        bool wasSuccessful = totalReward > baseReward; // Got bonus points = line cleared
        
        // STEP 3: Only learn from moves that ACTUALLY caused line clears
        // This avoids conflicting signals for moves where any action is equally good
        if (!wasSuccessful)
        {
            // Don't learn from failed games
            return;
        }
        
        // No line clear moves identified? Use last move as fallback
        if (lineClearMoves.Count == 0 && episode.Count > 0)
        {
            lineClearMoves.Add(episode.Count - 1);
        }
        
        for (int repeat = 0; repeat < repeatCount; repeat++)
        {
            // Only train on moves that caused line clears
            // The setup move has no consistent "right answer" - any column works for move 1
            foreach (var clearMoveIdx in lineClearMoves)
            {
                // Train ONLY on the move that cleared the line
                // This state has clear information: "board has pieces, place here to clear"
                TrainOnMove(episode, clearMoveIdx);
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
        return piece switch
        {
            PieceType.Single => 1,
            PieceType.Vertical2 => 2,
            PieceType.Horizontal2 => 2,
            PieceType.Vertical3 => 3,
            PieceType.Horizontal3 => 3,
            PieceType.CornerTopLeft => 3,
            PieceType.CornerTopRight => 3,
            PieceType.CornerBottomLeft => 3,
            PieceType.CornerBottomRight => 3,
            PieceType.TUp => 4,
            PieceType.TDown => 4,
            PieceType.TLeft => 4,
            PieceType.TRight => 4,
            PieceType.Square2x2 => 4,
            PieceType.Vertical4 => 4,
            PieceType.Horizontal4 => 4,
            PieceType.ZLeft => 4,
            PieceType.ZRight => 4,
            PieceType.ZUp => 4,
            PieceType.ZDown => 4,
            PieceType.Vertical5 => 5,
            PieceType.Horizontal5 => 5,
            PieceType.Vertical2x3 => 6,
            PieceType.Horizontal2x3 => 6,
            PieceType.Square3x3 => 9,
            _ => 3
        };
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
