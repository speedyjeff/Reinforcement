using System;
using Learning;

// training
//  - train on all moves that lead to a good outcome (0 successes)
//  - train on moves that lead to good outcomes (becomes over trained)
//  - train on winning games (0 success)
//  - train on good outcome games and wins (strong success rate)
//  - train on ties (0 success)
// good outcomes
//  - increase in large pieces
//  - create a 2 in a row
//  - disincentive the opponent getting more large pieces
// parameters
//  - % randomization (25% - reasonable, 75% - 0 successes, 0% - 0 successes, 5% - reasonable)
//  - degrade of randomization (0.0001%)
//  - network internals (256, 128 - stalled when deterministic) (256 - stalled when deterministic)
//  - train against a random model or deterministic

// best (deterministic)
//  - 256, 0.0015f, 0.05f (took 6037 to always win)
//  49900: wins: none: 0 orange[0]: 6037 purple[49900]: 43864
//   the heuristic player avoided playing in the outer ring - so the neural network learned to just play in the outer ring :p

namespace Booop
{
    class PlayerNeural : PlayerBase
    {
        public PlayerNeural(PlayerType player, bool verbose) : base(player, verbose)
        {
            // todo - load/save

            // init
            Rand = new Random();
            Generation = 0;
            Previous = new List<TrainingDetails>();

            // initialize the neural network
            Network = new NeuralNetwork(
                new NeuralOptions()
                {
                    InputNumber = 6 * 6 * NumPieceTypes, // board dimensions * piece type (nothing, {small, large} x {orange, purple})
                    OutputNumber = 6 * 6 * 2, // board dimensions * {small, large}
                    HiddenLayerNumber = new int[] { 256 },
                    MinibatchCount = 1,
                    LearningRate = 0.0015f,
                    ParallizeExecution = true
                });

            // initialize for random moves
            Randomization = 0.05f;
            Random = new PlayerRandom(player, verbose: false);
            (Random as PlayerRandom).OnMoveIntercept += RandomMoveIntercept;
        }

        public float Randomization { get; set; }
        public int Generation { get; set; }

        public override bool TryMakeMove(Board board)
        {
            if (board.Rows != 6 || board.Columns != 6) throw new Exception("invalid board size");

            // get the move
            var piece = PieceType.None;
            var move = new Coordinate();
            var seam = new SeamCoordinate();

            // todo seam

            // get the answer from the neural network
            var input = BoardToNeuralInput(board);
            var output = Network.Evaluate(input);

            // randomized moves
            var isRandomMove = false;
            if ((Rand.Next() % 100) < (int)Math.Round(100f * Randomization))
            {
                // grab a random move from the random player
                RandomFresh = false;
                Random.TryMakeMove(board);
                if (!RandomFresh) throw new Exception("failed to get random move");

                // select this move
                isRandomMove = true;
                piece = RandomPiece;
                move = RandomMove;
                // todo seam
            }
            else
            {
                if (Verbose) PrintProbabilities(output.Probabilities);

                // use the network to get the suggested move
                if (!TryChooseMoveByProbability(board, output.Probabilities, out move, out piece)) throw new Exception("failed to choose move");

                // todo seam
            }

            // set the previous pieces (for training
            var prv = new TrainingDetails()
            {
                Move = new Coordinate() { Column = move.Column, Row = move.Row },
                Piece = piece,
                Output = output, // prv now owns this reference
                GoodOutcome = false
            };

            // get stats to compare post moves
            if (!board.TryGetAvailablePieces(Player == PlayerType.Orange ? PlayerType.Purple : PlayerType.Orange, out prv.BeforeOpponentSmall, out prv.BeforeOpponentLarge, out bool _)) throw new Exception("failed to get opponent piece counts");
            if (!board.TryGetAvailablePieces(Player, out prv.BeforePlayerSmall, out prv.BeforePlayerLarge, out _)) throw new Exception("failed to get piece counts");

            // add to the previous list
            Previous.Add(prv);

            // debug
            if (Verbose) Console.WriteLine($"player: {Player} rand?: {isRandomMove} piece: {piece} move: {move.Row},{move.Column} seamDirection: {seam.Direction} seamCoord: {seam.Coordinate.Row},{seam.Coordinate.Column}");

            // make the move
            return board.TryTurn(Player, piece, move, seam);
        }

