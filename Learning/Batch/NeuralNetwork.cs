using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

// this Neural Network is implemented with a Matrix class
// it uses 10x more memory is 2x slower than the version in NeuralNetwork
// keeping it for reference and potential future optimization

namespace Learning.Batch
{
    public struct NeuralOutput
    {
        public int Result;

        #region internal
        internal Matrix Input;
        internal Matrix[] Z;
        internal Matrix[] A;
        #endregion
    }

    public struct NeuralOptions
    {
        public int InputNumber;         // number of options in the input layer (eg. 10)
        public int OutputNumber;        // number of options in the output layer (eg. 10)
        public int[] HiddenLayerNumber; // an array containing how many hidden layers (Length) and how many options per layer
                                        // (eg. int[] { 10 } == one hidden layer of 10 options)
                                        // (default 1 hidden layer with 10)
        public double LearningRate;      // neural network learning rate (alpha) (eg. 0.15f)
        public int MinibatchCount;      // how many trainings should be applied per learning update (eg. 100)
                                        // (default 1)
    }

    public class NeuralNetwork
    {
        public NeuralNetwork(NeuralOptions options)
        {
            // validate options
            if (options.InputNumber <= 0) throw new Exception("must provide a positive number of input");
            if (options.OutputNumber <= 0) throw new Exception("must provide a positive number of output");
            if (options.MinibatchCount == 0) options.MinibatchCount = 1;
            if (options.LearningRate <= 0) throw new Exception("must provide a positive learning rate");
            if (options.HiddenLayerNumber == null) options.HiddenLayerNumber = new int[] { 10 };
            for (int i = 0; i < options.HiddenLayerNumber.Length; i++)
            {
                if (options.HiddenLayerNumber[i] <= 0) throw new Exception($"hidden layer {i} must be a positive number");
            }

            // initialize
            LearningRate = options.LearningRate;
            InputNumber = options.InputNumber;
            OutputNumber = options.OutputNumber;
            MinibatchCount = options.MinibatchCount;

            // minibatch init
            Updates = new List<NeuralUpdate>();

            // initialize number of layers (input + hidden.Length + output - 1)
            Weight = new Matrix[options.HiddenLayerNumber.Length + 1];
            Bias = new Matrix[options.HiddenLayerNumber.Length + 1];

            // create the weights and bias'
            var rand = RandomNumberGenerator.Create();
            for (int layer = 0; layer < options.HiddenLayerNumber.Length + 1; layer++)
            {
                // determine the dimensions of the layers
                var numNeuronsInLayer = layer < options.HiddenLayerNumber.Length ? options.HiddenLayerNumber[layer] : options.OutputNumber;
                var numIncomingConnections = (layer == 0) ? options.InputNumber : options.HiddenLayerNumber[layer - 1];

                // create the matrices
                Weight[layer] = Matrix.Create(
                    rows: numNeuronsInLayer, 
                    columns: numIncomingConnections, 
                    initialize: (r, c) => { return GetRandom(rand); });
                Bias[layer] = Matrix.Create(
                    rows: numNeuronsInLayer, 
                    columns: 1, 
                    initialize: (r, c) => { return GetRandom(rand); });
            }
        }

        public NeuralOutput Evaluate(Matrix input)
        {
            // forward propogate

            // ensure that the input is transposed
            if (input.Rows != InputNumber)
            {
                if (input.Columns != InputNumber) throw new Exception("must provide data transposed and match input number");

                // transpose the input
                input = input.Transpose();
            }

            // validate - todo (need to multiplex the result)
            if (input.Columns != 1) throw new Exception("must only provide one input");

            // output
            var output = new NeuralOutput()
            {
                // result
                Result = -1,

                // internal details needed for backward propogate
                Input = Matrix.Create(input),
                Z = new Matrix[Weight.Length],
                A = new Matrix[Weight.Length]
            };

            // Highlevel: 
            //  Each neuron in layer 1 is connected to each neuron in layer 2, 
            //  and each neuron in layer 2 is connected to each neuron in layer 3,
            //  and so on.
            //  In between each layer there are Weights and Bias' that are applied in this formula.
            //    = ReLU(w0*a0 + w1*a1 + ... + wn*an + Bias)
            //  The final result is calculated via a soft max and the index with the highest value
            //  is the result.

            /*
             Formulas
              Z[1]=W[1]X+b[1]
              A[1]=gReLU(Z[1]))
              Z[2]=W[2]A[1]+b[2]
              A[2]=gsoftmax(Z[2])
             */

            // progress through layers
            for (int layer = 0; layer < Weight.Length; layer++)
            {
                // first round
                //    Z0 = [Weight0] dot [input] + [Bias0]
                // second round 
                //    Z1 = [Weight1] dot [A0] + [Bias1]
                // last round
                //    Zn = [Weightn] dot [An-1] + [Biasn]
                output.Z[layer] = Weight[layer].Dot((layer == 0) ? input : output.A[layer - 1]).Addition(Bias[layer]);

                // first round
                //    A0 = ReLU(Z0)
                // second round
                //    A1 = ReLU(Z1)
                // last round
                //    An = Softmax(Zn)
                if (layer < Weight.Length - 1) output.A[layer] = ReLU(output.Z[layer]);
                else output.A[layer] = Softmax(output.Z[layer]);

                // validate
                if (output.A[layer].Columns != 1) throw new Exception("invalid");
                if (output.Z[layer].Columns != 1) throw new Exception("invalid");
            }

            // output is the index with the greatest value in An
            var max = Double.MinValue;
            output.A[output.A.Length - 1].Foreach((r, c, v) =>
            {
                if (max < v)
                {
                    max = v;
                    output.Result = r;
                }
                return v;
            });

            return output;
        }

