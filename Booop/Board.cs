using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

//    a  b  c  d  e  f
//   ------------------- seam
// 0 |  |  |  |  |  |  |
//   ------------------- v
// 1 |  |  |  |  |  |  |
//   ------------------- w
// 2 |  |  |  |  |  |  |
//   ------------------- x
// 3 |  |  |  |  |  |  |
//   ------------------- y
// 4 |  |  |  |  |  |  |
//   ------------------- z
// 5 |  |  |  |  |  |  |
//   -------------------
// seam 0  1  2  3  4  

// inspired by rules https://drive.google.com/file/d/15W8ugnaJ7ojxS_lW2I9JHgw7FG0eRpKh/view

namespace Booop
{
    class Board
    {
        public Board(int rows, int columns, int initialSmall, int initialLarge)
        {
            // init
            Rows = rows;
            Columns = columns;
            Winner = PlayerType.None;

            // check that initial piece does not exceed the cap
            if (initialSmall < 0 || initialSmall > MaxPieceCount) throw new Exception("invalid initial piece count");
            if (initialLarge < 0 || initialLarge > MaxPieceCount) throw new Exception("invalid initial piece count");

            // initialize spaces
            Spaces = new Piece[Rows][];
            for (int r = 0; r < Rows; r++)
            {
                Spaces[r] = new Piece[Columns];
                for(int c=0; c < Columns; c++)
                {
                    Spaces[r][c] = new Piece();
                }
            }

            // initialize piece counts
            PieceCounts = new int[3][]; // number of players
            for (int i = 0; i < PieceCounts.Length; i++)
            {
                PieceCounts[i] = new int[4]; // number of piece types

                if ((PlayerType)i == PlayerType.None) continue;
                PieceCounts[i][(int)PieceType.Small] = initialSmall;
                PieceCounts[i][(int)PieceType.Large] = initialLarge;
                PieceCounts[i][(int)PieceType.Seam] = 0;
            }

            // seams
            Seams = new SeamPiece[Math.Max((int)PlayerType.Purple, (int)PlayerType.Orange) + 1];  // number of Players
            for(int i=0; i<Seams.Length; i++) Seams[i] = new SeamPiece();
        }

        public Board(Board board)
        {
            // copy the contents from board into this instance
            Rows = board.Rows;
            Columns = board.Columns;
            Winner = board.Winner;

            // copy the spaces
            Spaces = new Piece[Rows][];
            for (int r = 0; r < Rows; r++)
            {
                Spaces[r] = new Piece[Columns];
                for (int c = 0; c < Columns; c++)
                {
                    Spaces[r][c] = new Piece()
                    {
                        Player = board.Spaces[r][c].Player,
                        Type = board.Spaces[r][c].Type
                    };
                }
            }

            // copy the piece counts
            PieceCounts = new int[board.PieceCounts.Length][];
            for (int i = 0; i < PieceCounts.Length; i++)
            {
                PieceCounts[i] = new int[board.PieceCounts[i].Length];
                for (int j = 0; j < PieceCounts[i].Length; j++)
                {
                    PieceCounts[i][j] = board.PieceCounts[i][j];
                }
            }

            // copy the seams
            Seams = new SeamPiece[board.Seams.Length];
            for (int i = 0; i < Seams.Length; i++)
            {
                Seams[i] = new SeamPiece()
                {
                    InUse = board.Seams[i].InUse,
                    CoordAfter = new Coordinate()
                    {
                        Row = board.Seams[i].CoordAfter.Row,
                        Column = board.Seams[i].CoordAfter.Column
                    },
                    CoordInc = new Coordinate()
                    {
                        Row = board.Seams[i].CoordInc.Row,
                        Column = board.Seams[i].CoordInc.Column
                    }
                };
            }

            // copy the event
            //OnGameWon = board.OnGameWon;
        }

        public PlayerType Winner { get; private set; }
        public int Rows { get; private set; }
        public int Columns { get; private set; }
        public Action<PlayerType> OnGameWon { get; set; }

        public const int MaxPieceCount = 8;

        public bool TryGetAvailablePieces(PlayerType player, out int small, out int large, out bool seam)
        {
            if (player == PlayerType.None)
            {
                small = 0;
                large = 0;
                seam = false;
                return false;
            }

            // return the counts
            small = PieceCounts[(int)player][(int)PieceType.Small];
            large = PieceCounts[(int)player][(int)PieceType.Large];
            seam = !Seams[(int)player].InUse;
            return true;
        }