        public override Coordinate ChooseUpgradePiece(Board board, List<Coordinate> coords)
        {
            // todo
            return Random.ChooseUpgradePiece(board, coords);
        }

        public override void Feedback(Board board, bool complete)
        {

            // decide on if the previous move should be reinforced
            var opponent = Player == PlayerType.Orange ? PlayerType.Purple : PlayerType.Orange;

            /*
// check if this player has not played yet
if (Previous.Piece == PieceType.None) return;

// get current piece counts
if (!board.TryGetAvailablePieces(Player, out int playerSmall, out int playerLarge, out bool _)) throw new Exception("failed to get piece counts");
if (!board.TryGetAvailablePieces(opponent, out int opponentSmall, out int opponentLarge, out bool _)) throw new Exception("failed to get piece counts");

// strategies
//   1. incentivize good outcomes
//   2. de-centize a small number of negative outcomes
//   3. reinforce all moves that lead to a win

var reward = 1f;

// the opponent won
if (board.Winner == opponent)
{
    // provide a negative reinforcement
    reward = -1f;
}
*/

            /*
            // we won
            else if (board.Winner == Player)
            {
                // provide a positive reinforcement
                reward = 1f;
            }

            // our large pieces increased
            else if (playerLarge > Previous.PlayerLarge)
            {
                // positive reinforcement
                reward = 1f;
            }
            */

            /*
            // their large pieces increased
            if (opponentLarge > Previous.PlayerLarge)
            {
                // negative reinforcement
                reward = -1f;
            }
            */
            /*
            // are two pieces next to each other
            else if (ContainsPair(board, Player))
            {
                // positive reinforcement
                reward = 1f;
            }
            */

            // check winners
            if (complete)
            {
                // train the network (apply them backward, as once there is a good outcome, reinforce all the moves that lead to that move)
                foreach(var prv in Previous)
                {
                    // train if this was a winning game, or the move had a good outcome
                    if ((board.Winner == Player || board.Winner == PlayerType.None) || prv.GoodOutcome)
                    {
                        var index = ToOutputIndex(board.Rows, board.Columns, prv.Move, prv.Piece);
                        var preferredResult = new float[Network.OutputNumber];
                        preferredResult[index] = 1f;
                        Network.Learn(prv.Output, preferredResult);
                    }
                }

                // clear previous moves
                Previous.Clear();
            }
            else if (Previous.Count > 0)
            {
                // check if this represents a good outcome
                // get current piece counts
                if (!board.TryGetAvailablePieces(Player, out int playerSmall, out int playerLarge, out bool _)) throw new Exception("failed to get piece counts");
                //if (!board.TryGetAvailablePieces(opponent, out int opponentSmall, out int opponentLarge, out bool _)) throw new Exception("failed to get piece counts");

                // get the last details
                var latest = Previous[Previous.Count - 1];

                // our large pieces increased or a pair was created
                if (
                    playerLarge > latest.BeforePlayerLarge 
                    || ContainsPair(board, Player)
                    )
                {
                    // positive reinforcement
                    latest.GoodOutcome = true;
                }
            }
        }

        #region private
        private class TrainingDetails
        {
            public Coordinate Move;
            public PieceType Piece;
            public NeuralOutput Output;
            public int BeforePlayerSmall;
            public int BeforePlayerLarge;
            public int BeforeOpponentSmall;
            public int BeforeOpponentLarge;
            public bool GoodOutcome;
        }

        private Random Rand;
        private NeuralNetwork Network;

        // previous moves (used for reinforcement)
        private List<TrainingDetails> Previous;

        // random setup
        private bool RandomFresh;
        private PlayerRandom Random;
        private PieceType RandomPiece;
        private Coordinate RandomMove;
        private SeamCoordinate RandomSeam;

        private static readonly Coordinate[] LeadingEdges = new Coordinate[]
        {
            new Coordinate() { Row = 0, Column = 1},
            new Coordinate() { Row = 1, Column = -1},
            new Coordinate() { Row = 1, Column = 0},
            new Coordinate() { Row = 1, Column = 1}
        };

        private const int NumPieceTypes = 5;

        private bool RandomMoveIntercept(PieceType piece, Coordinate move, SeamCoordinate seam)
        {
            // intercept
            RandomFresh = true;
            RandomPiece = piece;
            RandomMove = move;
            RandomSeam = seam;
            return true;
        }

        private Coordinate IndexToCoordinate(Board board, int index)
        {
            return new Coordinate()
            {
                Row = index / board.Columns,
                Column = index % board.Columns
            };
        }

