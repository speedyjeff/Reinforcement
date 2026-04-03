using System;
using System.Collections.Generic;
using System.Linq;

namespace blocks.engine
{
    /// <summary>
    /// Predefined test scenarios for verifying model learning
    /// </summary>
    public enum TestScenario
    {
        /// <summary>Random pieces (normal gameplay)</summary>
        Random,
        
        /// <summary>
        /// SIMPLEST TEST: Board has V5 pre-placed in column 0.
        /// Network gets ONE V3 piece and must place it in column 0 to clear.
        /// Only ONE correct column, limited row options = very learnable!
        /// Expected optimal score: 13 (3 cells + 10 bonus)
        /// </summary>
        OneMoveColumnClear,
        
        /// <summary>
        /// Easy: 8 Single pieces - optimal play clears one row/column
        /// Expected optimal score: ~18 (8 cells + 10 bonus)
        /// </summary>
        EasySingles,
        
        /// <summary>
        /// Medium: 2x Vertical2 + 1x Vertical3 - fills exactly one column (7 cells on 8x8 is not a clear)
        /// For 8x8: need 8 cells to clear, so this tests efficient placement
        /// </summary>
        MediumVerticals,
        
        /// <summary>
        /// Column Clear: Vertical5 + Vertical3 = 8 cells, clears one column perfectly
        /// Expected optimal score: ~18 (8 cells + 10 bonus)
        /// </summary>
        ColumnClear,
        
        /// <summary>
        /// Row Clear: Horizontal5 + Horizontal3 = 8 cells, clears one row perfectly  
        /// Expected optimal score: ~18 (8 cells + 10 bonus)
        /// </summary>
        RowClear,
        
        /// <summary>
        /// Double Clear: 2x (Horizontal5 + Horizontal3) = can clear 2 rows
        /// Expected optimal score: ~54 (16 cells + 20 bonus) * 2 for chain
        /// </summary>
        DoubleClear
    }

    public class BlockGenerator
    {
        /// <summary>
        /// Creates a standard random generator for normal gameplay
        /// </summary>
        public BlockGenerator() 
            : this(TestScenario.Random, maxBoardSize: null, staticSequence: null, puzzleMode: false)
        {
        }

        /// <summary>
        /// Creates a generator with a predefined test scenario.
        /// Test scenarios run in puzzle mode by default (game ends when pieces exhausted).
        /// </summary>
        public BlockGenerator(TestScenario scenario, bool puzzleMode = true) 
            : this(scenario, maxBoardSize: null, staticSequence: null, puzzleMode: scenario != TestScenario.Random && puzzleMode)
        {
        }

        /// <summary>
        /// Gets the cells that should be pre-filled for a scenario.
        /// Returns empty enumerable for scenarios with no pre-fill.
        /// </summary>
        public static IEnumerable<(int row, int col)> GetScenarioPreFill(TestScenario scenario)
        {
            switch (scenario)
            {
                case TestScenario.OneMoveColumnClear:
                    // Pre-fill V5 in column 0 (rows 0-4)
                    return new[] { (0, 0), (1, 0), (2, 0), (3, 0), (4, 0) };
                default:
                    return Enumerable.Empty<(int, int)>();
            }
        }

        /// <summary>
        /// Creates a generator that only returns pieces fitting within a maxSize x maxSize board
        /// Useful for testing on smaller boards (e.g., 3x3, 4x4)
        /// </summary>
        public BlockGenerator(int maxBoardSize) 
            : this(TestScenario.Random, maxBoardSize, staticSequence: null, puzzleMode: false)
        {
        }

        /// <summary>
        /// Creates a generator with a custom static piece sequence.
        /// When puzzleMode is true, game ends when queue is exhausted.
        /// When puzzleMode is false, falls back to random selection.
        /// </summary>
        public BlockGenerator(IEnumerable<PieceType> staticSequence, int? maxBoardSize = null, bool puzzleMode = true) 
            : this(TestScenario.Random, maxBoardSize, staticSequence, puzzleMode)
        {
        }

        /// <summary>
        /// Returns the next piece - from static queue if available, otherwise random.
        /// In puzzle mode, throws InvalidOperationException if queue is exhausted.
        /// </summary>
        public PieceType GetNextPiece()
        {
            if (!TryGetNextPiece(out var piece))
            {
                throw new InvalidOperationException("Puzzle mode: no more pieces available. Use TryGetNextPiece to check first.");
            }
            return piece;
        }

