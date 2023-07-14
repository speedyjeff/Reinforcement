using Learning;
using System;

namespace TeeGame
{
    // input
    //  encode the board (1 for has piece, and 0 is empty)
    // output
    //  every valid move combination 

    class NComputer : IPlayer
    {
        public NComputer(int maxIterations)
        {
            MaxIterations = maxIterations;
            Rand = new Random();
            Outputs = new List<Tuple<NeuralOutput,int>>();
            Model = new NeuralNetwork(
                new NeuralOptions()
                {
                    HiddenLayerNumber = new int[] {64, 128, 64},
                    InputNumber = 15,
                    OutputNumber = ValidMoves.Keys.Count,
                    LearningRate = 0.0015f,
                    MinibatchCount = 1,
                    ParallizeExecution = true
                });
        }

        public Move ChooseAction(TeeBoard board)
        {
            // translate the board into input for the model
            var input = new float[TeePositions.Length];
            for(int i=0; i<input.Length; i++)
            {
                input[i] = board.IsSet(TeeSupport.TeeToChar(TeePositions[i])) ? 1f : 0f;
            };

            // choose an output
            var output = Model.Evaluate(input);

            // get the available moves
            var moves = board.GetAvailableMoves();
            if (moves.Count == 0) throw new Exception("no moves");

            // choose an action
            var max = Single.MinValue;
            Move move = new Move();
            var maxIndex = 0;

            // inject random decisions to help with diversity of selection
            if ((double)Iteration/(double)MaxIterations < 0.8d &&
                Iteration % 2 == 0)
            {
                // for the first 80%, inject random 50% of the time
                var index = Rand.Next() % moves.Count;
                move = moves[index];
                var key = $"{move.Source}{move.Destination}";
                if (!ValidMoves.TryGetValue(key, out maxIndex)) throw new Exception("missing valid move");
            }
            else
            {
                // choose the one with the highest probability
                foreach (var m in moves)
                {
                    // lookup move and get probability
                    var key = $"{m.Source}{m.Destination}";
                    if (!ValidMoves.TryGetValue(key, out int index)) throw new Exception("missing valid move");
                    var prob = output.Probabilities[index];
                    if (prob > max)
                    {
                        move = m;
                        max = prob;
                        maxIndex = index;
                    }
                }
            }

            // store outputs to reinforce winning decisions
            Outputs.Add(new Tuple<NeuralOutput,int>(output, maxIndex));

            return move;
        }

        public void Finish(TeeBoard board, int teesRemaining, Move lastMove)
        {
            if (Outputs.Count > 0)
            {
                if (teesRemaining == 1)
                {
                    // train when the board was successfully solved
                    foreach (var tup in Outputs)
                    {
                        Model.Learn(tup.Item1, tup.Item2);
                    }
                }
                Outputs.Clear();
            }

            // increment the iteration counter
            Iteration++;
        }

