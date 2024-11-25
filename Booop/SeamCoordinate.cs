using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booop
{
    class SeamCoordinate
    {
        public Direction Direction;
        public Coordinate Coordinate;

        public SeamCoordinate()
        {
            Direction = Direction.None;
            Coordinate = new Coordinate();
        }
    }
}
