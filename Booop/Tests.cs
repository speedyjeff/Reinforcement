using System;
using System.Numerics;

// negative tests

namespace Booop
{
    static class Tests
    {
        public static void All()
        {
            // booop in each of the 8 directions
            BooopDirection(new Coordinate() { Row = -1, Column = -1 });
            BooopDirection(new Coordinate() { Row = -1, Column = 0 });
            BooopDirection(new Coordinate() { Row = -1, Column = 1 });
            BooopDirection(new Coordinate() { Row = 0, Column = -1 });
            BooopDirection(new Coordinate() { Row = 0, Column = 1 });
            BooopDirection(new Coordinate() { Row = 1, Column = -1 });
            BooopDirection(new Coordinate() { Row = 1, Column = 0 });
            BooopDirection(new Coordinate() { Row = 1, Column = 1 });

            // putting a piece on every part of the board
            AllCells();

            // off edge of board
            OffEdge(new Coordinate() { Row = 0, Column = 0 });
            OffEdge(new Coordinate() { Row = 0, Column = 3 });
            OffEdge(new Coordinate() { Row = 0, Column = 5 });
            OffEdge(new Coordinate() { Row = 2, Column = 0 });
            OffEdge(new Coordinate() { Row = 4, Column = 5 });
            OffEdge(new Coordinate() { Row = 5, Column = 0 });
            OffEdge(new Coordinate() { Row = 5, Column = 3 });
            OffEdge(new Coordinate() { Row = 5, Column = 5 });

            // barricade - no booop
            HorizontalBarricade();
            VerticalBarricade();
            DiagonalBarricade();

            // small cannot booop large and large can booop both small and large
            LargeBooopSmall();
            SmallNotBooopLarge();

            // resolving a 3
            ThreeInARow(PieceType.Small, PieceType.Small, PieceType.Small, PieceType.Small);
            ThreeInARowDiagonal(PieceType.Small, PieceType.Small, PieceType.Small, PieceType.Small);

            // resolving a mixed 3
            ThreeInARow(PieceType.Small, PieceType.Small, PieceType.Small, PieceType.Large);
            ThreeInARowDiagonal(PieceType.Small, PieceType.Small, PieceType.Small, PieceType.Large);

            // getting a win
            ThreeInARow(PieceType.Large, PieceType.Large, PieceType.Large, PieceType.Large);
            ThreeInARowDiagonal(PieceType.Large, PieceType.Large, PieceType.Large, PieceType.Large);

            // seam placement
            SeamPlacement();

            // Seam booop in each of the 4 directions
            SeamBooopDirection(new Coordinate() { Row = -1, Column = 0 });
            SeamBooopDirection(new Coordinate() { Row = 0, Column = -1 });
            SeamBooopDirection(new Coordinate() { Row = 0, Column = 1 });
            SeamBooopDirection(new Coordinate() { Row = 1, Column = 0 });

            // seam booop to 3
            SeamBooopThreeHorizontal(PieceType.Small);
            SeamBooopThreeVertical(PieceType.Small);

            // seam booop to win
            SeamBooopThreeHorizontal(PieceType.Large);
            SeamBooopThreeVertical(PieceType.Large);

            // seam booop to off edge
            // top left corner
            SeamOffEdge(new Coordinate() { Row = 0, Column = 1 }, new SeamCoordinate() { Direction = Direction.Horizontal, Coordinate = new Coordinate() { Row = 0, Column = 0 } });
            SeamOffEdge(new Coordinate() { Row = 1, Column = 0 }, new SeamCoordinate() { Direction = Direction.Vertical, Coordinate = new Coordinate() { Row = 0, Column = 0 } });

            // top right corner
            SeamOffEdge(new Coordinate() { Row = 0, Column = 4 }, new SeamCoordinate() { Direction = Direction.Horizontal, Coordinate = new Coordinate() { Row = 0, Column = 5 } });
            SeamOffEdge(new Coordinate() { Row = 1, Column = 5 }, new SeamCoordinate() { Direction = Direction.Vertical, Coordinate = new Coordinate() { Row = 0, Column = 4 } });

            // bottom left corner
            SeamOffEdge(new Coordinate() { Row = 5, Column = 1 }, new SeamCoordinate() { Direction = Direction.Horizontal, Coordinate = new Coordinate() { Row = 4, Column = 0 } });
            SeamOffEdge(new Coordinate() { Row = 4, Column = 0 }, new SeamCoordinate() { Direction = Direction.Vertical, Coordinate = new Coordinate() { Row = 5, Column = 0 } });

            // bottom right corner
            SeamOffEdge(new Coordinate() { Row = 5, Column = 4 }, new SeamCoordinate() { Direction = Direction.Horizontal, Coordinate = new Coordinate() { Row = 4, Column = 5 } });
            SeamOffEdge(new Coordinate() { Row = 4, Column = 5 }, new SeamCoordinate() { Direction = Direction.Vertical, Coordinate = new Coordinate() { Row = 5, Column = 4 } });
        }

