using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    public class Move
    {
        public Move()
        {
            Coordinate = new Coordinate();
        }

        public Coordinate Coordinate { get; set; }
        public Direction Direction { get; set; }
    }
}
