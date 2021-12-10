using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    public struct MinimalBoard
    {
        public static MinimalBoard Create(CheckersBoard board)
        {
            // create the minimal board based on the CheckersBoard
            var mboard = new MinimalBoard();
            var coord = new Coordinate();
            for(coord.Row =0; coord.Row < board.Dimension; coord.Row++)
            {
                for(coord.Column =0; coord.Column < board.Dimension; coord.Column++)
                {
                    coord.Piece = board[coord.Row, coord.Column];
                    if (coord.Piece.Side != Side.None)
                    {
                        mboard.Put(coord);
                    }
                }
            }

            return mboard;
        }

        public void Clear(Coordinate coord)
        {
            if (!GetMask(coord, out int index, out int shift, out int kingIndex, out int kingShift)) throw new Exception("failed to get mask");

            byte mask;
            // clear out the piece
            mask = (byte)~(0x3 << shift);
            Data[index] &= mask;

            // clear out the king
            mask = (byte)~(0x1 << kingShift);
            Data[kingIndex] &= mask;
        }

        public void Put(Coordinate coord)
        {
            if (!GetMask(coord, out int index, out int shift, out int kingIndex, out int kingShift)) throw new Exception("failed to get mask");

            byte mask;
            // put piece
            mask = (byte)(GetSide(coord.Piece.Side) << shift);
            if ((Data[index] & (byte)(0x3 << shift)) != 0) throw new Exception("an existing piece is present");
            Data[index] |= mask;

            // indicate if king
            if (coord.Piece.IsKing)
            {
                mask = (byte)(0x1 << kingShift);
                if ((Data[kingIndex] & (byte)(0x1 << kingShift)) != 0) throw new Exception("an existing king is present");
                Data[kingIndex] |= mask;
            }
        }

        public string AsString()
        {
            var text = $"{Data[0]:x2}{Data[1]:x2}{Data[2]:x2}{Data[3]:x2}{Data[4]:x2}{Data[5]:x2}{Data[6]:x2}{Data[7]:x2}{Data[8]:x2}{Data[9]:x2}{Data[10]:x2}{Data[11]:x2}";
            if (string.IsNullOrWhiteSpace(text)) throw new Exception("failed to create string");
            return text;
        }

        public override string ToString()
        {
            // for debug purposes print out the hex values
            return $"{Convert.ToString(Data[0], 2)} {Convert.ToString(Data[1], 2)} {Convert.ToString(Data[2], 2)} {Convert.ToString(Data[3], 2)} {Convert.ToString(Data[4], 2)} {Convert.ToString(Data[5], 2)} {Convert.ToString(Data[6], 2)} {Convert.ToString(Data[7], 2)} | {Convert.ToString(Data[8], 2)} {Convert.ToString(Data[9], 2)} {Convert.ToString(Data[10], 2)} {Convert.ToString(Data[11], 2)}";
        }

        #region private
        //   0 1 2 3 4 5 6 7
        // 0 - 0 - 0 - 0 - 0         --> 33221100 (8 bits, every 2 bits represents a slot)
        // 1 1 - 1 - 1 - 1 - kings 8 
        // 2 - 2 - 2 - 2 - 2  
        // 3 3 - 3 - 3 - 3 - kings 9
        // 4 - 4 - 4 - 4 - 4 
        // 5 5 - 5 - 5 - 5 - kings 10
        // 6 - 6 - 6 - 6 - 6 
        // 7 7 - 7 - 7 - 7 - kings 11
        private byte[] Data = new byte[12];
        private const int SideSlot = 0;
        private const int KingSlot = 8;

        private int GetSide(Side side)
        {
            switch(side)
            {
                case Side.White: return 0x2;
                case Side.Black: return 0x1;
                default: throw new Exception($"unknown side : {side}");
            }
        }

        private bool GetMask(Coordinate coord, out int index, out int shift, out int kingIndex, out int kingShift)
        {
            // get piece info
            index = SideSlot + coord.Row;
            shift = (int)Math.Floor((double)coord.Column / 2d) * 2;

            // get king info
            kingIndex = KingSlot + (int)Math.Floor((double)coord.Row / 2d);
            kingShift = (int)Math.Floor((double)coord.Column / 2d) + (coord.Row % 2 != 0 ? 4 : 0);

            // validate
            if (coord.Column < 0 || coord.Column >= 8) throw new Exception("invalid column");
            if (coord.Row < 0 || coord.Row >= 8) throw new Exception("invalid row");

            return true;
        }

        #endregion
    }
}
