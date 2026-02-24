using System;

public enum PieceType
{
    Invalid_First = 0,
    Single = 1,
    Vertical2 = 2,
    Vertical3 = 3,
    Vertical4 = 4,
    Vertical5 = 5,
    Square2x2 = 6,
    Square3x3 = 7,
    Vertical2x3 = 8,
    Horizontal2 = 9,
    Horizontal3 = 10,
    Horizontal4 = 11,
    Horizontal5 = 12,
    Horizontal2x3 = 13,
    CornerTopLeft = 14,
    CornerTopRight = 15,
    CornerBottomLeft = 16,
    CornerBottomRight = 17,
    TUp = 18,
    TDown = 19,
    TLeft = 20,
    TRight = 21,
    ZLeft = 22,
    ZRight = 23,
    ZUp = 24,
    ZDown = 25,
    BigTUp = 26,
    BigTDown = 27,
    BigTLeft = 28,
    BigTRight = 29,
    NUp = 30,
    NDown = 31,
    NLeft = 32,
    NRight = 33,
    BigLTopLeft = 34,
    BigLTopRight = 35,
    BigLBottomLeft = 36,
    BigLBottomRight = 37,
    BigLHorizontalTopLeft = 38,
    BigLHorizontalTopRight = 39,
    BigLHorizontalBottomLeft = 40,
    BigLHorizontalBottomRight = 41,
    BigCornerTopLeft = 42,
    BigCornerTopRight = 43,
    BigCornerBottomLeft = 44,
    BigCornerBottomRight = 45,
    Invalid_Last = 46
}

public static class PieceTypeExtensions
{
    public static List<(int, int)> GetShape(this PieceType type)
    {
        if (!Shapes.TryGetValue(type, out var shape))
        {
            throw new ArgumentException($"Piece type '{type}' not found.");
        }
        return shape;
    }

    public static string GetAscii(this PieceType type)
    {
        return type switch
        {
            PieceType.Single => "■",
            PieceType.Vertical2 => "┃2",
            PieceType.Vertical3 => "┃3",
            PieceType.Vertical4 => "┃4",
            PieceType.Vertical5 => "┃5",
            PieceType.Square2x2 => "■2x2",
            PieceType.Square3x3 => "■3x3",
            PieceType.Vertical2x3 => "┃2x3",
            PieceType.Horizontal2 => "━2",
            PieceType.Horizontal3 => "━3",
            PieceType.Horizontal4 => "━4",
            PieceType.Horizontal5 => "━5",
            PieceType.Horizontal2x3 => "▭2x3",
            PieceType.CornerTopLeft => "┏3",
            PieceType.CornerTopRight => "┓3",
            PieceType.CornerBottomLeft => "┗3",
            PieceType.CornerBottomRight => "┛3",
            PieceType.TUp => "┳4",
            PieceType.TDown => "┻4",
            PieceType.TLeft => "┫4",
            PieceType.TRight => "┣4",
            PieceType.ZUp => "Z↑",
            PieceType.ZDown => "Z↓",
            PieceType.ZLeft => "Z←",
            PieceType.ZRight => "Z→",
            PieceType.BigTUp => "┳5",
            PieceType.BigTDown => "┻5",
            PieceType.BigTLeft => "┣5",
            PieceType.BigTRight => "┫5",
            PieceType.NUp => "∩4",
            PieceType.NDown => "U4",
            PieceType.NLeft => "⊏4",
            PieceType.NRight => "⊐4",
            PieceType.BigLTopLeft => "┐4",
            PieceType.BigLTopRight => "┌4",
            PieceType.BigLBottomLeft => "┘4",
            PieceType.BigLBottomRight => "└4",
            PieceType.BigLHorizontalTopLeft => "⌐↖4",
            PieceType.BigLHorizontalTopRight => "⌐↗4",
            PieceType.BigLHorizontalBottomLeft => "⌐↙4",
            PieceType.BigLHorizontalBottomRight => "⌐↘4",
            PieceType.BigCornerTopLeft => "┏5",
            PieceType.BigCornerTopRight => "┓5",
            PieceType.BigCornerBottomLeft => "┗5",
            PieceType.BigCornerBottomRight => "┛5",
            _ => throw new Exception("unknown piece")
        };
    }

