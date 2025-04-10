using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maze
{
    public class GridLocation
    {
        public int Row;
        public int Column;
        public static int MaxColumn;

        public GridLocation ApplyDirection(Direction d)
        {
            switch (d)
            {
                case Direction.Down: return new GridLocation() { Row = Row + 1, Column = Column };
                case Direction.Left: return new GridLocation() { Row = Row, Column = Column - 1 };
                case Direction.Right: return new GridLocation() { Row = Row, Column = Column + 1 };
                case Direction.Up: return new GridLocation() { Row = Row - 1, Column = Column };
                default: throw new Exception("Invalid direction to apply : " + d);
            }
        }

        public override bool Equals(object? other)
        {
            var loc = other as GridLocation;
            return loc != null && Row == loc.Row && Column == loc.Column;
        }

        public int ToInt32()
        {
            return (Row * MaxColumn) + Column;
        }

        public override int GetHashCode()
        {
            return ToInt32();
        }
    }
}