        public static void BooopDirection(Coordinate inc)
        {
            var rows = 6;
            var columns = 6;
            for(var row = 0+2; row<rows-2; row++)
            {
                for(var col = 0+2; col<columns-2; col++)
                {
                    var board = new Board(rows, columns, initialSmall: Board.MaxPieceCount, initialLarge: 0);
                    var first = new Coordinate() { Row = row, Column = col };
                    var second = new Coordinate() { Row = row + inc.Row, Column = col + inc.Column };
                    var resulting = new Coordinate() { Row = row - inc.Row, Column = col - inc.Column };

                    // place a piece such that a piece placed at (row+inc.Row, column+inc.Column) moves that piece 2*inc
                    if (!board.TryTurn(
                        PlayerType.Orange, 
                        PieceType.Small, 
                        first,
                        NoSeam)) throw new Exception("failed to place initial piece");

                    // validate the board
                    TryValidateBoard(
                        board,
                        new List<Coordinate>()
                        {
                            first
                        });

                    // place a piece to booop this one
                    if (!board.TryTurn(
                        PlayerType.Orange,
                        PieceType.Small,
                        second,
                        NoSeam)) throw new Exception("failed to place initial piece");

                    // validate the board
                    TryValidateBoard(
                        board,
                        new List<Coordinate>()
                        {
                            second,
                            resulting
                        });
                }
            }
        }

