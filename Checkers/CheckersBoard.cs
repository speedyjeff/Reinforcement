using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    public class CheckersBoard
    {
        public CheckersBoard(int dimension, List<Coordinate> initial = null)
        {
            // init
            Dimension = dimension;

            // determine how many rows of checkers
            int numberRowsOfCheckers;
            switch (Dimension)
            {
                case 8:
                case 7: numberRowsOfCheckers = 3; break;
                case 6:
                case 5: numberRowsOfCheckers = 2; break;
                case 4: numberRowsOfCheckers = 1; break;
                default: throw new Exception("invalid dimension.  must be <=8 and >=4");
            }

            // setup
            Board = new Piece[Dimension][];

            // initialize to a checkers board
            Coordinate coord = new Coordinate();
            for(coord.Row=0; coord.Row < Board.Length; coord.Row++)
            {
                Board[coord.Row] = new Piece[Dimension];
                for (coord.Column = 0; coord.Column < Board[coord.Row].Length; coord.Column++)
                {
                    Board[coord.Row][coord.Column] = new Piece() { IsInvalid = true };

                    // make the checker board
                    if (coord.Row % 2 == 0 && coord.Column % 2 == 0) continue;
                    if (coord.Row % 2 != 0 && coord.Column % 2 != 0) continue;

                    // initialize the board
                    Board[coord.Row][coord.Column].Side = Side.None;
                    Board[coord.Row][coord.Column].IsKing = false;
                    Board[coord.Row][coord.Column].IsInvalid = false;
                }
            }

            if (initial != null && initial.Count > 0)
            {
                // place the initial pieces per the plan
                foreach(var p in initial)
                {
                    if (p.Row < 0 || p.Row >= Dimension ||
                        p.Column < 0 || p.Column >= Dimension) throw new Exception($"Invalid initial placement : {p.Row},{p.Column}");
                    if (Board[p.Row][p.Column].IsInvalid) continue;

                    // place the piece
                    Board[p.Row][p.Column].Side = p.Piece.Side;
                    Board[p.Row][p.Column].IsKing = p.Piece.IsKing;
                }
            }
            else
            {
                // place in the default configuration
                for (var row = 0; row < Board.Length; row++)
                {
                    for (var column = 0; column < Board[row].Length; column++)
                    {
                        if (Board[row][column].IsInvalid) continue;
                        // white
                        if (row < numberRowsOfCheckers) Board[row][column].Side = Side.White;
                        // black
                        else if (row >= (Dimension - numberRowsOfCheckers)) Board[row][column].Side = Side.Black;
                    }
                }
            }

            // init
            Winner = Side.None;
            IsDone = false;

            // white goes first
            Turn = Side.White;
        }

        public Side Turn { get; private set; }
        public Side Winner { get; private set; }
        public bool IsDone { get; private set; }

        public int Dimension { get; private set; }

        public event Action OnSideChange;

        public Piece this[int row, int column]
        {
            get
            {
                if (row < 0 || row >= Board.Length ||
                    column < 0 || column >= Board[row].Length) throw new Exception("invalid index");
                return Board[row][column];
            }
        }

        public List<Move> GetAvailableMoves()
        {
            // based on the current color, determine the moves
            var moves = new List<Move>();
            var jumpMoves = new List<Move>();

            // iterate through the board and determine moves for each of the coordinates
            for (var row = 0; row < Board.Length; row++)
            {
                for (var column = 0; column < Board[row].Length; column++)
                {
                    if (Board[row][column].IsInvalid) continue;

                    // this is the current player
                    if (Board[row][column].Side == Turn)
                    {
                        // check all the possible moves
                        foreach(var direction in new Direction[] { Direction.DownLeft, Direction.DownRight, Direction.UpLeft, Direction.UpRight})
                        {
                            if (IsValidMove(row, column, direction, out int newRow, out int newColumn, out bool isJump))
                            {
                                // create move
                                var move = new Move()
                                {
                                    Coordinate = new Coordinate()
                                    {
                                        Row = row,
                                        Column = column,
                                        Piece = new Piece()
                                        {
                                            Side = Board[row][column].Side,
                                            IsKing = Board[row][column].IsKing,
                                            IsInvalid = false
                                        }
                                    },
                                    Direction = direction
                                };

                                // store
                                if (isJump) jumpMoves.Add(move);
                                else moves.Add(move);
                            }
                        }
                    }
                } // for column
            } // for row

            // return the moves (jumps are mandetory)
            if (jumpMoves.Count > 0) return jumpMoves;
            else if (moves.Count > 0) return moves;

            // for debugging, have a display of the board
            System.Diagnostics.Debug.WriteLine(DisplayBoard());

            CheckDone(out Side winner);

            throw new Exception("failed to find a move");
        }

        public bool TryMakeMove(Move move)
        {
            // input validation
            if (move.Coordinate.Row < 0 || move.Coordinate.Row >= Board.Length ||
                move.Coordinate.Column < 0 || move.Coordinate.Column >= Board[move.Coordinate.Row].Length) throw new Exception("invalid move");

            // most choose a piece that is owned by Turn
            if (Board[move.Coordinate.Row][move.Coordinate.Column].Side != Turn ||
                Board[move.Coordinate.Row][move.Coordinate.Column].IsInvalid) return false;

            // try to apply the move
            if (!IsValidMove(move.Coordinate.Row, move.Coordinate.Column, move.Direction, out int newRow, out int newColumn, out bool isJump)) return false;

            // validate
            var rdelta = move.Coordinate.Row - newRow;
            var cdelta = move.Coordinate.Column - newColumn;
            if (rdelta == 0 || cdelta == 0) throw new Exception("not a valid move");
            if (newRow < 0 || newRow >= Board.Length ||
                newColumn < 0 || newColumn >= Board[newRow].Length ||
                Board[newRow][newColumn].IsInvalid ||
                Board[newRow][newColumn].Side != Side.None) throw new Exception("invalid move");

            // apply the move
            Board[newRow][newColumn].Side = Board[move.Coordinate.Row][move.Coordinate.Column].Side;
            Board[newRow][newColumn].IsKing = Board[move.Coordinate.Row][move.Coordinate.Column].IsKing;

            // remove from the current spot
            Board[move.Coordinate.Row][move.Coordinate.Column].Side = Side.None;
            Board[move.Coordinate.Row][move.Coordinate.Column].IsKing = false;

            // remove any 'jumped' opponents
            if (isJump)
            {
                var jumpRow = move.Coordinate.Row;
                var jumpColumn = move.Coordinate.Column;
                ApplyDirection(move.Direction, ref jumpRow, ref jumpColumn);

                // validate
                if (jumpRow < 0 || jumpRow >= Board.Length ||
                    jumpColumn < 0 || jumpColumn >= Board[jumpRow].Length ||
                    Board[jumpRow][jumpColumn].Side == Side.None ||
                    Board[jumpRow][jumpColumn].Side == Board[move.Coordinate.Row][move.Coordinate.Column].Side ||
                    Board[jumpRow][jumpColumn].IsInvalid) throw new Exception("invalid jump piece");

                // remove the piece
                Board[jumpRow][jumpColumn].Side = Side.None;
                Board[jumpRow][jumpColumn].IsKing = false;
            }

            // is king?
            var isNewKing = false;
            if (newRow == 0 || newRow == (Dimension - 1))
            {
                // note if this is a new king
                isNewKing = !Board[newRow][newColumn].IsKing;

                // must be before check for done (as this king may have jumps)
                Board[newRow][newColumn].IsKing = true;
            }

            // check for completion (when we switch sides)
            IsDone = CheckDone(out Side winner);
            Winner = winner;

            // check if we should switch who's turn it is
            // new kings are not able to continue jumping
            if (isJump && !isNewKing)
            {
                // a jump may lead to another mandatory jump, need to check
                foreach (var direction in new Direction[] { Direction.DownLeft, Direction.DownRight, Direction.UpLeft, Direction.UpRight })
                {
                    if (IsValidMove(newRow, newColumn, direction, out int anotherRow, out int anotherColumn, out bool anotherIsJump))
                    {
                        // return without changing who's turn it is (as there is another mandetory jump)
                        if (anotherIsJump) return true;
                    }
                }
            }

            // switch who's turn it is
            Turn = (Turn == Side.Black) ? Side.White : Side.Black;

            // notify the turn change
            if (OnSideChange != null) OnSideChange();

            return true;
        }

        #region private
        private Piece[][] Board;
        private int PreviousCount = 0;
        private int StalemateCount = 0;

        private const int StalemateMaxCount = 25;

        public bool CheckDone(out Side winner)
        {
            var blackCount = 0;
            var whiteCount = 0;
            var blackHasMove = false;
            var whiteHasMove = false;

            // conditions
            //  - piece count is 0
            //  - no piece has a move
            for (var row = 0; row < Board.Length; row++)
            {
                for (var column = 0; column < Board[row].Length; column++)
                {
                    if (Board[row][column].IsInvalid) continue;

                    // count the number of pieces per side
                    switch (Board[row][column].Side)
                    {
                        case Side.Black: blackCount++; break;
                        case Side.White: whiteCount++; break;
                        case Side.None: break;
                        default: throw new Exception($"unknown side {Board[row][column].Side}");
                    }

                    // only need one successful move to indicate that there is an available move
                    if ((!blackHasMove && Board[row][column].Side == Side.Black) ||
                        (!whiteHasMove && Board[row][column].Side == Side.White))
                    {
                        // check if this one has a move
                        foreach (var direction in new Direction[] { Direction.DownLeft, Direction.DownRight, Direction.UpLeft, Direction.UpRight })
                        {
                            if (IsValidMove(row, column, direction, out int newRow, out int newColumn, out bool anotherIsJump))
                            {
                                if (Board[row][column].Side == Side.White) whiteHasMove = true;
                                else if (Board[row][column].Side == Side.Black) blackHasMove = true;
                                else throw new Exception("invalid side");

                                break;
                            }
                        }
                    }
                } // for row
            } // for column

            // increment stalemate counter
            if (PreviousCount == (blackCount + whiteCount)) StalemateCount++;
            else
            {
                // reset the stalemate counter
                PreviousCount = (blackCount + whiteCount);
                StalemateCount = 0;
            }

            // check if a side has no moves
            if (!blackHasMove && !whiteHasMove)
            {
                winner = Side.None;
                return true;
            }
            else if (!blackHasMove)
            {
                winner = Side.White;
                return true;
            }
            else if (!whiteHasMove)
            {
                winner = Side.Black;
                return true;
            }

            // check for no pieces left
            if (blackCount == 0 && whiteCount == 0) throw new Exception("wha cats!");
            else if (blackCount == 0)
            {
                winner = Side.White;
                return true;
            }
            else if (whiteCount == 0)
            {
                winner = Side.Black;
                return true;
            }

            // check if we at a stalemate
            if (StalemateCount == StalemateMaxCount)
            {
                winner = Side.None;
                return true;
            }

            // no winner
            winner = Side.None;
            return false;
        }

        private void ApplyDirection(Direction direction, ref int row, ref int column)
        {
            switch (direction)
            {
                case Direction.DownLeft:
                    row++;
                    column--;
                    break;
                case Direction.UpLeft:
                    row--;
                    column--;
                    break;
                case Direction.DownRight:
                    row++;
                    column++;
                    break;
                case Direction.UpRight:
                    row--;
                    column++;
                    break;
                default: throw new Exception($"unknown direciton {direction}");
            }
        }

        private bool IsValidMove(int row, int column, Direction direction, out int newRow, out int newColumn, out bool isJump)
        {
            // init
            newRow = row;
            newColumn = column;
            isJump = false;

            // validation
            if (row < 0 || row >= Board.Length ||
                column < 0 || column >= Board[row].Length) throw new Exception("invalid coordinates");
            if (Board[row][column].IsInvalid) throw new Exception("not a valid start");
            if (Board[row][column].Side == Side.None) throw new Exception("not a valid piece");

            // determine if the move is valid (non-King, default rules)
            if (direction == Direction.DownLeft || direction == Direction.DownRight)
            {
                if (!Board[row][column].IsKing && Board[row][column].Side == Side.Black) return false;
            }
            else if (direction == Direction.UpLeft || direction == Direction.UpRight)
            {
                if (!Board[row][column].IsKing && Board[row][column].Side == Side.White) return false;
            }
            else throw new Exception($"unknown direciton {direction}");

            // check if the move is valid
            var attempt = 0;
            do
            {
                // apply the destination
                ApplyDirection(direction, ref newRow, ref newColumn);

                // check if these are out of bounds
                if (newRow < 0 || newRow >= Board.Length ||
                    newColumn < 0 || newColumn >= Board[newRow].Length ||
                    Board[newRow][newColumn].IsInvalid) return false;

                // determine if the destination is open
                if (Board[newRow][newColumn].Side == Side.None) return true;
                // check if this is blocked by our color
                if (Board[newRow][newColumn].Side == Board[row][column].Side) return false;

                // this is a possible jump, move one space further
                isJump = true;
            } while (attempt++ < 1);

            // not possible
            newRow = newColumn = -1;
            isJump = false;
            return false;
        }


        private string DisplayBoard()
        {
            var sb = new StringBuilder();
            for (var row = 0; row < Board.Length; row++)
            {
                for (var column = 0; column < Board[row].Length; column++)
                {
                    var c = Board[row][column].Side == Side.None ? '-' : (Board[row][column].Side == Side.Black ? 'b' : 'w');
                    if (Board[row][column].IsKing) c = char.ToUpper(c);
                    sb.Append(c);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
        #endregion
    }
}