        /// <summary>
        /// Attempts to get the next piece. Returns false if in puzzle mode and queue is exhausted.
        /// </summary>
        /// <param name="piece">The next piece, or default if none available</param>
        /// <returns>True if a piece was returned, false if puzzle is complete</returns>
        public bool TryGetNextPiece(out PieceType piece)
        {
            // If we have static pieces queued, return those first
            if (StaticPieces.Count > 0)
            {
                piece = StaticPieces.Dequeue();
                return true;
            }

            // In puzzle mode, no more pieces means puzzle is complete
            if (PuzzleMode)
            {
                piece = default;
                return false;
            }

            // Otherwise return a random piece from allowed set
            var index = Random.Next(AllowedPiecesList.Count);
            piece = AllowedPiecesList[index];
            return true;
        }

        #region private

        private readonly Random Random;
        private readonly Queue<PieceType> StaticPieces;
        private readonly List<PieceType> AllowedPiecesList;
        private readonly int? MaxBoardSize;
        private readonly TestScenario Scenario;
        private readonly bool PuzzleMode;

        /// <summary>
        /// Gets the piece sequence for a predefined test scenario
        /// </summary>
        private static IEnumerable<PieceType> GetScenarioPieces(TestScenario scenario)
        {
            switch (scenario)
            {
                case TestScenario.OneMoveColumnClear:
                    return new[] { PieceType.Vertical3 }; // Just one piece - V5 is pre-placed
                
                case TestScenario.EasySingles:
                    return Enumerable.Repeat(PieceType.Single, 32);
                
                case TestScenario.MediumVerticals:
                    return new[] { PieceType.Vertical2, PieceType.Vertical2, PieceType.Vertical3 };
                
                case TestScenario.ColumnClear:
                    return new[] { PieceType.Vertical5, PieceType.Vertical3 };
                
                case TestScenario.RowClear:
                    return new[] { PieceType.Horizontal5, PieceType.Horizontal3 };
                
                case TestScenario.DoubleClear:
                    return new[] { PieceType.Horizontal5, PieceType.Horizontal3, PieceType.Horizontal5, PieceType.Horizontal3 };
                
                case TestScenario.Random:
                default:
                    return Enumerable.Empty<PieceType>();
            }
        }
        /// Primary constructor - all other constructors chain to this one
        /// </summary>
        private BlockGenerator(TestScenario scenario, int? maxBoardSize, IEnumerable<PieceType>? staticSequence, bool puzzleMode = false)
        {
            Random = new Random();
            Scenario = scenario;
            MaxBoardSize = maxBoardSize;
            PuzzleMode = puzzleMode;
            
            // Determine allowed pieces based on board size
            AllowedPiecesList = maxBoardSize.HasValue
                ? GetPiecesThatFit(maxBoardSize.Value).ToList()
                : GetAllValidPieces().ToList();
            
            if (AllowedPiecesList.Count == 0)
            {
                throw new ArgumentException($"No pieces fit within a {maxBoardSize}x{maxBoardSize} board");
            }
            
            // Initialize static pieces - from explicit sequence or scenario
            var pieces = staticSequence ?? GetScenarioPieces(scenario);
            StaticPieces = new Queue<PieceType>(pieces);
        }

        /// <summary>
        /// Gets all valid piece types
        /// </summary>
        private static IEnumerable<PieceType> GetAllValidPieces()
        {
            for (int i = (int)PieceType.Invalid_First + 1; i < (int)PieceType.Invalid_Last; i++)
            {
                yield return (PieceType)i;
            }
        }

        /// <summary>
        /// Gets pieces that fit within a maxSize x maxSize bounding box
        /// </summary>
        private static IEnumerable<PieceType> GetPiecesThatFit(int maxSize)
        {
            foreach (var piece in GetAllValidPieces())
            {
                var shape = piece.GetShape();
                int maxRow = shape.Max(p => p.Item1) + 1; // +1 because 0-indexed
                int maxCol = shape.Max(p => p.Item2) + 1;
                
                if (maxRow <= maxSize && maxCol <= maxSize)
                {
                    yield return piece;
                }
            }
        }

        #endregion
    }
}