        public static void AllCells()
        {
            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: 0);
            for (var row = 0; row < board.Rows; row++)
            {
                for (var col = 0; col < board.Columns; col++)
                {
                    // check if there was a win
                    if (board.Winner != PlayerType.None)
                    {
                        // reset
                        board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: 0);
                    }

                    // get piece counts
                    if (!board.TryGetAvailablePieces(PlayerType.Orange, out int smallCount, out int largeCount, out bool canUseSeam)) throw new Exception("failed to get piece counts");

                    // place the piece
                    if (!board.TryTurn(
                        PlayerType.Orange,
                        smallCount > 0 ? PieceType.Small : PieceType.Large,
                        new Coordinate() { Row = row, Column = col},
                        NoSeam)) throw new Exception("failed to place initial piece");
                }
            }
        }

        public static void OffEdge(Coordinate primary)
        {
            // ensure that pieces fall off edge when boooped
            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: 0);
            
            // check piece counts
            if (!board.TryGetAvailablePieces(PlayerType.Orange, out int smallCount, out int largeCount, out bool canUseSeam)) throw new Exception("failed to get piece counts");
            if (smallCount != Board.MaxPieceCount || largeCount != 0) throw new Exception("invalid piece counts");

            // put the piece
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Small,
                primary,
                NoSeam)) throw new Exception("failed to place initial piece");

            // check piece count
            if (!board.TryGetAvailablePieces(PlayerType.Orange, out smallCount, out largeCount, out canUseSeam)) throw new Exception("failed to get piece counts");
            if (smallCount != (Board.MaxPieceCount-1) || largeCount != 0) throw new Exception("invalid piece counts");

            // validate
            TryValidateBoard(
                board,
                new List<Coordinate>()
                {
                    primary
                });

            // put a piece such that it will cause the first piece to fall off the edge
            var secondary = new Coordinate() { Row = primary.Row + 1, Column = primary.Column + 1 };
            if (primary.Row == board.Rows-1) secondary.Row = primary.Row - 1;
            if (primary.Column == board.Columns-1) secondary.Column = primary.Column - 1;
            if (!board.TryTurn(
                    PlayerType.Orange,
                PieceType.Small,
                secondary,
                NoSeam)) throw new Exception("failed to place initial piece");

            // check piece count
            if (!board.TryGetAvailablePieces(PlayerType.Orange, out smallCount, out largeCount, out canUseSeam)) throw new Exception("failed to get piece counts");
            if (smallCount != (Board.MaxPieceCount - 1) || largeCount != 0) throw new Exception("invalid piece counts");

            // validate
            TryValidateBoard(
                board,
                new List<Coordinate>()
                {
                    secondary
                });
        }

        public static void HorizontalBarricade()
        {
            // put pieces such that they make a barricade
            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: 0);
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Small,
                new Coordinate() { Row = 2, Column = 2 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Small,
                new Coordinate() { Row = 2, Column = 3 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Small,
                new Coordinate() { Row = 2, Column = 4 },
                NoSeam)) throw new Exception("failed to place initial piece");

            // validate
            TryValidateBoard(
                board,
                new List<Coordinate>()
                {
                    new Coordinate() { Row = 2, Column = 1 },
                    new Coordinate() { Row = 2, Column = 2 },
                    new Coordinate() { Row = 2, Column = 4 }
                });
        }

        public static void VerticalBarricade()
        {
            // put pieces such that they make a vertical barricade
            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: 0);
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Small,
                new Coordinate() { Row = 2, Column = 2 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Small,
                new Coordinate() { Row = 3, Column = 2 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Small,
                new Coordinate() { Row = 4, Column = 2 },
                NoSeam)) throw new Exception("failed to place initial piece");

            // validate
            TryValidateBoard(
                board,
                new List<Coordinate>()
                {
                    new Coordinate() { Row = 1, Column = 2 },
                    new Coordinate() { Row = 2, Column = 2 },
                    new Coordinate() { Row = 4, Column = 2 }
                });
        }

        public static void DiagonalBarricade()
        {
            // put pieces such that they make a diagonal barricade
            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: 0);
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Small,
                new Coordinate() { Row = 2, Column = 2 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                    PlayerType.Orange,
                PieceType.Small,
                new Coordinate() { Row = 3, Column = 3 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                    PlayerType.Orange,
                    PieceType.Small,
                    new Coordinate() { Row = 4, Column = 4 },
                    NoSeam)) throw new Exception("failed to place initial piece");

            // validate
            TryValidateBoard(
                board,
                new List<Coordinate>()
                {
                    new Coordinate() { Row = 1, Column = 1 },
                    new Coordinate() { Row = 2, Column = 2 },
                    new Coordinate() { Row = 4, Column = 4 }
                });
        }

        public static void LargeBooopSmall()
        {
            // put a large piece next to a small piece
            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: 1);
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Small,
                new Coordinate() { Row = 2, Column = 2 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Large,
                new Coordinate() { Row = 2, Column = 3 },
                NoSeam)) throw new Exception("failed to place initial piece");

            // validate
            TryValidateBoard(
                board,
                new List<Coordinate>()
                {
                    new Coordinate() { Row = 2, Column = 1 },
                    new Coordinate() { Row = 2, Column = 3 }
                });
        }

        public static void SmallNotBooopLarge()
        {
            // put a small next to a large
            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: 1);
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Large,
                new Coordinate() { Row = 2, Column = 2 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                    PlayerType.Orange,
                PieceType.Small,
                new Coordinate() { Row = 2, Column = 3 },
                NoSeam)) throw new Exception("failed to place initial piece");

            // validate
            TryValidateBoard(
                board,
                new List<Coordinate>()
                {
                    new Coordinate() { Row = 2, Column = 2 },
                    new Coordinate() { Row = 2, Column = 3 }
                });
        }

        public static void ThreeInARow(PieceType p1, PieceType p2, PieceType p3, PieceType p4)
        {
            // count what pieces we are adding
            var smallAdded = 0;
            var largeAdded = 0;
            if (p1 == PieceType.Small) { smallAdded++; } else { largeAdded++; }
            if (p2 == PieceType.Small) { smallAdded++; } else { largeAdded++; }
            if (p3 == PieceType.Small) { smallAdded++; } else { largeAdded++; }
            if (p4 == PieceType.Small) { smallAdded++; } else { largeAdded++; }

            // put pieces such that they make a 3 in a row
            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: largeAdded);

            // check piece count
            if (!board.TryGetAvailablePieces(PlayerType.Orange, out int smallCount, out int largeCount, out bool canUseSeam)) throw new Exception("failed to get piece counts");
            if (smallCount != Board.MaxPieceCount || largeCount != largeAdded) throw new Exception("invalid piece counts");

            // check if win
            var win = false;
            board.OnGameWon += (player) => win = true;

            // setup the board
            if (!board.TryTurn(
                PlayerType.Orange,
                p1,
                new Coordinate() { Row = 2, Column = 2 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                PlayerType.Orange,
                p2,
                new Coordinate() { Row = 2, Column = 3 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                PlayerType.Orange,
                p3,
                new Coordinate() { Row = 2, Column = 4 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                PlayerType.Orange,
                p4, 
                new Coordinate() { Row = 2, Column = 3 },
                NoSeam)) throw new Exception("failed to place initial piece");

            // validate
            if (p1 == PieceType.Small)
            {
                // validate
                TryValidateBoard(
                    board,
                    new List<Coordinate>()
                    {
                    new Coordinate() { Row = 2, Column = 5 }
                    });

                // check piece count
                if (!board.TryGetAvailablePieces(PlayerType.Orange, out smallCount, out largeCount, out canUseSeam)) throw new Exception("failed to get piece counts");
                if (smallCount != (Board.MaxPieceCount - smallAdded) || largeCount != 3) throw new Exception("invalid piece counts");

                if (win) throw new Exception("should not be a win");
            }
            else if (p1 == PieceType.Large)
            {
                // validate
                TryValidateBoard(
                    board,
                    new List<Coordinate>()
                    {
                    new Coordinate() { Row = 2, Column = 1 },
                    new Coordinate() { Row = 2, Column = 2 },
                    new Coordinate() { Row = 2, Column = 3 },
                    new Coordinate() { Row = 2, Column = 5 },
                    });

                if (!board.TryGetAvailablePieces(PlayerType.Orange, out smallCount, out largeCount, out canUseSeam)) throw new Exception("failed to get piece counts");
                if (smallCount != Board.MaxPieceCount || largeCount != 0) throw new Exception("invalid piece counts");
                if (!win) throw new Exception("should be a win");
            }
            else
            {
                throw new Exception("invalid piece type");
            }
        }

        public static void ThreeInARowDiagonal(PieceType p1, PieceType p2, PieceType p3, PieceType p4)
        {
            // count what pieces we are adding
            var smallAdded = 0;
            var largeAdded = 0;
            if (p1 == PieceType.Small) { smallAdded++; } else { largeAdded++; }
            if (p2 == PieceType.Small) { smallAdded++; } else { largeAdded++; }
            if (p3 == PieceType.Small) { smallAdded++; } else { largeAdded++; }
            if (p4 == PieceType.Small) { smallAdded++; } else { largeAdded++; }

            // put pieces such that they make a 3 in a row
            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: largeAdded);

            // check piece count
            if (!board.TryGetAvailablePieces(PlayerType.Orange, out int smallCount, out int largeCount, out bool canUseSeam)) throw new Exception("failed to get piece counts");
            if (smallCount != Board.MaxPieceCount || largeCount != largeAdded) throw new Exception("invalid piece counts");

            // check if win
            var win = false;
            board.OnGameWon += (player) => win = true;

            // setup the board
            if (!board.TryTurn(
                PlayerType.Orange,
                p1,
                new Coordinate() { Row = 2, Column = 2 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                PlayerType.Orange,
                p2,
                new Coordinate() { Row = 3, Column = 1 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                PlayerType.Orange,
                p3,
                new Coordinate() { Row = 0, Column = 4 },
                NoSeam)) throw new Exception("failed to place initial piece");
            if (!board.TryTurn(
                PlayerType.Orange,
                p4,
                new Coordinate() { Row = 1, Column = 3 },
                NoSeam)) throw new Exception("failed to place initial piece");

            // validate
            if (p1 == PieceType.Small)
            {
                // validate
                TryValidateBoard(
                    board,
                    new List<Coordinate>());

                // check piece count
                if (!board.TryGetAvailablePieces(PlayerType.Orange, out smallCount, out largeCount, out canUseSeam)) throw new Exception("failed to get piece counts");
                if (smallCount != (Board.MaxPieceCount - smallAdded + 1 /* off board */) || largeCount != 3) throw new Exception("invalid piece counts");
                if (win) throw new Exception("should not be a win");
            }
            else if (p1 == PieceType.Large)
            {
                // validate
                TryValidateBoard(
                    board,
                    new List<Coordinate>()
                    {
                        new Coordinate() { Row = 3, Column = 1 },
                        new Coordinate() { Row = 2, Column = 2 },
                        new Coordinate() { Row = 1, Column = 3 }
                    });

                // check piece count
                if (!board.TryGetAvailablePieces(PlayerType.Orange, out smallCount, out largeCount, out canUseSeam)) throw new Exception("failed to get piece counts");
                if (smallCount != Board.MaxPieceCount || largeCount != 1 /* one off the board*/) throw new Exception("invalid piece counts");
                if (!win) throw new Exception("should be a win");
            }
            else
            {
                throw new Exception("invalid piece type");
            }
        }

        public static void SeamPlacement()
        {
            // place all the possible seams
            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: 0);
            if (!board.TryGetAvailableSeams(PlayerType.Orange, out List<SeamCoordinate> availableSeams)) throw new Exception("failed to get available seams");

            // check piece count
            if (!board.TryGetAvailablePieces(PlayerType.Orange, out int smallCount, out int largeCount, out bool canUseSeam)) throw new Exception("failed to get piece counts");
            if (smallCount != Board.MaxPieceCount || largeCount != 0) throw new Exception("invalid piece counts");

            // place pieces and ensure that each of the seams run their course
            foreach (var seam in availableSeams)
            {
                // get a new board
                board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: 0);

                for (int i = 0; i < board.Rows + 1; i++)
                {
                    // check if we can use the seam                    
                    if (i > 0 && board.TryGetAvailableSeams(PlayerType.Orange, out List<SeamCoordinate> innerSeams)) throw new Exception("seam returned early");

                    // place the first available
                    if (!board.TryGetAvailableMoves(PlayerType.Orange, out List<Coordinate> moves)) throw new Exception("failed to get moves");

                    if (!board.TryTurn(
                        PlayerType.Orange,
                        PieceType.Small,
                        moves[0],
                        i == 0 ? seam : new SeamCoordinate())) throw new Exception("failed to place initial piece");
                }

                // ensure the seam can be used
                if (!board.TryGetAvailableSeams(PlayerType.Orange, out List<SeamCoordinate> seams)) throw new Exception("seam returned early");

            }
        }

        public static void SeamBooopDirection(Coordinate inc)
        {
            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: 0);
            var seam = new SeamCoordinate();
            var first = new Coordinate();
            var second = new Coordinate() { Row = 0, Column = 2 };

            if (Math.Abs(inc.Row) + Math.Abs(inc.Column) != 1) throw new Exception("invalid direction");

            // determine the seam and resulting piece placement after boooping
            if (inc.Row < 0)
            {
                // booop up
                seam.Direction = Direction.Horizontal;
                seam.Coordinate = new Coordinate() { Row = 4, Column = 0 };
                first = new Coordinate() { Row = 4, Column = 1 };
            }
            else if (inc.Row > 0)
            {
                // booop down
                seam.Direction = Direction.Horizontal;
                seam.Coordinate = new Coordinate() { Row = 0, Column = 0 };
                first = new Coordinate() { Row = 1, Column = 1 };
                second = new Coordinate() { Row = 1, Column = 4 };
            }
            else if (inc.Column < 0)
            {
                // booop left
                seam.Direction = Direction.Vertical;
                seam.Coordinate = new Coordinate() { Row = 0, Column = 4 };
                first = new Coordinate() { Row = 1, Column = 4 };
            }
            else if (inc.Column > 0)
            {
                // booop right
                seam.Direction = Direction.Vertical;
                seam.Coordinate = new Coordinate() { Row = 0, Column = 0 };
                first = new Coordinate() { Row = 1, Column = 1 };
                second = new Coordinate() { Row = 4, Column = 2 };
            }
            else throw new Exception("invalid direction");

            var resulting = new Coordinate() { Row = first.Row + inc.Row, Column = first.Column + inc.Column };

            // place a piece such that the seam will move it to row+inc.Row, column+inc.Column
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Small,
                first,
                seam)) throw new Exception("failed to place initial piece");

            // validate that there is not an available seam
            if (board.TryGetAvailableSeams(PlayerType.Orange, out List<SeamCoordinate> seams)) throw new Exception("failed to get available seams");

            // validate the board
            TryValidateBoard(
                board,
                new List<Coordinate>()
                {
                    first
                });

            // place a second piece (does not matter where) such that the seam advances and booops the first
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Small,
                second,
                NoSeam)) throw new Exception("failed to place initial piece");

            // validate the board
            TryValidateBoard(
                board,
                new List<Coordinate>()
                {
                    resulting,
                    second
                });
        }

        
        public static void SeamBooopThreeHorizontal(PieceType piece)
        {
            var start = new Coordinate[]
                {
                new Coordinate() { Row = 2, Column = 2 },
                new Coordinate() { Row = 2, Column = 4 },
                new Coordinate() { Row = 2, Column = 5 }
                };
            var end = new Coordinate[0];
            var seam = new SeamCoordinate()
            {
                Direction = Direction.Vertical,
                Coordinate = new Coordinate() { Row = 0, Column = 1 }
            };

            if (piece == PieceType.Large)
            {
                end = new Coordinate[]
                    {
                    new Coordinate() { Row = 2, Column = 3 },
                    new Coordinate() { Row = 2, Column = 4 },
                    new Coordinate() { Row = 2, Column = 5 }
                };
            }

            SeamBooopThree(piece, seam, start, end);
        }

        public static void SeamBooopThreeVertical(PieceType piece)
        {
            var start = new Coordinate[]
                {
                new Coordinate() { Row = 2, Column = 2 },
                new Coordinate() { Row = 4, Column = 2 },
                new Coordinate() { Row = 5, Column = 2 }
                };
            var end = new Coordinate[0];
            var seam = new SeamCoordinate()
            {
                Direction = Direction.Horizontal,
                Coordinate = new Coordinate() { Row = 1, Column = 0 }
            };

            if (piece == PieceType.Large)
            {
                end = new Coordinate[]
                    {
                    new Coordinate() { Row = 3, Column = 2 },
                    new Coordinate() { Row = 4, Column = 2 },
                    new Coordinate() { Row = 5, Column = 2 }
                };
            }

            SeamBooopThree(piece, seam, start, end);
        }

        public static void SeamBooopThree(PieceType piece, SeamCoordinate seam, Coordinate[] start, Coordinate[] end)
        {
            if (start == null || start.Length != 3) throw new Exception("invalid start");
            if (end == null) throw new Exception("invalid end");

            // get piece counts
            var smallAdded = (piece == PieceType.Small) ? start.Length : 0;
            var largeAdded = (piece == PieceType.Large) ? start.Length : 0;

            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: largeAdded);

            // check piece count
            if (!board.TryGetAvailablePieces(PlayerType.Orange, out int smallCount, out int largeCount, out bool canUseSeam)) throw new Exception("failed to get piece counts");
            if (smallCount != Board.MaxPieceCount || largeCount != largeAdded) throw new Exception("invalid piece counts");

            // setup callbacks
            board.OnGameWon += (player) =>
            {
                if (piece != PieceType.Large) throw new Exception("should not win");
            };

            // place pieces such that it will be boooped to a 3 by a seam
            for (int i = 0; i < start.Length; i++)
            {
                if (!board.TryTurn(
                    PlayerType.Orange,
                    piece,
                    start[i],
                    i == 0 ? seam : new SeamCoordinate()
                    )) throw new Exception("failed to place initial piece");
            }

            // check piece count
            if (!board.TryGetAvailablePieces(PlayerType.Orange, out smallCount, out largeCount, out canUseSeam)) throw new Exception("failed to get piece counts");
            if (piece == PieceType.Small && largeCount != 3) throw new Exception("invalid large count");
            else if (piece == PieceType.Large && largeCount != 0) throw new Exception("invalid large count");
            if (smallCount != (Board.MaxPieceCount - smallAdded)) throw new Exception("invalid piece counts");

            // validate the board
            var locations = new List<Coordinate>();
            if (end.Length > 0)
            {
                foreach (var coord in end)
                {
                    locations.Add(coord);
                }
            }
            TryValidateBoard(
                board,
                locations);
        }

        public static void SeamOffEdge(Coordinate coord, SeamCoordinate seam)
        {
            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: 0);

            // check piece count
            if (!board.TryGetAvailablePieces(PlayerType.Orange, out int smallCount, out int largeCount, out bool canUseSeam)) throw new Exception("failed to get piece counts");
            if (smallCount != Board.MaxPieceCount || largeCount != 0) throw new Exception("invalid piece counts");

            // put the piece along with the seam
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Small,
                coord,
                seam)) throw new Exception("failed to place initial piece");

            // add a random piece which will advance the seam
            if (!board.TryTurn(
                PlayerType.Orange,
                PieceType.Small,
                new Coordinate() { Row = 2, Column = 2 },
                NoSeam)) throw new Exception("failed to place initial piece");

            // check piece count
            if (!board.TryGetAvailablePieces(PlayerType.Orange, out smallCount, out largeCount, out canUseSeam)) throw new Exception("failed to get piece counts");
            if (smallCount != (Board.MaxPieceCount - 1) || largeCount != 0) throw new Exception("invalid piece counts");

            // validate board
            TryValidateBoard(
                board,
                new List<Coordinate>()
                {
                    new Coordinate() { Row = 2, Column = 2}
                });
        }

        #region private
        private static SeamCoordinate NoSeam = new SeamCoordinate();
        
        private static bool TryValidateBoard(Board board, List<Coordinate> present)
        {
            // ensure that the present coordinates are present
            foreach (var coord in present)
            {
                if (!board.TryGetCell(
                        coord,
                        out PieceType piece,
                        out PlayerType player)) throw new Exception("failed to get piece");
                if (piece == PieceType.None || player == PlayerType.None) throw new Exception("invalid piece details");
            }

            // count how many cells are populated and verify that it matches the present count
            var count = 0;
            for (var row = 0; row < board.Rows; row++)
            {
                for (var col = 0; col < board.Columns; col++)
                {
                    if (!board.TryGetCell(
                        new Coordinate() { Row = row, Column = col },
                        out PieceType piece,
                        out PlayerType player)) throw new Exception("failed to get piece");
                    
                    if (player != PlayerType.None || piece != PieceType.None)
                    {
                        count++;
                    }
                }
            }

            // check the count
            if (count != present.Count) throw new Exception("invalid piece count");

            return true;
        }
        #endregion
    }
}
