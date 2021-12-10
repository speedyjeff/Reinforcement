using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    public class Coordinate
    {
        public Coordinate()
        {
            Piece = new Piece();
        }

        public int Row { get; set; }
        public int Column { get; set; }
        public Piece Piece { get; set; }
    }
}