        public void Learn(NeuralOutput output, int preferredResult)
        {
            // backward propogate

            // validate
            if (preferredResult < 0 || preferredResult >= OutputNumber) throw new Exception("invalid preferred result");
            if (output.Result < 0 || output.Result >= OutputNumber ||
                output.Input == null || output.A == null || output.Z == null ||
                output.Input.Rows != InputNumber) throw new Exception("invalid output");

            // highlevel:
            //  compute the partial deriviatives of
            //    dC/dweight and dC/dbias
            //    where C = (y-An)^2 (where Y is the best outcome)
            //  looking for local minimum of the complext function - Gradient descent
            // error = dC/dAn*dReLU(Zn)
            //  measures how much C changes at An (dC/dAn) and how much the activation function changes at Zn
            // update the weights and bias' to learn for the next round (per mini batch).
            // The algorith works backwards from the last layer to the first (as the name suggests)

            /*
             Formulas
              dZ[2]=A[2]−Y
              dW[2]=1mdZ[2]A[1]T
              dB[2]=1mΣdZ[2]
              dZ[1]=W[2]TdZ[2].∗g[1]′(z[1])
              dW[1]=1mdZ[1]A[0]T
              dB[1]=1mΣdZ[1]
             */

            // init
            var layer = Weight.Length - 1;
            Matrix dZlast;

            // gather update data
            var update = new NeuralUpdate()
            {
                dW = new Matrix[Weight.Length],
                dB = new Matrix[Weight.Length]
            };

            // create the output array with the right element set
            var Y = Matrix.Create(
                rows: output.A[layer].Rows, 
                columns: 1, 
                initialize: (r,c) => 
                {
                    if (r == preferredResult) return 1d;
                    else return 0;
                });

            // dZ = 2 * (A - Y)
            dZlast = output.A[layer].Subtract(Y).Multiply(2d);

            // dW = dZ * A(-1)
            update.dW[layer] = dZlast.Dot(output.A[layer - 1].Transpose());

            // dB = dZ
            update.dB[layer] = Matrix.Create(dZlast);

            // compute the rest in context of these values, backwards
            for (layer = Weight.Length - 2; layer >= 0; layer--)
            {
                // dZcurrent = W(+1).T dot dZlast * g`(Z)
                var dZcurrent = Weight[layer+1].Transpose()
                    .Dot(dZlast)
                    .Dot(dOfReLU(output.Z[layer]));

                // dW = dZ dot A(-1)
                update.dW[layer] = dZcurrent.Dot(((layer == 0) ? output.Input : output.A[layer - 1]).Transpose());

                // float dB = dZ
                update.dB[layer] = Matrix.Create(dZcurrent);

                // pass dZcurrent calculation to next layer
                dZlast = dZcurrent;
            }

            // add update
            lock (Updates)
            {
                Updates.Add(update);
            }
            // check if we need to do an update
            if (Updates.Count >= MinibatchCount) ForceUpdate();
        }

