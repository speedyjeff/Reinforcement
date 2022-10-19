using Learning;
using Learning.Helpers;
using System.IO;
using System.Text;

namespace Checkers
{
    enum LearningStyle : int { SuccessfulRun = 1, DisincentiveCats = 2, DisincentiveLoss = 4};
    enum TrainingStyle { Predictable = 1, Random = 2};
    enum LearningSituations : int { Loss = 1, King = 2, Capture = 4, Oscillation = 8};

    struct NeuralComputerOptions
    {
        public bool NoLearning;
        public bool DumpStats;
        public float MaxTrainingPct;
        public LearningStyle LStyle;
        public LearningSituations LSituations;
        public TrainingStyle TStyle;
        public int Seed;
    }

    internal class NeuralComputer : IPlayer
    {
        public NeuralComputer(Side side, int dimension, int iterations, NeuralComputerOptions options)
        {
            if (Dimension % 2 != 0) throw new Exception("must provide an even dimension");

            // init
            Random = new RandomComputer(options.Seed);
            Dimension = dimension;
            Me = side;
            MaxIterations = iterations;
            Iteration = 0;
            Steps = new List<Tuple<float[], int>>();
            NoLearning = options.NoLearning;
            IsTraining = options.MaxTrainingPct > 0;
            //Rounds = new OutcomeTracker(size: 100);
            Stats = options.DumpStats ? new OutcomeStats() : null;
            MaxTrainingPct = options.MaxTrainingPct;
            LStyle = options.LStyle;
            TStyle = options.TStyle;
            LSituations = options.LSituations;

            // determine the input offsets
            OffsetSize = (int)Math.Floor((float)(Dimension * Dimension) / 2f);
            OffsetMine = 0;
            OffsetMineKing = OffsetMine + OffsetSize;
            OffsetTheirs = OffsetMineKing + OffsetSize;
            OffsetTheirsKing = OffsetTheirs + OffsetSize;
            OffsetEmpty = OffsetTheirsKing + OffsetSize;
            InputSize = OffsetEmpty + OffsetSize;

            // network
            try
            {
                if (File.Exists($"{Me}.neural"))
                {
                    Network = NeuralNetwork.Load($"{Me}.neural");
                }
            }
            catch (Exception)
            { 
            }

            // create a network if one is not found
            if (Network == null)
            {
                Network = new NeuralNetwork(
                    new NeuralOptions()
                    {
                        InputNumber = InputSize,
                        OutputNumber = OffsetSize * OffsetMoves,
                        HiddenLayerNumber = new int[] { 128, 256, 128 },
                        LearningRate = 0.000015f,
                        MinibatchCount = 1
                    });
            }
        }

        public bool IsTraining { get; private set; }

        public Move ChooseAction(CheckersBoard board)
        {
            var binput = BoardToInput(board);
            Move result = null;

            // choose
            if (IsTraining)
            {
                // choose randomly
                result = Random.ChooseAction(board);
            }
            else
            {
                // ask the network
                var output = Network.Evaluate(binput);

                // choose the best match of available moves
                var max = Single.MinValue;
                foreach(var move in board.GetAvailableMoves())
                {
                    var idx = MoveToIndex(move);
                    if (max < output.Probabilities[idx])
                    {
                        max = output.Probabilities[idx];
                        result = move;
                    }
                }

                // stats
                if (Stats != null)
                {
                    var context = ReadableInput(binput);
                    var idx = MoveToIndex(result);
                    Stats.Add(Iteration, context, choice: idx, output.Probabilities);
                }
            }

            if (result == null) throw new Exception("failed to get a move");

            // store the move
            if (!NoLearning)
            {
                var index = MoveToIndex(result);
                Steps.Add(new Tuple<float[], int>(binput, index));
            }

            return result;
        }

        public void Finish(CheckersBoard board, Side winner, Move lastMove)
        {
            if (Steps.Count > 0)
            {
                // if this is a win, then train
                if ((LStyle & LearningStyle.SuccessfulRun) == LearningStyle.SuccessfulRun && winner == Me)
                {
                    foreach (var tup in Steps)
                    {
                        // train
                        ApplyReward(input: tup.Item1, result: tup.Item2, positive: true);

                        // stats
                        if (Stats != null)
                        {
                            var context = ReadableInput(tup.Item1);
                            Stats.Add(Iteration, context, choice: tup.Item2);
                        }
                    }
                }

                if (((LStyle & LearningStyle.DisincentiveLoss) == LearningStyle.DisincentiveLoss && winner != Side.None && winner != Me) ||
                    ((LStyle & LearningStyle.DisincentiveCats) == LearningStyle.DisincentiveCats && winner == Side.None))
                {
                    // disincentive these steps
                    foreach (var tup in Steps)
                    {
                        // train
                        ApplyReward(input: tup.Item1, result: tup.Item2, positive: false);
                    }
                }

                if (LSituations > 0)
                {
                    // apply partial rewards (including the last since this is a cats/lose)
                    DetermineAndApplyRewards();
                }

                // clear the steps
                Steps.Clear();
            }

            // increment the iteration
            Iteration++;

            // update IsTraining
            IsTraining = DetermineTraining();
        }