        private int CoordinateToIndex(int rows, int columns, Coordinate coord)
        {
            return (coord.Row * columns) + coord.Column;
        }

        private int ToOutputIndex(int rows, int columns, Coordinate coord, PieceType piece)
        {
            // get the index
            var index = CoordinateToIndex(rows, columns, coord);

            if (piece == PieceType.Small) index += 0;
            else if (piece == PieceType.Large) index += (rows * columns);
            else throw new Exception("invalid layout");

            return index;
        }

        private bool TryChooseMoveByProbability(Board board, float[] probabilities, out Coordinate move, out PieceType piece)
        {
            // init
            move = new Coordinate();
            piece = PieceType.None;

            // the probabilities are {row x columns} for small placement and then another {row x columns} for large placement
            var max = Single.MinValue;

            // get piece counts
            if (!board.TryGetAvailablePieces(Player, out int smallCount, out int largeCount, out bool canUseSeam)) throw new Exception("failed to get piece counts");
            if (!board.TryGetAvailableMoves(Player, out List<Coordinate> moves)) throw new Exception("failed to get moves");

            // add all the available moves to set (for quick lookup)
            var availableIndexes = new HashSet<int>();
            foreach (var m in moves) availableIndexes.Add(CoordinateToIndex(board.Rows, board.Columns, m));

            // find the best move of the small pieces
            if (smallCount > 0)
            {
                for (int i = 0; i < board.Rows * board.Columns; i++)
                {
                    var index = i;
                    if (probabilities[index] > max && availableIndexes.Contains(i))
                    {
                        max = probabilities[index];
                        move = IndexToCoordinate(board, i);
                        piece = PieceType.Small;
                    }
                }
            }

            // find the best move of the large pieces
            if (largeCount > 0)
            {
                for (int i = 0; i < board.Rows * board.Columns; i++)
                {
                    var index = i + (board.Rows * board.Columns);
                    if (probabilities[index] > max && availableIndexes.Contains(i))
                    {
                        max = probabilities[index];
                        move = IndexToCoordinate(board, i);
                        piece = PieceType.Large;
                    }
                }
            }

            // validate
            if (piece == PieceType.None) throw new Exception("failed to find a move");

            return true;
        }

        private int ToInputIndex(Board board, Coordinate coord, PlayerType player, PieceType piece)
        {
            // [nothing, ....]
            // [small orange, ....]
            // [large orange, ....]
            // [small purple, ....]
            // [large purple, ....]

            // get the index
            var index = CoordinateToIndex(board.Rows, board.Columns, coord);
            var fullSet = (board.Rows * board.Columns);

            if (player == PlayerType.Orange)
            {
                if (piece == PieceType.Small) index += fullSet;
                else if (piece == PieceType.Large) index += (2 * fullSet);
                else throw new Exception("invalid layout");
            }
            else if (player == PlayerType.Purple)
            {
                if (piece == PieceType.Small) index += (3 * fullSet);
                else if (piece == PieceType.Large) index += (4 * fullSet);
                else throw new Exception("invalid layout");
            }

            return index;
        }

        private float[] BoardToNeuralInput(Board board)
        {
            var input = new float[Network.InputNumber];
            for (int r = 0; r < board.Rows; r++)
            {
                for (int c = 0; c < board.Columns; c++)
                {
                    var coord = new Coordinate() { Row = r, Column = c };
                    if (!board.TryGetCell(coord, out PieceType piece, out PlayerType player)) throw new Exception("failed to get cell");

                    // set the piece
                    var index = ToInputIndex(board, coord, player, piece);
                    input[index] = 1f;
                }
            }
            return input;
        }

        private void PrintProbabilities(float[] probabilities)
        {
            // display the probabilities in a compact single line to console
            for (int i = 0; i < probabilities.Length; i++)
            {
                Console.Write($"{probabilities[i] * 9.9f:0}");
            }
            Console.WriteLine();
        }

        // training
        private bool ContainsPair(Board board, PlayerType player)
        {
            if (board == null) return false;

            // check if this player contains two pieces next to each other
            for (int row = 0; row < board.Rows - 1; row++)
            {
                for (int col = 0; col < board.Columns - 1; col++)
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
                                // found a pair
                                return true;
                            }
                        }
                    }
                }
            }

            // no pairs found
            return false;
        }
        #endregion
    }
}