        public bool TryGetAvailableMoves(PlayerType player, out List<Coordinate> moves)
        {
            if (player == PlayerType.None)
            {
                moves = null;
                return false;
            }

            // consider all empty spaces as available
            moves = new List<Coordinate>();
            for (int row = 0; row < Spaces.Length; row++)
            {
                for(int col = 0; col < Spaces[row].Length; col++)
                {
                    if (Spaces[row][col].Player == PlayerType.None)
                    {
                        moves.Add(new Coordinate() { Row = row, Column = col });
                    }
                }
            }

            // check that there is a valid move available
            if (moves.Count == 0) throw new Exception("no available moves");

            // return
            return true;
        }

        public bool TryGetAvailableSeams(PlayerType player, out List<SeamCoordinate> seams)
        {
            if (player == PlayerType.None || Seams[(int)player].InUse)
            {
                seams = null;
                return false;
            }

            // todo - this list is always the same - cache?
            // get possible seam locations
            seams = new List<SeamCoordinate>();

            // vertical
            foreach (int row in new int[] {0, Rows-1})
            {
                for (int col = 0; col < Columns-1; col++)
                {
                    seams.Add(new SeamCoordinate() { Coordinate = new Coordinate() { Row = row, Column = col }, Direction = Direction.Vertical });
                }
            }

            // horizontal
            foreach (int col in new int[] {0, Columns-1})
            {
                for (int row = 0; row < Rows-1; row++)
                {
                    seams.Add(new SeamCoordinate() { Coordinate = new Coordinate() { Row = row, Column = col }, Direction = Direction.Horizontal });
                }
            }

            return true;
        }

        public bool TryResolveOutOfPieces(PlayerType player, Func<Board, List<Coordinate>, Coordinate> toReplace)
        {
            // in the event that the player does not have a piece to play (all 8 on the board)
            // then they need to choose which piece to 'upgrade' as a large

            // validate
            if (player == PlayerType.None) return false;
            if (toReplace == null) return false;

            // check if the player has all 8 pieces on the board
            if (PieceCounts[(int)player][(int)PieceType.Small] > 0 || PieceCounts[(int)player][(int)PieceType.Large] > 0) return true;

            // get the coordinates for the 8 small pieces
            var smallCoordinates = new List<Coordinate>();
            if (!TryGetActivePieceCounts(player, out int small, out int large, smallCoordinates)) throw new Exception("not able to get pieces");
            if (large == MaxPieceCount) return true;
            if (smallCoordinates.Count == 0) throw new Exception("no small pieces found");

            // request that a coordinate be selected to replace with a large
            var coord = toReplace(this, smallCoordinates);

            // validate the coordinate
            if (coord.Row < 0 || coord.Row >= Rows || coord.Column < 0 || coord.Column >= Columns) return false;
            if (Spaces[coord.Row][coord.Column].Player != player || Spaces[coord.Row][coord.Column].Type != PieceType.Small) return false;

            // clear the piece off the board
            Spaces[coord.Row][coord.Column].Type = PieceType.None;
            Spaces[coord.Row][coord.Column].Player = PlayerType.None;

            // give back a large piece
            PieceCounts[(int)player][(int)PieceType.Large]++;
            if (PieceCounts[(int)player][(int)PieceType.Large] > MaxPieceCount) PieceCounts[(int)player][(int)PieceType.Large] = MaxPieceCount;

            return true;
        }

