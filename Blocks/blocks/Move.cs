using System;
using blocks.engine;

public struct Move
{
    public PieceType Piece { get; private set; }
    public int Row { get; private set; }
    public int Col { get; private set; }

    public Move(PieceType piece, int row, int col)
    {
        Piece = piece;
        Row = row;
        Col = col;
    }
}