    public static string GetString(this PieceType type, string prefix, string suffix)
    {
        // get the spatial shape for this piece
        var shape = type.GetShape();
        if (shape == null || shape.Count == 0)
        {
            return "unknown piece";
        }

        // Find the bounding box of the piece
        int maxRow = 0;
        int maxCol = 0;
        foreach (var (row, col) in shape)
        {
            if (row > maxRow) maxRow = row;
            if (col > maxCol) maxCol = col;
        }
        
        // Create a jagged array to represent the piece
        var grid = new char[maxRow + 1][];
        
        // Initialize grid with spaces
        for (int r = 0; r <= maxRow; r++)
        {
            grid[r] = new char[maxCol + 1];
            for (int c = 0; c <= maxCol; c++)
            {
                grid[r][c] = ' ';
            }
        }
        
        // Fill in the piece coordinates with '#'
        foreach (var (row, col) in shape)
        {
            grid[row][col] = '#';
        }
        
        // Convert grid to string with newlines
        var result = new System.Text.StringBuilder();
        result.Append(prefix);
        for (int r = 0; r <= maxRow; r++)
        {
            for (int c = 0; c <= maxCol; c++)
            {
                result.Append(grid[r][c]);
            }

            // go to next line
            result.Append(suffix);
            if (r < maxRow) // Don't add newline after the last row
            {
                result.AppendLine();
                result.Append(prefix);
            }
        }
        
        return result.ToString();
    }