        public bool TryTurn(PlayerType player, PieceType piece, Coordinate coord, SeamCoordinate seam)
        {
            // exit early if the game is over
            if (Winner != PlayerType.None) return false;

            // track if the seam piece has been returned (as it cannot be placed on the same turn)
            var seamPieceReturned = false;

            // validation piece
            if (player != PlayerType.Orange && player != PlayerType.Purple) return false;
            if (coord.Row < 0 || coord.Row >= Rows || coord.Column < 0 || coord.Column >= Columns) return false;
            if (Spaces[coord.Row][coord.Column].Type != PieceType.None) return false;
            if (piece != PieceType.Small && piece != PieceType.Large) return false;
            if (PieceCounts[(int)player][(int)piece] <= 0 || PieceCounts[(int)player][(int)piece] > MaxPieceCount) throw new Exception("invalid piece count");

            // validate seam
            if (seam.Direction != Direction.None)
            {
                // validation (eg. ensure there is only one of this players Seam pieces on the board)
                if ((int)player <= 0 || (int)player >= Seams.Length) return false;
                if (Seams[(int)player].InUse) return false;
                if (seam.Direction != Direction.Horizontal && seam.Direction != Direction.Vertical) return false;

                // check that the seam is at the edge of the board
                if (seam.Direction == Direction.Horizontal)
                {
                    if (seam.Coordinate.Column != 0 && seam.Coordinate.Column != Columns - 1) return false;
                    if (seam.Coordinate.Row < 0 || seam.Coordinate.Row >= Rows - 1) return false;
                }
                else if (seam.Direction == Direction.Vertical)
                {
                    if (seam.Coordinate.Row != 0 && seam.Coordinate.Row != Rows - 1) return false;
                    if (seam.Coordinate.Column < 0 || seam.Coordinate.Column >= Columns - 1) return false;
                }
                else throw new Exception("invalid direction");
            }

            // put the piece on the board
            Spaces[coord.Row][coord.Column].Type = piece;
            Spaces[coord.Row][coord.Column].Player = player;
            PieceCounts[(int)player][(int)piece]--;
            if (PieceCounts[(int)player][(int)piece] < 0) throw new Exception("invalid piece count");

            // resolve 'Booops' in all directions
            TryResolveBooop(
                new Coordinate() { Row = coord.Row, Column = coord.Column },
                new Coordinate() { Row = coord.Row - 1, Column = coord.Column - 1 });
            TryResolveBooop(
                new Coordinate() { Row = coord.Row, Column = coord.Column },
                new Coordinate() { Row = coord.Row - 1, Column = coord.Column });
            TryResolveBooop(
                new Coordinate() { Row = coord.Row, Column = coord.Column },
                new Coordinate() { Row = coord.Row - 1, Column = coord.Column + 1 });
            TryResolveBooop(
                new Coordinate() { Row = coord.Row, Column = coord.Column },
                new Coordinate() { Row = coord.Row, Column = coord.Column - 1 });
            TryResolveBooop(
                new Coordinate() { Row = coord.Row, Column = coord.Column },
                new Coordinate() { Row = coord.Row, Column = coord.Column + 1 });
            TryResolveBooop(
                new Coordinate() { Row = coord.Row, Column = coord.Column },
                new Coordinate() { Row = coord.Row + 1, Column = coord.Column - 1 });
            TryResolveBooop(
                new Coordinate() { Row = coord.Row, Column = coord.Column },
                new Coordinate() { Row = coord.Row + 1, Column = coord.Column });
            TryResolveBooop(
                new Coordinate() { Row = coord.Row, Column = coord.Column },
                new Coordinate() { Row = coord.Row + 1, Column = coord.Column + 1 });

            // move the seam piece
            if (Seams[(int)player].InUse)
            {
                // increment
                Seams[(int)player].CoordAfter.Row += Seams[(int)player].CoordInc.Row;
                Seams[(int)player].CoordAfter.Column += Seams[(int)player].CoordInc.Column;

                // check if the piece has moved off the board
                if (Seams[(int)player].CoordAfter.Row < 0 || Seams[(int)player].CoordAfter.Row > (Rows - 1) ||
                    Seams[(int)player].CoordAfter.Column < 0 || Seams[(int)player].CoordAfter.Column > (Columns - 1))
                {
                    // track that the seam piece has been returned
                    seamPieceReturned = true;

                    // clear the seam piece
                    Seams[(int)player].InUse = false;
                }
            }

            //  resolve the seam piece 'Booop'ing
            TryResolveSeamBooop(player);

            // put the seam piece
            if (seam.Direction != Direction.None)
            {
                if (seamPieceReturned) throw new Exception("invalid seam usage");  // cannot put the seam piece back on the board the same turn

                // put the piece
                Seams[(int)player].InUse = true;
                Seams[(int)player].CoordAfter.Row = seam.Coordinate.Row;
                Seams[(int)player].CoordAfter.Column = seam.Coordinate.Column;

                // set direction incrementors
                Seams[(int)player].CoordInc.Row = 0;
                if (seam.Direction == Direction.Vertical)
                {
                    if (seam.Coordinate.Row == 0) Seams[(int)player].CoordInc.Row = 1;
                    else Seams[(int)player].CoordInc.Row = -1;
                }
                Seams[(int)player].CoordInc.Column = 0;
                if (seam.Direction == Direction.Horizontal)
                {
                    if (seam.Coordinate.Column == 0) Seams[(int)player].CoordInc.Column = 1;
                    else Seams[(int)player].CoordInc.Column = -1;
                }
            }

            // resolve 3 of a kinds
            // todo - what to do when there is a 3+
            TryResolveThreeOfAKind(player);

            // check for alternative winning conditions
            if (Winner == PlayerType.None)
            {
                // if all 8 of the players Large pieces are on the board, they win
                TryGetActivePieceCounts(player, out int small, out int large, smallCoordinates: null);

                if (large == MaxPieceCount)
                {
                    // winner
                    Winner = player;
                    if (OnGameWon != null) OnGameWon(player);
                }
            }

            return true;
        }