        public void Save()
        {
            // save the model
            Network.Save(filename: $"{Me}.neural");

            // save the stats
            if (Stats != null) Stats.ToFile($"{Me}.stats");
        }

        #region private
        private RandomComputer Random;
        private NeuralNetwork Network;
        private int Dimension;
        private Side Me;
        private int Iteration;
        private OutcomeStats Stats;
        private int MaxIterations;
        private bool NoLearning;
        private float MaxTrainingPct;
        private LearningStyle LStyle;
        private TrainingStyle TStyle;
        private LearningSituations LSituations;

        // input layout
        //  [mine, mine king, theirs, their king, empty]
        // eg. 2 x 2
        //  [0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0] 

        // input offsets
        private int OffsetMine;
        private int OffsetMineKing;
        private int OffsetTheirs;
        private int OffsetTheirsKing;
        private int OffsetEmpty;
        private int InputSize;
        private int OffsetSize;

        // output offsets
        //  [cell 0 upleft, cell 0 up right, cell 0 downleft, cell 0 down right, cell 1 ...
        private const int OffsetMoves = 4;

        private List<Tuple<float[], int>> Steps;

        // learning
        private byte[] TrainingDistribution;
        private int TrainingIndex;

        // rewards
        struct PieceCounts
        {
            public int Mine;
            public int MineKing;
            public int Theirs;
            public int TheirsKing;

            public int Total { get { return Mine + MineKing + Theirs + TheirsKing; } }
            public int MineTotal { get { return Mine + MineKing; } }
            public int TheirsTotal { get { return Theirs + TheirsKing; } }
        }

        private bool DetermineTraining()
        {
            if (MaxTrainingPct <= 0) return false;

            // check if we need to update the distrobution
            if (TrainingDistribution == null || TrainingIndex < 0 || TrainingIndex >= TrainingDistribution.Length)
            {
                // use a sliding scale of injected training from 1 to maxTrainingPct based on how many iterations have been done
                var pctTraining = ((float)(MaxIterations - Iteration) / (float)MaxIterations) * MaxTrainingPct;
                if (TrainingDistribution == null) TrainingDistribution = new byte[100];
                var numTraining = (int)Math.Ceiling(pctTraining * (float)TrainingDistribution.Length);

                if ((TStyle & TrainingStyle.Random) == TrainingStyle.Predictable)
                {
                    // clear
                    for (int i = 0; i < TrainingDistribution.Length; i++) TrainingDistribution[i] = (byte)((i < numTraining) ? 1 : 0);
                }
                else
                {
                    // clear
                    for (int i = 0; i < TrainingDistribution.Length; i++) TrainingDistribution[i] = 0;

                    // randomize the indexes of the training events
                    var rand = new Random();
                    for (int i = 0; i < numTraining; i++)
                    {
                        var idx = 0;
                        do
                        {
                            idx = rand.Next() % TrainingDistribution.Length;
                        }
                        while (TrainingDistribution[idx] != 0);
                        TrainingDistribution[idx] = 1;
                    }
                }

                // reset
                TrainingIndex = 0;
            }

            // return training
            return TrainingDistribution[TrainingIndex++] != 0;
        }

        private void DetermineAndApplyRewards()
        {
            // nothing to compare
            if (Steps.Count <= 1) return;

            // iterate through all the changes and highlight actions that should be incentived
            var previous = new PieceCounts();
            var debug_Boards = new List<string>();
            for(int i=0; i<Steps.Count; i++)
            {
                var current = new PieceCounts();

                // calculate current
                for (int j=0; j<OffsetSize; j++)
                {
                    if (Steps[i].Item1[j + OffsetMine] != 0) current.Mine++;
                    else if (Steps[i].Item1[j + OffsetMineKing] != 0) current.MineKing++;
                    else if (Steps[i].Item1[j + OffsetTheirs] != 0) current.Theirs++;
                    else if (Steps[i].Item1[j + OffsetTheirsKing] != 0) current.TheirsKing++;
                }

                // debug
                debug_Boards.Add(ReadableInput(Steps[i].Item1));

                // compare to previous counts (skip the first one, as there is no previous)
                if (i > 0)
                {
                    // lost a piece - the previous move should be disincetived
                    if ((LSituations & LearningSituations.Loss) == LearningSituations.Loss &&
                        current.Mine < previous.Mine)
                    {
                        ApplyReward(input: Steps[i - 1].Item1, result: Steps[i - 1].Item2, positive: false);
                    }
                    // gained a king - the previous move should be incentived
                    else if ((LSituations & LearningSituations.King) == LearningSituations.King &&
                        current.MineKing > previous.MineKing)
                    {
                        ApplyReward(input: Steps[i - 1].Item1, result: Steps[i - 1].Item2, positive: true);
                    }
                    // captured a piece - the previous move should be incentived (this is not strictly necessary, as a jump is required to be taken)
                    else if ((LSituations & LearningSituations.Capture) == LearningSituations.Capture &&
                        current.Theirs < previous.Theirs)
                    {
                        ApplyReward(input: Steps[i - 1].Item1, result: Steps[i - 1].Item2, positive: true);
                    }
                    // avoid oscilating in the same place - disincentive
                    else if ((LSituations & LearningSituations.Oscillation) == LearningSituations.Oscillation &&
                        i >= 2 && current.Theirs == previous.Theirs && current.Mine == previous.Mine)
                    {
                        // if the 'mine' pieces are in the same place as Steps[i].Item1 and Steps[i-2].Item1 then there is a piece that is oscilating
                        var same = true;
                        for (int j = 0; j < OffsetSize && same; j++)
                        {
                            same = (Steps[i].Item1[j + OffsetMine] == Steps[i - 2].Item1[j + OffsetMine] &&
                                    Steps[i].Item1[j + OffsetMineKing] == Steps[i - 2].Item1[j + OffsetMineKing]);
                        }

                        // disincentive the oscilating move (the move that started the oscilation)
                        if (same) ApplyReward(input: Steps[i - 2].Item1, result: Steps[i - 2].Item2, positive: false);
                    }
                }

                // set previous
                previous = current;
            }
        }

