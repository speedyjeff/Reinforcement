using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe
{
    public struct Coordinate 
    { 
        public int Row; 
        public int Column;

        public override string ToString()
        {
            return $"{Row},{Column}";
        }
    }
}