        public bool TryGetCell(Coordinate coord, out PieceType piece, out PlayerType player)
        {
            // validate that the coordinate is within the board
            if (coord.Row < 0 || coord.Row >= Rows || coord.Column < 0 || coord.Column >= Columns)
            {
                piece = PieceType.None;
                player = PlayerType.None;
                return false;
            }

            // return the details
            piece = Spaces[coord.Row][coord.Column].Type;
            player = Spaces[coord.Row][coord.Column].Player;
            return true;
        }

        public bool TryGetSeam(PlayerType player, out Coordinate coordAfter, out Coordinate coordInc)
        {
            // validate
            if ((int)player <= 0 || (int)player >= Seams.Length || !Seams[(int)player].InUse)
            {
                coordAfter = new Coordinate();
                coordInc = new Coordinate();
                return false;
            }

            // return the details
            coordAfter = Seams[(int)player].CoordAfter;
            coordInc = Seams[(int)player].CoordInc;
            return true;
        }

        #region private
        struct SeamPiece
        {
            public bool InUse;
            public Coordinate CoordAfter;
            public Coordinate CoordInc;

            public SeamPiece()
            {
                InUse = false;
            }
        }

        private Piece[][] Spaces;
        private SeamPiece[] Seams;
        private int[][] PieceCounts;

        private bool TryResolveThreeOfAKind(PlayerType player)
        {
            // look through the Spaces and identify any 3 of a kinds of the same player
            // if any of the pieces in the 3 are Small
            //    return them all to the player as Large
            // if all 3 are Large
            //    the game is over
            if (player == PlayerType.None) return false;

            // look in chunks of 3
            for (int r = 0; r < Spaces.Length; r++)
            {
                for (int c = 0; c < Spaces[r].Length; c++)
                {
                    // down
                    TryResolveThreeOfAKind(player, 
                        new Coordinate() { Row = r, Column = c }, 
                        new Coordinate() { Row = r + 1, Column = c }, 
                        new Coordinate() { Row = r + 2, Column = c });

                    // right
                    TryResolveThreeOfAKind(player,
                        new Coordinate() { Row = r, Column = c }, 
                        new Coordinate() { Row = r, Column = c + 1 },
                        new Coordinate() { Row = r, Column = c + 2 });

                    // down + right
                    TryResolveThreeOfAKind(player, 
                        new Coordinate() { Row = r, Column = c }, 
                        new Coordinate() { Row = r + 1, Column = c + 1 },
                        new Coordinate() { Row = r + 2, Column = c + 2 });

                    // down + left
                    TryResolveThreeOfAKind(player,
                        new Coordinate() { Row = r, Column = c },
                        new Coordinate() { Row = r + 1, Column = c - 1 },
                        new Coordinate() { Row = r + 2, Column = c - 2});
                }
            }

            return true;
        }

