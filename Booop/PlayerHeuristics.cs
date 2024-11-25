using System;
using System.ComponentModel;

// todo - change the code to adjust the local copy of board and then check for situations (this accomodates multiple pieces moving in 1 move)

namespace Booop
{
    class PlayerHeuristics : PlayerBase
    {
        public PlayerHeuristics(PlayerType player, bool verbose, bool deterministic) : base(player, verbose)
        {
            Rand = new Random();
            Deterministic = deterministic;
        }

        public bool Deterministic { get; private set; }

        public override bool TryMakeMove(Board board)
        {
            // todo seam

            // ensure proper board size
            if (board.Rows != 6 || board.Columns != 6) throw new Exception("unsupported board size");

            // get moves
            if (!board.TryGetAvailableMoves(Player, out List<Coordinate> moves)) throw new Exception("failed to get moves");

            // apply a set of heuristics to determine the best move
            if (!board.TryGetAvailablePieces(Player, out int small, out int large, out bool seam)) throw new Exception("failed to get piece counts");

            // make boards for each of the moves and compare across a set of criteria on which is ideal
            var speculativeMoves = new List<SpeculativeMove>();
            foreach(var m in moves)
            {
                // small piece
                if (small > 0)
                {
                    var smove = new SpeculativeMove();
                    smove.Board = new Board(board);
                    smove.Move = m;
                    smove.Piece = PieceType.Small;
                    if (!smove.Board.TryTurn(Player, smove.Piece, smove.Move, new SeamCoordinate())) throw new Exception("failed to make a move");
                    speculativeMoves.Add(smove);
                }

                // large piece
                if (large > 0)
                {
                    var smove = new SpeculativeMove();
                    smove.Board = new Board(board);
                    smove.Move = m;
                    smove.Piece = PieceType.Large;
                    if (!smove.Board.TryTurn(Player, smove.Piece, smove.Move, new SeamCoordinate())) throw new Exception("failed to make a move");
                    speculativeMoves.Add(smove);
                }
            }

            // shuffle the moves (to avoid biasing always in the upper left corner)
            if (!Deterministic)
            {
                var moveArray = speculativeMoves.ToArray();
                for (int i = 0; i < moveArray.Length; i++)
                {
                    var temp = moveArray[i];
                    var index = i;
                    do
                    {
                        index = Rand.Next() % moveArray.Length;
                    }
                    while (index == i);
                    moveArray[i] = moveArray[index];
                    moveArray[index] = temp;
                }
                speculativeMoves = moveArray.ToList();
            }

            // run the heuristics
            if (TryHeuristics(board, speculativeMoves, withPruning: true, out Coordinate move, out PieceType piece))
            {
                if (!board.TryTurn(Player, piece, move, new SeamCoordinate())) throw new Exception("failed to make a move");
                return true;
            }

            // run the heuristics without pruning (eg. pick the best of the worst)
            if (TryHeuristics(board, speculativeMoves, withPruning: false, out move, out piece))
            {
                if (!board.TryTurn(Player, piece, move, new SeamCoordinate())) throw new Exception("failed to make a move");
                return true;
            }

            // just choose one
            //if (speculativeMoves.Count > 0)
            //{
            //    var smove = speculativeMoves[Rand.Next() % speculativeMoves.Count];
            //    if (!board.TryTurn(Player, smove.Piece, smove.Move, new SeamCoordinate())) throw new Exception("failed to make a move");
            //    if (Verbose) Console.WriteLine($"random move {smove.Move.Row},{smove.Move.Column}");
            //    return true;
            //}

            // in theory the above rules should be sufficient
            Console.WriteLine($"playing as {Player}");
            Utility.Print(board);
            throw new Exception("failed to make a move");
        }

