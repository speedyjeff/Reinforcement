using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booop
{
    struct Piece
    {
        public Piece()
        {
            Type = PieceType.None;
            Player = PlayerType.None;
        }

        public PieceType Type { get; set; }
        public PlayerType Player { get; set; }
    }
}
