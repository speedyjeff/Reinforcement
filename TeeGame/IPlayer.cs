using System;

namespace TeeGame
{
    interface IPlayer
    {
        Move ChooseAction(TeeBoard board);
        void Finish(TeeBoard board, int teesRemaining, Move lastMove);
    }
}
