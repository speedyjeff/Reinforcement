using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    interface IPlayer
    {
        Move ChooseAction(CheckersBoard board);
        void Finish(CheckersBoard board, Side winner, Move lastMove);
        void Save();
    }
}