        public void ForceUpdate()
        {
            // check if there is work to do
            if (Updates.Count > 0)
            {
                lock (Updates)
                {
                    // accumulate the update values
                    var agg = new NeuralUpdate()
                    {
                        dW = new Matrix[Weight.Length],
                        dB = new Matrix[Bias.Length]
                    };

                    // sum the updates
                    foreach (var u in Updates)
                    {
                        for (var layer = 0; layer < Weight.Length && layer < Bias.Length; layer++)
                        {
                            // bias'
                            if (agg.dB[layer] == null) agg.dB[layer] = Matrix.Create(u.dB[layer]);
                            else agg.dB[layer] = agg.dB[layer].Addition(u.dB[layer]);

                            // weights
                            if (agg.dW[layer] == null) agg.dW[layer] = Matrix.Create(u.dW[layer]);
                            else agg.dW[layer] = agg.dW[layer].Addition(u.dW[layer]);
                        }
                    }

                    // update the weights and bias'
                    for (var layer = 0; layer < Weight.Length; layer++)
                    {
                        // W = W - alpha * dW
                        Weight[layer] = Weight[layer].Subtract(agg.dW[layer].Multiply(LearningRate / (double)Updates.Count));

                        // B = B - alpha * dB
                        Bias[layer] = Bias[layer].Subtract(agg.dB[layer].Multiply(LearningRate / (double)Updates.Count));
                    }

                    // clear
                    Updates.Clear();
                } // lock(Updates)
            } // if (Updates.Count > 0)
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public static NeuralNetwork Load()
        {
            throw new NotImplementedException();
        }

        #region private

        // example
        //  10 input, 16 hidden layer, 5 outputs
        //
        //        | between               | between              |
        //        | layer 0 & 1           | layer 1 & 2          |
        // -------------------------------------------------------
        // weight | {16, 10}              | {5, 16}              |
        //        | 16 neurons in layer 1 | 5 neurons in output  |
        //        | each connected to 10  | each connected to 16 |
        //        | neurons in layer 0    | neurons in layer 1   |
        // -------------------------------------------------------
        // bias   | {16, 1}               | {5, 1}               |
        //        | 16 neurons in layer 1 | 5 neurons in output  |
        //        | each connected to 10  | each connected to 16 |
        //        | neurons in layer 0    | neurons in layer 1   |
        // -------------------------------------------------------
        //
        // layer 0    layer 1     layer 2
        //  0 --------- 0    ...    0
        //    \________ 1           1
        //  1  \_______ 2           ...
        //  ...         ...         5
        //  10 -----    ...
        //              \_ 15   

        // Dimensions
        //  0 - layers (eg. 0 [input to hidden], 1 - [hidden to output])
        //  1 - number of neurons in layer (eg. 0 - 10 [hidden], 1 - 10 [output]) (skip input, start at first hidden to end)
        //  2 - number of connections into layer (eg. 0 - 784 [input to hidden], 1 - 10 [hidden to output])  (start at input to end, skip output)
        private Matrix[] Weight;
        private Matrix[] Bias;
        private double LearningRate;
        private int InputNumber;
        private int OutputNumber;
        private int MinibatchCount;

        // batch up updates to apply (the average smooths out learning)
        struct NeuralUpdate
        {
            public Matrix[] dW;
            public Matrix[] dB;
        }
        private List<NeuralUpdate> Updates;

        private double GetRandom(RandomNumberGenerator rand)
        {
            var int32buffer = new byte[4];
            rand.GetNonZeroBytes(int32buffer);
            // ensure positive
            int32buffer[3] &= 0x7f;
            var number = BitConverter.ToInt32(int32buffer);
            // get a random double between -0.5 and 0.5
            return ((double)number / (double)Int32.MaxValue) - 0.5d;
        }

        private Matrix ReLU(Matrix a)
        {
            // if x <= 0 : 0
            //    x >  0 : x
            return a.Foreach((r, c, v) => { return Math.Max(0, v); });
        }

        private Matrix dOfReLU(Matrix a)
        {
            // deriviative of ReLU
            // if x <= 0 : 0
            //    x >  0 : 1
            return a.Foreach((r, c, v) => { return v > 0 ? 1d : 0d; });
        }

        private Matrix Softmax(Matrix a)
        {
            // = foreach(var x in a) r += e^(x-max) / sum(e^x-max)
            var sum = 0d;
            var max = Double.MinValue;

            // max
            return a
                .Foreach((r, c, v) =>
                {
                    // find max
                    if (max < v) max = v; return v;
                })
                .Foreach((r, c, v) =>
                {
                    // calculate e^(v-max)
                    var res = Math.Pow(Math.E, v - max);
                    sum += res;
                    return res;
                })
                .Foreach((r, c, v) =>
                {
                    // e^(x-max) / sum(e^x-max)
                    return v / sum;
                });
        }
        #endregion
    }
}