        private void ApplyReward(float[] input, int result, bool positive)
        {
            var output = Network.Evaluate(input);
            if (positive)
            {
                Network.Learn(output, preferredResult: result);
            }
            else
            {
                var preferredResults = new float[Network.OutputNumber];
                // set the result as a dis-incentive
                preferredResults[result] = -1f;
                // learn
                Network.Learn(output, preferredResults);
            }
        }

        // utility
        private float[] BoardToInput(CheckersBoard board)
        {
            var input = new float[InputSize];

            var coord = new Coordinate();
            for (coord.Row = 0; coord.Row < board.Dimension; coord.Row++)
            {
                for (coord.Column = 0; coord.Column < board.Dimension; coord.Column++)
                {
                    coord.Piece = board[coord.Row, coord.Column];

                    // only report actionable cells
                    if (coord.Piece.IsInvalid) continue;

                    // get offset based on piece
                    var offset = -1;
                    if (coord.Piece.Side == Me)
                    {
                        // mine
                        if (coord.Piece.IsKing) offset = OffsetMineKing;
                        else offset = OffsetMine;
                    }
                    else if (coord.Piece.Side == Side.None)
                    {
                        // empty
                        offset = OffsetEmpty;
                    }
                    else
                    {
                        // theirs
                        if (coord.Piece.IsKing) offset = OffsetTheirsKing;
                        else offset = OffsetTheirs;
                    }

                    // add the cell number
                    var index = CoordinateToIndex(coord);
                    input[offset + index] = 1f;
                }
            }

            return input;
        }

        private int CoordinateToIndex(Coordinate coord)
        {
            // even
            if (Dimension % 2 == 0) return (coord.Row * (int)Math.Floor((float)Dimension / 2f)) + (int)Math.Floor((float)coord.Column / 2f);

            // odd
            // ---------------------
            // |   | 0 |   | 1 |   |
            // | 2 |   | 3 |   | 4 |
            // |   | 5 |   | 6 |   |
            // | 7 |   | 8 |   | 9 |
            // |   |10 |   |11 |   |
            // ---------------------
            throw new Exception("odd dimension");

        }

        private int MoveToIndex(Move move)
        {
            // get coordinate
            var index = CoordinateToIndex(move.Coordinate);

            // make room for the 4 directions
            index *= OffsetMoves;

            // add the directional offset
            switch (move.Direction)
            {
                case Direction.UpLeft: index += 0; break;
                case Direction.UpRight: index += 1; break;
                case Direction.DownLeft: index += 2; break;
                case Direction.DownRight: index += 3; break;
                default: throw new Exception("invalid directoin");
            }

            return index;
        }

        private string ReadableInput(float[] input)
        {
            if (Dimension % 2 != 0) throw new Exception("must be even");
            var rowCount = (int)Math.Floor((float)Dimension / 2f);

            var sb = new StringBuilder();
            var row = 0;
            sb.Append("| ");
            for (int i = 0; i < OffsetSize; i++)
            {
                // increment row
                if (i != 0)
                {
                    if (i % rowCount == 0)
                    {
                        row++;
                        // start of row
                        sb.Append('|');
                        // space before piece
                        if (row % 2 == 0) sb.Append(' ');
                    }
                    else
                    {
                        // space after piece
                        sb.Append(' ');
                    }
                }

                // output board
                if (input[i + OffsetMine] != 0) sb.Append('m');
                else if (input[i + OffsetMineKing] != 0) sb.Append('M');
                else if (input[i + OffsetTheirs] != 0) sb.Append('t');
                else if (input[i + OffsetTheirsKing] != 0) sb.Append('T');
                else if (input[i + OffsetEmpty] != 0) sb.Append('-');
                else throw new Exception("invalid board");
            }

            return sb.ToString();
        }
        #endregion
    }
}