        public override Coordinate ChooseUpgradePiece(Board board, List<Coordinate> coords)
        {
            // corner case
            if (coords.Count == 1) return coords[0];

            // randomize the incoming (to avoid biasing always in the upper left corner)
            if (!Deterministic)
            {
                var coordArray = coords.ToArray();
                for (int i = 0; i < coordArray.Length; i++)
                {
                    var temp = coordArray[i];
                    var index = i;
                    do
                    {
                        index = Rand.Next() % coordArray.Length;
                    }
                    while (index == i);
                    coordArray[i] = coordArray[index];
                    coordArray[index] = temp;
                }
                coords = coordArray.ToList();
            }

            // apply a set of heuristics to determine the best upgrade
            //  1. pick a piece that is in the outer ring
            foreach (var coord in coords)
            {
                if (IsOuterRing(coord)) return coord;
            }

            //  2. never pick a piece that is part of a 2
            foreach(var coord in coords)
            {
                // count how many neighbors match (eg. a 2 in a row)
                var matchNeighborCnt = 0;
                foreach (var delta in AllNeighbors)
                {
                    var row = coord.Row + delta.Row;
                    var col = coord.Column + delta.Column;

                    // check that the next one is the same player
                    if (row >= 0 && row < board.Rows &&
                            col >= 0 && col < board.Columns)
                    {
                        if (board.TryGetCell(new Coordinate() { Row = row, Column = col }, out PieceType piece, out PlayerType p) && p == Player)
                        {
                            // this has a neighbor of this type
                            matchNeighborCnt++;
                        }
                    }
                }

                // check that there are no matching neighbors
                if (matchNeighborCnt == 0) return coord;
            }

            //  just pick one
            if (Deterministic) return coords[0];
            else return coords[Rand.Next() % coords.Count];

            //  this may be enough, as this means that 8 pieces are on the board and likely in one of these cases
            //Console.WriteLine($"playing as {Player}");
            //Utility.Print(board);
            //throw new Exception("not enough heuristics to determine upgrade piece");
        }

        #region private
        private class SpeculativeMove
        {
            public Coordinate Move;
            public PieceType Piece;
            public Board Board;
        }
        private static readonly Coordinate[] AllNeighbors = new Coordinate[]
        {
            new Coordinate() { Row = -1, Column = -1},
            new Coordinate() { Row = -1, Column = 0},
            new Coordinate() { Row = -1, Column = 1},
            new Coordinate() { Row = 0, Column = -1},
            new Coordinate() { Row = 0, Column = 1},
            new Coordinate() { Row = 1, Column = -1},
            new Coordinate() { Row = 1, Column = 0},
            new Coordinate() { Row = 1, Column = 1}
        };
        private static readonly Coordinate[] LeadingEdges = new Coordinate[]
        {
            new Coordinate() { Row = 0, Column = 1},
            new Coordinate() { Row = 1, Column = -1},
            new Coordinate() { Row = 1, Column = 0},
            new Coordinate() { Row = 1, Column = 1}
        };
        private Random Rand;

        private bool TryHeuristics(Board board, List<SpeculativeMove> speculativeMoves, bool withPruning, out Coordinate move, out PieceType piece)
        {
            // get the opponent
            var opponent = Player == PlayerType.Orange ? PlayerType.Purple : PlayerType.Orange;

            //  0. if you are a winner, make the winning move
            if (IsWinner(board, speculativeMoves, Player, out SpeculativeMove smove))
            {
                move = smove.Move;
                piece = smove.Piece;
                if (Verbose) Console.WriteLine($"winning move {smove.Move.Row},{smove.Move.Column}");
                return true;
            }

            //  1. avoid moves where the opponent is the winner
            if (withPruning)
            {
                speculativeMoves = FilterOutWins(board, speculativeMoves, opponent);
            }

            //  2. avoid moves that give the opponent a 3 in a row
            if (withPruning)
            {
                speculativeMoves = FilterOut3InARow(board, speculativeMoves, opponent);
            }

            //  3. block a 2 in a row for the opponent
            if (IsBlocking2InARow(board, speculativeMoves, opponent, out smove))
            {
                move = smove.Move;
                piece = smove.Piece;
                if (Verbose) Console.WriteLine($"block 2 in a row move {smove.Move.Row},{smove.Move.Column}");
                return true;
            }

            //  4. if you can make a 3 in a row
            if (Is3InARow(board, speculativeMoves, Player, out smove))
            {
                move = smove.Move;
                piece = smove.Piece;
                if (Verbose) Console.WriteLine($"3 in a row move {smove.Move.Row},{smove.Move.Column}");
                return true;
            }

            //  5. avoid moves that give the opponent a 2 in a row
            if (withPruning)
            {
                speculativeMoves = FilterOut2InARow(board, speculativeMoves, opponent);
            }

            //  6. if you can make a 2 in a row
            if (Is2InARow(board, speculativeMoves, Player, out smove))
            {
                move = smove.Move;
                piece = smove.Piece;
                if (Verbose) Console.WriteLine($"2 in a row move {smove.Move.Row},{smove.Move.Column}");
                return true;
            }

            //  7. avoid putting pieces in the outer ring
            // WARNING: this rule ended up training the AI to only play in the outer ring
            //if (withPruning)
            //{
            //    speculativeMoves = FilterOutOuterRings(board, speculativeMoves, Player);
            //}

            //  8. increase pieces in the center 4 spaces
            if (IsMoreInCenterRing(board, speculativeMoves, Player, out smove))
            {
                move = smove.Move;
                piece = smove.Piece;
                if (Verbose) Console.WriteLine($"more in center ring move {smove.Move.Row},{smove.Move.Column}");
                return true;
            }

            //  9. choose one in the center ring
            if (IsCenterRing(board, speculativeMoves, out smove))
            {
                move = smove.Move;
                piece = smove.Piece;
                if (Verbose) Console.WriteLine($"is center ring move {smove.Move.Row},{smove.Move.Column}");
                return true;
            }

            // could not find a move
            move = new Coordinate();
            piece = PieceType.Small;
            return false;
        }

