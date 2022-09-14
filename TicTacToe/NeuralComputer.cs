using Learning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// There are a lot of supporting materials about how to train a neural network for tic-tac-toe
// https://medium.com/@carsten.friedrich/part-4-neural-network-q-learning-a-tic-tac-toe-player-that-learns-kind-of-2090ca4798d
// https://medium.com/@carsten.friedrich/part-5-q-network-review-and-becoming-less-greedy-64d74471206

namespace TicTacToe
{
    internal class NeuralComputer : IPlayer
    {
        public NeuralComputer(Piece piece, int dimension, int maxIterations)
        {
            // store state
            Me = piece;
            Dimension = dimension;
            MaxIterations = maxIterations;

            // init
            Random = new RandomComputer();
            Steps = new List<Tuple<float[],int>>();
            Stats = (maxIterations <= 100000) ? new NeuralNetworkComputerStats() : null;

            // determine offsets for the input array for the neural network
            OffsetMine = 0;
            OffsetTheirs = (Dimension * Dimension);
            OffsetEmpty = OffsetTheirs * 2;

            // init neural network
            Network = new NeuralNetwork(
                new NeuralOptions()
                {
                    InputNumber = Dimension * Dimension * 3,
                    OutputNumber = Dimension * Dimension,
                    HiddenLayerNumber = new int[] { Dimension * Dimension * 3 * 9 },
                    LearningRate = 0.0015f, 
                    MinibatchCount = 1
                });
        }

        public Piece Me { get; private set; }
        public bool IsTraining { get { return Iteration % 10 < 5; } }

        public Coordinate ChooseAction(TicTacToeBoard board)
        {
            Coordinate coord = new Coordinate() { Row = -1, Column = -1 };

            // encode the board
            var binput = BoardToInput(board);

            // switch back and forth from training and not, to avoid overly greedy algorithms
            if (IsTraining)
            {
                // choose randomly
                coord = Random.ChooseAction(board);
            }
            else
            {
                // use the neural network
                var output = Network.Evaluate(binput);

                // choose the best available move
                var max = Single.MinValue;
                foreach (var move in board.GetAvailble())
                {
                    var idx = CoordinateToIndex(move);
                    if (max < output.Probabilities[idx])
                    {
                        coord = move;
                        max = output.Probabilities[idx];
                    }
                }

                // stats
                if (Stats != null)
                {
                    var choice = CoordinateToIndex(coord);
                    var context = InputToReadable(binput);
                    Stats.Add(Iteration, context, choice, output.Probabilities);
                }
            }

            // save the steps taken 
            var index = CoordinateToIndex(coord);
            Steps.Add(new Tuple<float[], int>(binput, index));

            return coord;
        }

        public void Finish(TicTacToeBoard board, Piece winningPiece, Coordinate lastCoordinate)
        {
            // increment iteration
            Iteration++;

            // if this was a win and we are training, then train
            if (Steps.Count > 0)
            {
                // train the model for positive outcomes
                if (Me == winningPiece || winningPiece == Piece.Empty)
                {
                    foreach (var tup in Steps)
                    {
                        var output = Network.Evaluate(tup.Item1);

                        // train to reinforce this play through
                        Network.Learn(output, tup.Item2);

                        // stats
                        if (Stats != null)
                        {
                            var context = InputToReadable(tup.Item1);
                            Stats.Add(Iteration, context, choice: tup.Item2);
                        }
                    }
                }

                // clear the steps
                Steps.Clear();
            }

            // check if this is the last one and dump the stats
            if (Iteration >= MaxIterations)
            {
                if (Stats != null) Stats.ToFile("stats.tsv");
            }
        }

        #region private
        private IPlayer Random;
        private int Iteration;
        private int MaxIterations;
        private int Dimension;
        private NeuralNetwork Network;
        private NeuralNetworkComputerStats Stats;

        // related to board output
        private int OffsetMine;
        private int OffsetTheirs;
        private int OffsetEmpty;

        // store the steps taken to reach the last result
        private List<Tuple<float[],int>> Steps;

        // stats
        private string InputToReadable(float[] board)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Dimension * Dimension; i++)
            {
                if (board[i + OffsetMine] != 0)
                {
                    // mine
                    if (Me == Piece.O) sb.Append('o');
                    else sb.Append('x');
                }
                else if (board[i + OffsetTheirs] != 0)
                {
                    // yours
                    if (Me == Piece.O) sb.Append('x');
                    else sb.Append('o');
                }
                else if (board[i + OffsetEmpty] != 0)
                {
                    // empty
                    sb.Append('-');
                }
                else
                {
                    throw new Exception("must be one of the above");
                }
            }
            return sb.ToString();
        }

        // utility
        private float[] BoardToInput(TicTacToeBoard board)
        {
            // Dim x Dim x 3 possible states [mine, yours, empty]
            // eg. 3 x 3 board
            //       mine               theirs                     empty
            //      [0,1,2,3,4,5,6,7,8, 9,10,11,12,13,14,15,16,17, 18,19,20,21,22,23,24,25,26]
            var input = new float[Dimension * Dimension * 3];
            Coordinate coord;

            for (coord.Row = 0; coord.Row < board.Dimension; coord.Row++)
            {
                for (coord.Column = 0; coord.Column < board.Dimension; coord.Column++)
                {
                    if (!board.TryGetPiece(coord, out Piece p)) throw new Exception("invalid board");

                    // [mine | theirs | empty]
                    var index = (coord.Row * Dimension) + coord.Column;
                    if (p == Piece.Empty) index += OffsetEmpty;
                    else if (p == Me) index += OffsetMine;
                    else index += OffsetTheirs;

                    // set the index
                    if (input[index] != 0) throw new Exception("invalid board layout");
                    input[index] = 1f;
                }
            }

            return input;
        }

        private int CoordinateToIndex(Coordinate coord)
        {
            return (coord.Row * Dimension) + coord.Column;
        }
        #endregion
    }
}
