using System;

namespace TeeGame
{
    public struct Move
    {
        public char Source
        {
            get
            {
                return TeeSupport.TeeToChar(From);
            }
        }

        public char Destination
        {
            get
            {
                return TeeSupport.TeeToChar(To);
            }
        }

        #region internal
        internal Tees From;
        internal Tees Jumped;
        internal Tees To;


        internal Move(Tees from, Tees jumped, Tees to)
        {
            From = from;
            Jumped = jumped;
            To = to;
        }
        #endregion
    }
}
