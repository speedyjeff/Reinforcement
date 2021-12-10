using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    public class Piece
    {
        public bool IsKing { get; set; }
        public Side Side { get; set; }
        public bool IsInvalid { get; set; }
    }
}
