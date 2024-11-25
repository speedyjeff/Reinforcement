using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booop
{
    class PlayerHuman : PlayerBase
    {
        public PlayerHuman(PlayerType player, bool verbose) : base(player, verbose)
        {
        }

        public override bool TryMakeMove(Board board)
        {
            // ask the human player for a move
            Console.WriteLine("Enter your move (index of the coord):");

            // get the available moves
            if (!board.TryGetAvailableMoves(Player, out List<Coordinate> moves)) throw new Exception("failed to get moves");

            // display the available moves
            for(int i=0; i<moves.Count; i++)
            {
                Console.Write($"{i}: {moves[i].Row} {ToColumn(moves[i].Column)}\t");
                if (i % 3 == 0) Console.WriteLine();
            }
            Console.WriteLine();

            // choose a coordinate
            var move = new Coordinate();
            while (true)
            {
                var moveIndex = Console.ReadLine();
                if (Int32.TryParse(moveIndex, out int index) && index >= 0 && index < moves.Count)
                {
                    move = moves[index];
                    break ;
                }
            }

            // get piece counts
            if (!board.TryGetAvailablePieces(Player, out int smallCount, out int largeCount, out bool canUseSeam)) throw new Exception("failed to get piece counts");

            // choose a piece
            var piece = PieceType.Small;
            if (largeCount > 0)
            {
                while(true)
                {
                    Console.WriteLine("use a large piece [y|n]: ");
                    var p = Console.ReadLine();
                    if (p == null || p.Equals("")) continue;
                    if (p.StartsWith("n", StringComparison.OrdinalIgnoreCase)) break;
                    if (p.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                    {
                        piece = PieceType.Large;
                        break;
                    }
                }
            }

            // choose a seam
            var seam = new SeamCoordinate();
            if (board.TryGetAvailableSeams(Player, out List<SeamCoordinate> seams))
            {
                // print out the available seams
                Console.WriteLine("Available seams (placed after row,column):");
                for (int i = 0; i < seams.Count; i++)
                {
                    Console.Write($"{i}: {seams[i].Direction} {seams[i].Coordinate.Row} {(char)('a' + seams[i].Coordinate.Column)}\t");
                    if (i % 3 == 0) Console.WriteLine();
                }
                Console.WriteLine();

                while (true)
                {
                    Console.WriteLine("Choose a seam [<0 for no]: ");

                    var seamIndex = Console.ReadLine();
                    if (Int32.TryParse(seamIndex, out int index))
                    {
                        if (index < 0) break;
                        else if (index >= 0 && index < seams.Count)
                        {
                            seam = seams[index];
                            break;
                        }
                    }
                }
            }

            // choose this placement
            Console.WriteLine($"moving: {move.Row} {ToColumn(move.Column)} with seam {seam.Direction} {seam.Coordinate.Row},{seam.Coordinate.Column}");
            return board.TryTurn(Player, piece, move, seam);
        }

        public override Coordinate ChooseUpgradePiece(Board board, List<Coordinate> coords)
        {
            // share the list with the player and ask them to choose which to upgrade
            Console.WriteLine("No pieces in hand.");
            Console.WriteLine("Choose a small piece to upgrade (returned as large to your hand):");
            for (int i = 0; i < coords.Count; i++)
            {
                Console.Write($"{i}: {coords[i].Row} {ToColumn(coords[i].Column)}\t");
                if (i % 3 == 0) Console.WriteLine();
            }
            Console.WriteLine();

            // choose a coordinate
            var move = new Coordinate();
            while (true)
            {
                var moveIndex = Console.ReadLine();
                if (Int32.TryParse(moveIndex, out int index) && index >= 0 && index < coords.Count)
                {
                    move = coords[index];
                    break;
                }
            }

            return move;
        }

        #region private
        private char ToColumn(int column)
        {
            return (char)('a' + column);
        }
        #endregion
    }
}