        private bool TryResolveThreeOfAKind(PlayerType player, Coordinate coord1, Coordinate coord2, Coordinate coord3)
        {
            if (coord1.Row < 0 || coord1.Row >= Rows ||
                coord1.Column < 0 || coord1.Column >= Columns) return false;
            if (coord2.Row < 0 || coord2.Row >= Rows ||
                coord2.Column < 0 || coord2.Column >= Columns) return false;
            if (coord3.Row < 0 || coord3.Row >= Rows ||
                coord3.Column < 0 || coord3.Column >= Columns) return false;

            // check that they are all the same player
            if (Spaces[coord1.Row][coord1.Column].Player == player &&
                Spaces[coord2.Row][coord2.Column].Player == player &&
                Spaces[coord3.Row][coord3.Column].Player == player)
            {
                // check if any are Small
                if (Spaces[coord1.Row][coord1.Column].Type == PieceType.Small ||
                    Spaces[coord2.Row][coord2.Column].Type == PieceType.Small ||
                    Spaces[coord3.Row][coord3.Column].Type == PieceType.Small)
                {
                    // return them all as Large
                    PieceCounts[(int)player][(int)PieceType.Large] += 3;
                    if (PieceCounts[(int)player][(int)PieceType.Large] > MaxPieceCount) PieceCounts[(int)player][(int)PieceType.Large] = MaxPieceCount;

                    // clear all three spaces
                    Spaces[coord1.Row][coord1.Column].Player = PlayerType.None;
                    Spaces[coord1.Row][coord1.Column].Type = PieceType.None;
                    Spaces[coord2.Row][coord2.Column].Player = PlayerType.None;
                    Spaces[coord2.Row][coord2.Column].Type = PieceType.None;
                    Spaces[coord3.Row][coord3.Column].Player = PlayerType.None;
                    Spaces[coord3.Row][coord3.Column].Type = PieceType.None;
                }
                else
                {
                    // validate
                    if (Spaces[coord1.Row][coord1.Column].Type != PieceType.Large ||
                        Spaces[coord2.Row][coord2.Column].Type != PieceType.Large ||
                        Spaces[coord3.Row][coord3.Column].Type != PieceType.Large) throw new Exception("invalid piece type");

                    // this is a winner
                    Winner = player;
                    if (OnGameWon != null) OnGameWon(player);
                }

                return true;
            }

            return false;
        }

        private bool TryResolveBooop(Coordinate primary, Coordinate secondary)
        {
            // using the following rules, determine if a 'Booop' has occurred
            //  1. small pieces can only move small pieces, but large pieces can move both small and large pieces
            //  2. a piece can be 'Booop'd only if the next space is free or off the board

            // initial validation
            if (primary.Row < 0 || primary.Row >= Rows || primary.Column < 0 || primary.Column >= Columns) throw new Exception("invalid space");
            if (secondary.Row < 0 || secondary.Row >= Rows || secondary.Column < 0 || secondary.Column >= Columns) return false;
            if (Spaces[secondary.Row][secondary.Column].Type == PieceType.None) return false;

            // rule #1
            if (Spaces[primary.Row][primary.Column].Type == PieceType.Small && Spaces[secondary.Row][secondary.Column].Type == PieceType.Large) return false;

            // rule #2
            var destination = new Coordinate()
            {
                Row = secondary.Row + (secondary.Row - primary.Row),
                Column = secondary.Column + (secondary.Column - primary.Column)
            };

            var validMove = false;
            if (destination.Row < 0 || destination.Row >= Rows || destination.Column < 0 || destination.Column >= Columns)
            {
                // this piece is moving off the board - return the piece
                PieceCounts[(int)Spaces[secondary.Row][secondary.Column].Player][(int)Spaces[secondary.Row][secondary.Column].Type]++;
                if (PieceCounts[(int)Spaces[secondary.Row][secondary.Column].Player][(int)Spaces[secondary.Row][secondary.Column].Type] > MaxPieceCount) throw new Exception("invalid piece count");
                validMove = true;
            }
            else if (Spaces[destination.Row][destination.Column].Type == PieceType.None)
            {
                // move the secondary piece to destination
                Spaces[destination.Row][destination.Column].Type = Spaces[secondary.Row][secondary.Column].Type;
                Spaces[destination.Row][destination.Column].Player = Spaces[secondary.Row][secondary.Column].Player;
                validMove = true;
            }

            // clear secondary piece
            if (validMove)
            {
                Spaces[secondary.Row][secondary.Column].Type = PieceType.None;
                Spaces[secondary.Row][secondary.Column].Player = PlayerType.None;
            }

            // 'Booop'd
            return validMove;
        }