        #region private
        private Random Rand;
        private int MaxIterations;
        private int Iteration;
        private NeuralNetwork Model;
        private List<Tuple<NeuralOutput,int>> Outputs;
        private static Tees[] TeePositions = new Tees[] { Tees._00, Tees._01, Tees._02, Tees._03, Tees._04, Tees._05, Tees._06, Tees._07, Tees._08, Tees._09, Tees._10, Tees._11, Tees._12, Tees._13, Tees._14 };
        private static Dictionary<string,int> ValidMoves = new Dictionary<string,int>()
        {
            // 0
            {$"{TeeSupport.TeeToChar(Tees._00)}{TeeSupport.TeeToChar(Tees._03)}",0 },
            {$"{TeeSupport.TeeToChar(Tees._00)}{TeeSupport.TeeToChar(Tees._05)}",1 },

            // 1
            {$"{TeeSupport.TeeToChar(Tees._01)}{TeeSupport.TeeToChar(Tees._06)}",2 },
            {$"{TeeSupport.TeeToChar(Tees._01)}{TeeSupport.TeeToChar(Tees._08)}",3 },

            // 2
            {$"{TeeSupport.TeeToChar(Tees._02)}{TeeSupport.TeeToChar(Tees._09)}",4 },
            {$"{TeeSupport.TeeToChar(Tees._02)}{TeeSupport.TeeToChar(Tees._07)}",5 },

            // 3
            {$"{TeeSupport.TeeToChar(Tees._03)}{TeeSupport.TeeToChar(Tees._00)}",6 },
            {$"{TeeSupport.TeeToChar(Tees._03)}{TeeSupport.TeeToChar(Tees._10)}",7 },
            {$"{TeeSupport.TeeToChar(Tees._03)}{TeeSupport.TeeToChar(Tees._12)}",8 },
            {$"{TeeSupport.TeeToChar(Tees._03)}{TeeSupport.TeeToChar(Tees._05)}",9 },

            // 4
            {$"{TeeSupport.TeeToChar(Tees._04)}{TeeSupport.TeeToChar(Tees._11)}",10 },
            {$"{TeeSupport.TeeToChar(Tees._04)}{TeeSupport.TeeToChar(Tees._13)}",11 },

            // 5
            {$"{TeeSupport.TeeToChar(Tees._05)}{TeeSupport.TeeToChar(Tees._00)}",12 },
            {$"{TeeSupport.TeeToChar(Tees._05)}{TeeSupport.TeeToChar(Tees._12)}",13 },
            {$"{TeeSupport.TeeToChar(Tees._05)}{TeeSupport.TeeToChar(Tees._14)}",14 },
            {$"{TeeSupport.TeeToChar(Tees._05)}{TeeSupport.TeeToChar(Tees._03)}",15 },

            // 6
            {$"{TeeSupport.TeeToChar(Tees._06)}{TeeSupport.TeeToChar(Tees._01)}",16 },
            {$"{TeeSupport.TeeToChar(Tees._06)}{TeeSupport.TeeToChar(Tees._08)}",17 },

            // 7
            {$"{TeeSupport.TeeToChar(Tees._07)}{TeeSupport.TeeToChar(Tees._02)}",18 },
            {$"{TeeSupport.TeeToChar(Tees._07)}{TeeSupport.TeeToChar(Tees._09)}",19 },

            // 8
            {$"{TeeSupport.TeeToChar(Tees._08)}{TeeSupport.TeeToChar(Tees._01)}",20 },
            {$"{TeeSupport.TeeToChar(Tees._08)}{TeeSupport.TeeToChar(Tees._06)}",21 },

            // 9
            {$"{TeeSupport.TeeToChar(Tees._09)}{TeeSupport.TeeToChar(Tees._02)}",22 },
            {$"{TeeSupport.TeeToChar(Tees._09)}{TeeSupport.TeeToChar(Tees._07)}",23 },

            // 10
            {$"{TeeSupport.TeeToChar(Tees._10)}{TeeSupport.TeeToChar(Tees._03)}",24 },
            {$"{TeeSupport.TeeToChar(Tees._10)}{TeeSupport.TeeToChar(Tees._12)}",25 },

            // 11
            {$"{TeeSupport.TeeToChar(Tees._11)}{TeeSupport.TeeToChar(Tees._04)}",26 },
            {$"{TeeSupport.TeeToChar(Tees._11)}{TeeSupport.TeeToChar(Tees._13)}",27 },

            // 12
            {$"{TeeSupport.TeeToChar(Tees._12)}{TeeSupport.TeeToChar(Tees._03)}",28 },
            {$"{TeeSupport.TeeToChar(Tees._12)}{TeeSupport.TeeToChar(Tees._05)}",29 },
            {$"{TeeSupport.TeeToChar(Tees._12)}{TeeSupport.TeeToChar(Tees._10)}",30 },
            {$"{TeeSupport.TeeToChar(Tees._12)}{TeeSupport.TeeToChar(Tees._14)}",31 },

            // 13
            {$"{TeeSupport.TeeToChar(Tees._13)}{TeeSupport.TeeToChar(Tees._04)}",32 },
            {$"{TeeSupport.TeeToChar(Tees._13)}{TeeSupport.TeeToChar(Tees._11)}",33 },

            // 14
            {$"{TeeSupport.TeeToChar(Tees._14)}{TeeSupport.TeeToChar(Tees._05)}",34 },
            {$"{TeeSupport.TeeToChar(Tees._14)}{TeeSupport.TeeToChar(Tees._12)}",35 }
        };
        #endregion
    }
}
