using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booop
{
    abstract class PlayerBase
    {
        public PlayerType Player { get; protected set; }
        public bool Verbose { get; protected set; }
        public PlayerBase(PlayerType player, bool verbose)
        {
            Player = player;
            Verbose = verbose;
        }

        // called to make take a turn for the player
        public virtual bool TryMakeMove(Board board) { return false; }

        // in the event that all pieces are on the board, the player must choose a small piece to upgrade to large
        public virtual Coordinate ChooseUpgradePiece(Board board, List<Coordinate> coords) { return coords[0]; }

        public virtual void Feedback(Board board, bool complete) { }
    }
}
