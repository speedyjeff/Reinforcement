using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace blocks.engine;

public class Blocks
{
    public Blocks()
    {
        Grid = new int[GridSize][];
        for (int i = 0; i < GridSize; i++)
        {
            Grid[i] = new int[GridSize];
        }
        Score = 0;
        PreviousClearCount = 0;
        PiecesPlayed = 0;
    }

    /// <summary>
    /// Pre-fills specific cells on the board (for testing scenarios).
    /// Does not award points or trigger line clears.
    /// </summary>
    public void PreFillCells(IEnumerable<(int row, int col)> cells)
    {
        foreach (var (row, col) in cells)
        {
            if (row >= 0 && row < GridSize && col >= 0 && col < GridSize)
            {
                Grid[row][col] = 1;
            }
        }
    }

    public const int GridSize = 8;
    public const int NumPiecesInSet = 3;
    
    // Get the number of valid piece types (excluding Invalid_First and Invalid_Last)
    public static int GetValidPieceCount() 
    {
        return (int)PieceType.Invalid_Last - (int)PieceType.Invalid_First - 1;
    }

    public int Score { get; private set; }
    public int PiecesPlayed { get; private set; }
    
    // try to place a piece at (row, col). Returns true if successful, false if not found or cannot place.
    public bool PlacePiece(PieceType type, int row, int col)
    {
        if (row < 0 || row >= GridSize || col < 0 || col >= GridSize) return false;
        if (!CanPlacePiece(type, row, col)) return false;

        // get the shape for this piece
        var shape = type.GetShape();
        if (shape == null || shape.Count == 0) return false;

        // scoring
        //  1 point for each cell marked
        //  1 point for each cell cleared
        //  bonus 10 points for clearing a column/row
        //  additional bonus multiplier for chains of clears (turn to turn, clearned cells + bonus, 2x, 3x, 4x, etc.)

        // place this piece and update score for marking
        int marked = 0;
        foreach (var (dr, dc) in shape)
        {
            if (Grid[row + dr][col + dc] == 0)
            {
                Grid[row + dr][col + dc] = 1;
                marked++;
            }
        }
        Score += marked;

        // Atomic row/column clearing
        var rowsToClear = new bool[GridSize];
        var colsToClear = new bool[GridSize];

        // Find complete rows
        for (int r = 0; r < GridSize; r++)
        {
            bool complete = true;
            for (int c = 0; c < GridSize; c++)
            {
                if (Grid[r][c] == 0)
                {
                    complete = false;
                    break;
                }
            }
            if (complete) rowsToClear[r] = true;
        }

        // Find complete columns
        for (int c = 0; c < GridSize; c++)
        {
            bool complete = true;
            for (int r = 0; r < GridSize; r++)
            {
                if (Grid[r][c] == 0)
                {
                    complete = false;
                    break;
                }
            }
            if (complete) colsToClear[c] = true;
        }

        // Clear all marked rows and columns atomically, update score for clearing
        int cleared = 0;
        for (int r = 0; r < GridSize; r++)
        {
            for (int c = 0; c < GridSize; c++)
            {
                if ((rowsToClear[r] || colsToClear[c]) && Grid[r][c] != 0)
                {
                    Grid[r][c] = 0;
                    cleared++;
                }
            }
        }

        // scoring
        if (cleared > 0)
        {
            // add the score plus bonuses
            int bonus = 10 * (rowsToClear.Count(b => b) + colsToClear.Count(b => b));
            var playScore = (cleared + bonus) * (PreviousClearCount + 1);
            PreviousClearCount++;

            // update score
            Score += playScore;
        }
        else
        {
            PreviousClearCount = 0;
        }

        // Increment pieces played counter
        PiecesPlayed++;

        return true;
    }

    // check if a piece can be placed at (row, col)
    public bool CanPlacePiece(PieceType type, int row, int col)
    {
        var shape = type.GetShape();
        if (shape == null || shape.Count == 0)
        {
            throw new ArgumentException($"Piece type '{type}' not found.");
        }

        // check if this piece can be placed here
        foreach (var (dr, dc) in shape)
        {
            int r = row + dr;
            int c = col + dc;
            if (r < 0 || r >= GridSize || c < 0 || c >= GridSize)
                return false;
            if (Grid[r][c] != 0)
                return false;
        }
        
        return true;
    }

    // Get a copy of the grid
    public int[][] GetGrid()
    {
        var arr = new int[GridSize][];
        for (int i = 0; i < GridSize; i++)
        {
            arr[i] = new int[GridSize];
            for (int j = 0; j < GridSize; j++)
                arr[i][j] = Grid[i][j];
        }
        return arr;
    }

    // Check if the game is over (no pieces in the list can be placed)
    public bool IsGameOver(IEnumerable<PieceType> availablePieces)
    {
        // Try to place this piece anywhere on the grid
        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                foreach (var piece in availablePieces)
                {
                    if (CanPlacePiece(piece, row, col))
                    {
                        return false; // Found a valid placement, game is not over
                    }
                }
            }
        }
        return true; // No valid placements found for any piece, game is over
    }

    #region private
    private int[][] Grid;
    private int PreviousClearCount;
    #endregion
}