        private bool IsWinner(Board board, List<SpeculativeMove> smoves, PlayerType player, out SpeculativeMove move)
        {
            // look for moves that result in a win for player
            foreach (var m in smoves)
            {
                if (m.Board.Winner == player)
                {
                    move = m;
                    return true;
                }
            }

            // no winner
            move = null;
            return false;
        }

        private List<SpeculativeMove> FilterOutWins(Board board, List<SpeculativeMove> smoves, PlayerType player)
        {
            // keep only moves that do not result in a win for player
            var filtered = new List<SpeculativeMove>();
            foreach (var m in smoves)
            {
                if (m.Board.Winner != player)
                {
                    filtered.Add(m);
                }
            }
            return filtered;
        }

        private List<SpeculativeMove> FilterOut3InARow(Board board, List<SpeculativeMove> smoves, PlayerType player)
        {
            // get initial piece counts
            if (!board.TryGetAvailablePieces(player, out int small, out int large, out bool seam)) throw new Exception("failed to get piece counts");

            // keep only moves that do not result in an increase in large pieces for player
            var filtered = new List<SpeculativeMove>();
            foreach (var m in smoves)
            {
                // get the piece count for this player
                if (!m.Board.TryGetAvailablePieces(player, out int tmpSmall, out int tmpLarge, out bool tmpSeam)) throw new Exception("failed to get piece counts");

                // keep moves only if small pieces were not converted to large (eg. a 3 in a row occurred)
                if (tmpSmall >= small && tmpLarge >= large)
                {
                    filtered.Add(m);
                }
            }
            return filtered;
        }

        private bool Is3InARow(Board board, List<SpeculativeMove> smoves, PlayerType player, out SpeculativeMove move)
        {
            // get initial piece counts
            if (!board.TryGetAvailablePieces(player, out int small, out int large, out bool seam)) throw new Exception("failed to get piece counts");

            // find moves that result in an increase in large pieces for player
            foreach (var m in smoves)
            {
                // get the piece count for this player
                if (!m.Board.TryGetAvailablePieces(player, out int tmpSmall, out int tmpLarge, out bool tmpSeam)) throw new Exception("failed to get piece counts");

                // find a move where small pieces were converted to large (eg. a 3 in a row occurred)
                if (tmpSmall <= small && tmpLarge > large)
                {
                    move = m;
                    return true;
                }
            }

            // no 3 in a rows
            move = null;
            return false;
        }

        private List<SpeculativeMove> FilterOut2InARow(Board board, List<SpeculativeMove> smoves, PlayerType player)
        {
            // count how many 2 in a rows exist in the current board
            var count = Count2InARows(board, player);

            // keep only moves that do not result in a 2 in a row for player
            var filtered = new List<SpeculativeMove>();
            foreach (var m in smoves)
            {
                // check how many 2 in a rows exist for this speculative move
                var tmpCount = Count2InARows(m.Board, player);

                // keep moves only if the count of 2 in a rows did not increase
                if (tmpCount == 0 || tmpCount < count)
                {
                    filtered.Add(m);
                }
            }
            return filtered;
        }

        private bool Is2InARow(Board board, List<SpeculativeMove> smoves, PlayerType player, out SpeculativeMove move)
        {
            // count how many 2 in a rows exist in the current board
            var count = Count2InARows(board, player);

            // find moves that result in an increase in 2 in a rows for player
            foreach (var m in smoves)
            {
                // check how many 2 in a rows exist for this speculative move
                var tmpCount = Count2InARows(m.Board, player);

                // find a move where the count of 2 in a rows increased
                if (tmpCount > count)
                {
                    move = m;
                    return true;
                }
            }

            // no 2 in a rows
            move = null;
            return false;
        }

