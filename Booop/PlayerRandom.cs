using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Booop
{
    class PlayerRandom : PlayerBase
    {
        public PlayerRandom(PlayerType player, bool verbose) : base(player, verbose)
        {
            Rand = new Random();
        }

        public Func<PieceType, Coordinate, SeamCoordinate, bool> OnMoveIntercept { get; set; }

        public override bool TryMakeMove(Board board)
        {
            // get the available moves and select at random
            if (!board.TryGetAvailableMoves(Player, out List<Coordinate> moves)) throw new Exception("failed to get moves");

            // choose a move
            var move = moves[Rand.Next() % moves.Count];

            // get piece counts
            if (!board.TryGetAvailablePieces(Player, out int smallCount, out int largeCount, out bool canUseSeam)) throw new Exception("failed to get piece counts");

            // choose either a small or large piece
            var piece = PieceType.Small;
            // check if there is a large available
            if (largeCount > 0 && (Rand.Next() % 2 == 0 || smallCount == 0)) piece = PieceType.Large;

            // choose a Seam
            var seam = new SeamCoordinate();
            if (board.TryGetAvailableSeams(Player, out List<SeamCoordinate> seams))
            {
                // randomly choose if we should
                if (Rand.Next() % 100 < 10)
                {
                    // choose one
                    var index = Rand.Next() % seams.Count;
                    seam = seams[index];
                }
            }

            // debug
            if (Verbose) Console.WriteLine($"player: {Player} piece: {piece} move: {move.Row},{move.Column} seamDirection: {seam.Direction} seamCoord: {seam.Coordinate.Row},{seam.Coordinate.Column}");

            // check for interception
            if (OnMoveIntercept != null && OnMoveIntercept(piece, move, seam)) return false;

            // try to make the move
            return board.TryTurn(Player, piece, move, seam);
        }

        public override Coordinate ChooseUpgradePiece(Board board, List<Coordinate> coords)
        {
            if (coords == null || coords.Count == 0) throw new Exception("invalid coordinates");

            // randomly choose a piece to upgrade
            return coords[Rand.Next() % coords.Count];
        }

        #region private
        private Random Rand;
        #endregion
    }
}
