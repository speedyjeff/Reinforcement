using System;

namespace TeeGame
{

    // the game of Tees is played on a triangle board with 15 holes and 14 Tees
    // Rules of movement:
    //  jump diagonals or left to right
    //  must jump another piece
    //  once jumped, the piece is removed
    // Goal:
    //  remove all but one Tee
    //
    //         North
    //            ^
    //           / \
    //          / b \
    //         / T T \
    // West   / T T T \  East
    //       / T T T T \
    //      / T T T T T \
    //     ---------------
    //         South
    //

    public class TeeBoard
    {
        public TeeBoard()
        {
            Board = Tees.All ^ Tees._00;
        }

        public TeeBoard(Tees establishedBoard)
        {
            Board = establishedBoard;
        }

        public List<Move> GetAvailableMoves()
        {
            var moves = new List<Move>();
            Move move = new Move();

            // 0
            if (ValidMove(Tees._00, Tees._01, Tees._03, ref move)) moves.Add(move);
            if (ValidMove(Tees._00, Tees._02, Tees._05, ref move)) moves.Add(move);

            // 1
            if (ValidMove(Tees._01, Tees._03, Tees._06, ref move)) moves.Add(move);
            if (ValidMove(Tees._01, Tees._04, Tees._08, ref move)) moves.Add(move);

            // 2
            if (ValidMove(Tees._02, Tees._05, Tees._09, ref move)) moves.Add(move);
            if (ValidMove(Tees._02, Tees._04, Tees._07, ref move)) moves.Add(move);

            // 3
            if (ValidMove(Tees._03, Tees._01, Tees._00, ref move)) moves.Add(move);
            if (ValidMove(Tees._03, Tees._06, Tees._10, ref move)) moves.Add(move);
            if (ValidMove(Tees._03, Tees._07, Tees._12, ref move)) moves.Add(move);
            if (ValidMove(Tees._03, Tees._04, Tees._05, ref move)) moves.Add(move);

            // 4
            if (ValidMove(Tees._04, Tees._07, Tees._11, ref move)) moves.Add(move);
            if (ValidMove(Tees._04, Tees._08, Tees._13, ref move)) moves.Add(move);

            // 5
            if (ValidMove(Tees._05, Tees._02, Tees._00, ref move)) moves.Add(move);
            if (ValidMove(Tees._05, Tees._08, Tees._12, ref move)) moves.Add(move);
            if (ValidMove(Tees._05, Tees._09, Tees._14, ref move)) moves.Add(move);
            if (ValidMove(Tees._05, Tees._04, Tees._03, ref move)) moves.Add(move);

            // 6
            if (ValidMove(Tees._06, Tees._03, Tees._01, ref move)) moves.Add(move);
            if (ValidMove(Tees._06, Tees._07, Tees._08, ref move)) moves.Add(move);

            // 7
            if (ValidMove(Tees._07, Tees._04, Tees._02, ref move)) moves.Add(move);
            if (ValidMove(Tees._07, Tees._08, Tees._09, ref move)) moves.Add(move);

            // 8
            if (ValidMove(Tees._08, Tees._04, Tees._01, ref move)) moves.Add(move);
            if (ValidMove(Tees._08, Tees._07, Tees._06, ref move)) moves.Add(move);

            // 9
            if (ValidMove(Tees._09, Tees._05, Tees._02, ref move)) moves.Add(move);
            if (ValidMove(Tees._09, Tees._08, Tees._07, ref move)) moves.Add(move);

            // 10
            if (ValidMove(Tees._10, Tees._06, Tees._03, ref move)) moves.Add(move);
            if (ValidMove(Tees._10, Tees._11, Tees._12, ref move)) moves.Add(move);

            // 11
            if (ValidMove(Tees._11, Tees._07, Tees._04, ref move)) moves.Add(move);
            if (ValidMove(Tees._11, Tees._12, Tees._13, ref move)) moves.Add(move);

            // 12
            if (ValidMove(Tees._12, Tees._07, Tees._03, ref move)) moves.Add(move);
            if (ValidMove(Tees._12, Tees._08, Tees._05, ref move)) moves.Add(move);
            if (ValidMove(Tees._12, Tees._11, Tees._10, ref move)) moves.Add(move);
            if (ValidMove(Tees._12, Tees._13, Tees._14, ref move)) moves.Add(move);

            // 13
            if (ValidMove(Tees._13, Tees._08, Tees._04, ref move)) moves.Add(move);
            if (ValidMove(Tees._13, Tees._12, Tees._11, ref move)) moves.Add(move);

            // 14
            if (ValidMove(Tees._14, Tees._09, Tees._05, ref move)) moves.Add(move);
            if (ValidMove(Tees._14, Tees._13, Tees._12, ref move)) moves.Add(move);

            return moves;
        }

        public bool IsSet(char id)
        {
            var position = TeeSupport.CharToTee(id);
            return ((Board & position) != 0);
        }

        public int Count()
        {
            var count = 0;
            if ((Board & Tees._00) != 0) count++;
            if ((Board & Tees._01) != 0) count++;
            if ((Board & Tees._02) != 0) count++;
            if ((Board & Tees._03) != 0) count++;
            if ((Board & Tees._04) != 0) count++;
            if ((Board & Tees._05) != 0) count++;
            if ((Board & Tees._06) != 0) count++;
            if ((Board & Tees._07) != 0) count++;
            if ((Board & Tees._08) != 0) count++;
            if ((Board & Tees._09) != 0) count++;
            if ((Board & Tees._10) != 0) count++;
            if ((Board & Tees._11) != 0) count++;
            if ((Board & Tees._12) != 0) count++;
            if ((Board & Tees._13) != 0) count++;
            if ((Board & Tees._14) != 0) count++;
            return count;
        }

        public bool TryApplyMove(Move move)
        {
            // validate
            var tmp = new Move();
            if (!ValidMove(move.From, move.Jumped, move.To, ref tmp)) throw new Exception("invalid move");

            // apply (toggle the bit for each of these positions)
            Board ^= move.From;
            Board ^= move.Jumped;
            Board ^= move.To;

            return true;
        }

        public short BoardHash()
        {
            return (short)Board;
        }

        #region private
        private Tees Board;

        private bool ValidMove(Tees first, Tees second, Tees third, ref Move move)
        {
            // check if valid
            if (((Board & first) != 0) &&
                ((Board & second) != 0) &&
                ((Board & third) == 0))
            {
                // store for the move
                move.From = first;
                move.Jumped = second;
                move.To = third;

                return true;
            }

            return false;
        }

        #endregion
    }
}