        private bool IsBlocking2InARow(Board board, List<SpeculativeMove> smoves, PlayerType player, out SpeculativeMove move)
        {
            // count how many 2 in a rows exist in the current board
            var count = Count2InARows(board, player);

            // find moves that result in a decrease in 2 in a rows for player
            foreach (var m in smoves)
            {
                // check how many 2 in a rows exist for this speculative move
                var tmpCount = Count2InARows(m.Board, player);

                // find a move where the count of 2 in a rows decreases
                if (tmpCount < count)
                {
                    move = m;
                    return true;
                }
            }

            // no decrease in 2 in a rows
            move = null;
            return false;
        }

        private bool IsMoreInCenterRing(Board board, List<SpeculativeMove> smoves, PlayerType player, out SpeculativeMove move)
        {
            // count how many pieces are in the center ring
            var count = CountInCenterRing(board, player);

            // find moves that result in an increase in pieces in the center ring for player
            foreach (var m in smoves)
            {
                // check how many pieces are in the center ring for this speculative move
                var tmpCount = CountInCenterRing(m.Board, player);

                // find a move where the count of pieces in the center ring increased
                if (tmpCount > count)
                {
                    move = m;
                    return true;
                }
            }

            // no increase in pieces in the center ring
            move = null;
            return false;
        }

        /*
        // filtering out outer ring, trains the AI to play only in the outer ring
        private List<SpeculativeMove> FilterOutOuterRings(Board board, List<SpeculativeMove> smoves, PlayerType player)
        {
            // count how many pieces are in the outer ring
            var count = CountInOuterRing(board, player);

            // keep only moves that do not result in a piece in the outer ring for player
            var filtered = new List<SpeculativeMove>();
            foreach (var m in smoves)
            {
                var tmpCount = CountInOuterRing(m.Board, player);
                if (tmpCount <= count)
                {
                    filtered.Add(m);
                }
            }
            return filtered;
        }
        */

        private bool IsCenterRing(Board board, List<SpeculativeMove> smoves, out SpeculativeMove move)
        {
            // find moves that result in a piece in the center ring
            foreach (var m in smoves)
            {
                if (IsCenterRing(m.Move))
                {
                    move = m;
                    return true;
                }
            }

            // no center ring moves
            move = null;
            return false;
        }

        private int Count2InARows(Board board, PlayerType player)
        {
            // identify 2 in a rows for this player
            var count = 0;
            for (int row = 0; row < board.Rows-1; row++)
            {
                for (int col = 0; col < board.Columns-1; col++)
                {
                    // only consider player pieces
                    if (board.TryGetCell(
                        new Coordinate() { Row = row, Column = col }, 
                        out PieceType piece, 
                        out PlayerType p) && p == player)
                    {
                        // check directions in front and under
                        foreach (var delta in LeadingEdges)
                        {
                            if (board.TryGetCell(
                                new Coordinate() { Row = row + delta.Row, Column = col + delta.Column }, 
                                out PieceType opiece, 
                                out PlayerType op) && op == player)
                            {
                                count++;
                            }
                        }
                    }
                }
            }

            return count;
        }

        private int CountInCenterRing(Board board, PlayerType player)
        {
            // count how many pieces are in the center ring
            var count = 0;
            for (int row = 2; row < 4; row++)
            {
                for (int col = 2; col < 4; col++)
                {
                    if (board.TryGetCell(new Coordinate() { Row = row, Column = col }, out PieceType piece, out PlayerType p) && p == player)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        /*
        private int CountInOuterRing(Board board, PlayerType player)
        {
            // count how many pieces are in the outer ring
            var count = 0;
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    var coord = new Coordinate() { Row = row, Column = col };

                    if (IsOuterRing(coord))
                    {
                        if (board.TryGetCell(new Coordinate() { Row = row, Column = col }, out PieceType piece, out PlayerType p) && p == player)
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }
        */

        private bool IsCenterRing(Coordinate coord)
        {
            return coord.Row >= 2 && coord.Row <= 3 && coord.Column >= 2 && coord.Column <= 3;
        }

        private bool IsOuterRing(Coordinate coord)
        {
            return coord.Row == 0 || 
                coord.Row == 5 || 
                coord.Column == 0 || 
                coord.Column == 5;
        }
        #endregion
    }
}
