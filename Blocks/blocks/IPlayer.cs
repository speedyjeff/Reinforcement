
using System;
using System.Collections.Generic;
using blocks.engine;

public interface IPlayer
{
    Move ChooseMove(Blocks blocks, List<PieceType> pieces);
}