    #region private
    // Piece shapes: each piece is a list of (row, col) offsets from origin (no negative offsets)
    private static readonly Dictionary<PieceType, List<(int, int)>> Shapes = new()
    {
        { PieceType.Single, new()
        {
            (0, 0)
        } },
        { PieceType.Vertical2, new()
        {
            (0, 0),
            (1, 0)
        } },
        { PieceType.Vertical3, new()
        {
            (0, 0),
            (1, 0),
            (2, 0)
        } },
        { PieceType.Vertical4, new()
        {
            (0, 0),
            (1, 0),
            (2, 0),
            (3, 0)
        } },
        { PieceType.Vertical5, new()
        {
            (0,0),
            (1,0),
            (2,0),
            (3,0),
            (4,0)
        } },
        { PieceType.Vertical2x3, new()
        {
            (0, 0), (0, 1),
            (1, 0), (1, 1),
            (2, 0), (2, 1)
        } },
        { PieceType.Horizontal2, new()
        {
            (0, 0), (0, 1)
        } },
        { PieceType.Horizontal3, new()
        {
            (0, 0), (0, 1), (0, 2)
        } },
        { PieceType.Horizontal4, new()
        {
            (0, 0), (0, 1), (0, 2), (0, 3)
        } },
        { PieceType.Horizontal5, new()
        {
            (0,0),(0,1),(0,2),(0,3),(0,4)
        } },
        { PieceType.Horizontal2x3, new()
        {
            (0, 0), (0, 1), (0, 2),
            (1, 0), (1, 1), (1, 2)
        } },
        { PieceType.Square2x2, new() {
            (0,0),(0,1),
            (1,0),(1,1)
        } },
        { PieceType.Square3x3, new() {
            (0,0),(0,1),(0,2),
            (1,0),(1,1),(1,2),
            (2,0),(2,1),(2,2)
        } },
        { PieceType.CornerTopLeft, new()
        {
            (0,0),(0,1),
            (1,0)
        } },
        { PieceType.CornerTopRight, new()
        {
            (0,0),(0,1),
                  (1,1)
        } },
        { PieceType.CornerBottomLeft, new()
        {
            (0,0),
            (1,0),(1,1)
        } },
        { PieceType.CornerBottomRight, new()
        {
                  (0,1),
            (1,0),(1,1)
        } },
        { PieceType.TUp, new()
        {
                  (0,1),
            (1,0),(1,1),(1,2)
        } },
        { PieceType.TDown, new()
        {
            (0,0),(0,1),(0,2),
                  (1,1)
        } },
        { PieceType.TLeft, new()
        {
                  (0,1),
            (1,0),(1,1),
                  (2,1)
        } },
        { PieceType.TRight, new()
        {
            (0,0),
            (1,0),(1,1),
            (2,0)
        } },
        { PieceType.ZLeft, new()
        {
            (0,0),(0,1),
                  (1,1),(1,2)
        } },
        { PieceType.ZRight, new()
        {
                  (0,1),(0,2),
            (1,0),(1,1)
        } },
        { PieceType.ZUp, new()
        {
                  (0,1),
            (1,0),(1,1),
            (2,0)
        } },
        { PieceType.ZDown, new()
        {
            (0,0),
            (1,0),(1,1),
                  (2,1)
        } },
        { PieceType.BigTUp, new()
        {
            (0,0),(0,1),(0,2),
                  (1,1),
                  (2,1)
        } },
        { PieceType.BigTDown, new()
        {
                  (0,1),
                  (1,1),
            (2,0),(2,1),(2,2)
        } },
        { PieceType.BigTLeft, new()
        {
            (0,0),
            (1,0),(1,1),(1,2),
            (2,0)
        } },
        { PieceType.BigTRight, new()
        {
                        (0,2),
            (1,1),(1,0),(1,2),
                        (2,2)
        } },
        { PieceType.NUp,    new()
        {
            (0,0),(0,1),(0,2),
            (1,0),      (1,2)
        } },
        { PieceType.NDown,  new()
        {
            (0,0),      (0,2),
            (1,0),(1,1),(1,2)
        } },
        { PieceType.NLeft,  new()
        {
            (0,0),(0,1),
            (1,0),
            (2,0),(2,1)
        } },
        { PieceType.NRight, new()
        {
            (0,0),(0,1),
                  (1,1),
            (2,0),(2,1)
        } },
        { PieceType.BigLTopLeft, new()
        {
            (0,0),(0,1),
                  (1,1),
                  (2,1)
        } },
        { PieceType.BigLTopRight, new()
        {
            (0,0),(0,1),
            (1,0),
            (2,0)
        } },
        { PieceType.BigLBottomLeft, new()
        {
                  (0,1),
                  (1,1),
            (2,0),(2,1)
        } },
        { PieceType.BigLBottomRight, new()
        {
            (0,0),
            (1,0),
            (2,0),(2,1)
        } },
        { PieceType.BigLHorizontalTopLeft, new()
        {

            (0,0),
            (1,0),(1,1),(1,2)
        } },
        { PieceType.BigLHorizontalTopRight, new()
        {
                        (0,2),
            (1,0),(1,1),(1,2)
        } },
        { PieceType.BigLHorizontalBottomLeft, new()
        {
            (0,0),(0,1),(0,2),
            (1,0),
        } },
        { PieceType.BigLHorizontalBottomRight, new()
        {
            (0,0),(0,1),(0,2),
                        (1,2)
        } },
        { PieceType.BigCornerTopLeft, new()
        {
            (0,0),(0,1),(0,2),
            (1,0),
            (2,0)
        } },
        { PieceType.BigCornerTopRight, new()
        {
            (0,0),(0,1),(0,2),
                        (1,2),
                        (2,2)
        } },
        { PieceType.BigCornerBottomLeft, new()
        {
            (0,0),
            (1,0),
            (2,0),(2,1),(2,2)
        } },
        { PieceType.BigCornerBottomRight, new()
        {
                        (0,2),
                        (1,2),
            (2,0),(2,1),(2,2)
        } }
    };
    #endregion
}