        private bool TryResolveSeamBooop(PlayerType player)
        {
            // using the following rules, determine if a 'Booop' has occurred
            //  1. Seam pieces impact the pieces at and after the index (eg. if moving up, then the column at and +1)
            //  2. a piece can always be 'Booop'd and will look for the next open space including off the board

            // initial validation
            if (player == PlayerType.None) return false;
            if ((int)player < 0 || (int)player >= Seams.Length) return false;
            if (!Seams[(int)player].InUse) return false;

            // rule #1 - grab the indices of the impacted spaces
            var coord1 = new Coordinate()
            {
                Row = Seams[(int)player].CoordAfter.Row,
                Column = Seams[(int)player].CoordAfter.Column
            };
            var coord2 = new Coordinate()
            {
                Row = Seams[(int)player].CoordAfter.Row,
                Column = Seams[(int)player].CoordAfter.Column
            };
            // consider the current space as well as the next one (either to the left or below)
            if (Seams[(int)player].CoordInc.Column != 0) coord2.Row += 1;
            else if (Seams[(int)player].CoordInc.Row != 0) coord2.Column += 1;
            else throw new Exception("invalid increment");

            // rule #2
            TrySeemBoop(player, coord1);
            TrySeemBoop(player, coord2);

            // 'Booop'd
            return true;
        }

        private bool TrySeemBoop(PlayerType player, Coordinate coord)
        {
            if (Spaces[coord.Row][coord.Column].Type != PieceType.None)
            {
                // row,col will be 'Booop'd until a free space is found
                var coordNew = new Coordinate()
                {
                    Row = coord.Row,
                    Column = coord.Column
                };
                if (Seams[(int)player].CoordInc.Row != 0)
                {
                    // look left and right (eg. the seam just moved into this row and needs to interact on the right and left)
                    var delta = coord.Column - Seams[(int)player].CoordAfter.Column;
                    if (delta == 0) delta = -1;
                    coordNew.Column += delta;
                    while (coordNew.Column >= 0 && coordNew.Column < Columns && Spaces[coordNew.Row][coordNew.Column].Type != PieceType.None) coordNew.Column += delta;
                }
                else
                {
                    // look up and down (eg. just moved into this column and needs to interact up and down)
                    var delta = coord.Row - Seams[(int)player].CoordAfter.Row;
                    if (delta == 0) delta = -1;
                    coordNew.Row += delta;
                    while (coordNew.Row >= 0 && coordNew.Row < Rows && Spaces[coordNew.Row][coordNew.Column].Type != PieceType.None) coordNew.Row += delta;
                }

                // update new space
                if (coordNew.Row < 0 || coordNew.Row >= Rows ||
                    coordNew.Column < 0 || coordNew.Column >= Columns)
                {
                    // this piece has moved off the board - return it to the owner
                    PieceCounts[(int)Spaces[coord.Row][coord.Column].Player][(int)Spaces[coord.Row][coord.Column].Type]++;
                    if (PieceCounts[(int)Spaces[coord.Row][coord.Column].Player][(int)Spaces[coord.Row][coord.Column].Type] > MaxPieceCount) throw new Exception("invalid piece count");
                }
                else
                {
                    // copy details to this space
                    Spaces[coordNew.Row][coordNew.Column].Type = Spaces[coord.Row][coord.Column].Type;
                    Spaces[coordNew.Row][coordNew.Column].Player = Spaces[coord.Row][coord.Column].Player;
                }

                // clear the piece
                Spaces[coord.Row][coord.Column].Type = PieceType.None;
                Spaces[coord.Row][coord.Column].Player = PlayerType.None;

                return true;
            }

            return false;
        }

        private bool TryGetActivePieceCounts(PlayerType player, out int small, out int large, List<Coordinate> smallCoordinates)
        {
            // iterate through the board and count pieces for this player
            small = 0;
            large = 0;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    if (Spaces[r][c].Player == player)
                    {
                        if (Spaces[r][c].Type == PieceType.Small)
                        {
                            small++;
                            if (smallCoordinates != null) smallCoordinates.Add(new Coordinate() { Row = r, Column = c });
                        }
                        else if (Spaces[r][c].Type == PieceType.Large) large++;
                        else throw new Exception("invalid type");
                    }
                }
            }
            return true;
        }
        #endregion
        }
}
