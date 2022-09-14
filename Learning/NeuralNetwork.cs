using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;


// A lot of reference matreial was used to generate this code.  Here are a few of the most useful materials I found.
//   https://en.wikipedia.org/wiki/Mathematics_of_artificial_neural_networks
//   https://www.youtube.com/watch?v=aircAruvnKk (series of videos)
//   http://neuralnetworksanddeeplearning.com/index.html
//     https://github.com/mnielsen/neural-networks-and-deep-learning/blob/master/src/network.py
//   https://www.youtube.com/watch?v=w8yWXqWQYmU
//     https://www.kaggle.com/code/wwsalmon/simple-mnist-nn-from-scratch-numpy-no-tf-keras/notebook

// a good starting data set is the mnist hand written digits
//  http://yann.lecun.com/exdb/mnist/

// Setup using the mnist data set
//  var Network = new NeuralNetwork(
//  new NeuralOptions()
//  {
//      InputNumber = 784,
//      OutputNumber = 10,
//      HiddenLayerNumber = new int[] { 10 },
//      LearningRate = 0.15f,
//      MinibatchCount = 100
//  });
// input is a grayscale [0-1.0f] pixel for a 28x28 pixel image
// output is [0-9] that represents the hand written digit written in the image
// To evaluate an input call Evaluate
// To train the model, you call Learn with a preferred output so that that the model evolves in that direction

namespace Learning
{
    public struct NeuralOutput
    {
        public int Result;
        public float[] Probabilities;

        #region internal
        internal float[] Input;
        internal float[][] Z;
        internal float[][] A;
        #endregion
    }

    public struct NeuralOptions
    {
        public int InputNumber;         // number of options in the input layer (eg. 10)
        public int OutputNumber;        // number of options in the output layer (eg. 10)
        public int[] HiddenLayerNumber; // an array containing how many hidden layers (Length) and how many options per layer
                                        // (eg. int[] { 10 } == one hidden layer of 10 options)
                                        // (default 1 hidden layer with 10)
        public float LearningRate;      // neural network learning rate (alpha) (eg. 0.15f)
        public int MinibatchCount;      // how many trainings should be applied per learning update (eg. 100)
                                        // (default 1)
    }

    public class NeuralNetwork
    {
        public NeuralNetwork()
        {
            // init
            Updates = new List<NeuralUpdate>();
        }

        public NeuralNetwork(NeuralOptions options) : this()
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

            // initialize number of layers (input + hidden.Length + output - 1)
            Weight = new float[options.HiddenLayerNumber.Length + 1][][];
            Bias = new float[options.HiddenLayerNumber.Length + 1][][];

            // create the weights and bias'
            var rand = RandomNumberGenerator.Create();
            for (int layer=0; layer< options.HiddenLayerNumber.Length + 1; layer++)
            {
                // initialize neurons in layer
                var numNeuronsInLayer = layer < options.HiddenLayerNumber.Length ? options.HiddenLayerNumber[layer] : options.OutputNumber;
                Weight[layer] = new float[numNeuronsInLayer][];
                Bias[layer] = new float[numNeuronsInLayer][];

                for(int neuron=0; neuron < numNeuronsInLayer; neuron++)
                {
                    // initialize connections into layer
                    var numIncomingConnections = (layer == 0) ? options.InputNumber : options.HiddenLayerNumber[layer-1];
                    Weight[layer][neuron] = new float[numIncomingConnections];
                    Bias[layer][neuron] = new float[1]
                    {
                        GetRandom(rand)
                    };

                    // initialize with random values
                    for(int i = 0; i<numIncomingConnections; i++)
                    {
                        Weight[layer][neuron][i] = GetRandom(rand);
                    }
                }
            }
        }

        public float LearningRate { get; private set; }
        public int InputNumber { get; private set; }
        public int OutputNumber { get; private set; }
        public int MinibatchCount { get; private set; }

        public NeuralOutput Evaluate(float[] input)
        {
            // forward propogate

            // validate
            if (input == null || input.Length != InputNumber) throw new Exception("invalid input");

            // output
            var output = new NeuralOutput()
            {
                // result
                Result = -1,

                // internal details needed for backward propogate
                Input = new float[input.Length],
                Z = new float[Weight.Length][],
                A = new float[Weight.Length][]
            };

            // save input
            for (int i = 0; i < input.Length; i++) output.Input[i] = input[i];

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
                output.Z[layer] = new float[Weight[layer].Length];
                for (int neuron = 0; neuron < Weight[layer].Length; neuron++)
                {
                    output.Z[layer][neuron] = Dot(Weight[layer][neuron], (layer == 0) ? input : output.A[layer - 1]) + Bias[layer][neuron][0];
                }

                // first round
                //    A0 = ReLU(Z0)
                // second round
                //    A1 = ReLU(Z1)
                // last round
                //    An = Softmax(Zn)
                if (layer < Weight.Length - 1) output.A[layer] = ReLU(output.Z[layer]);
                else output.A[layer] = Softmax(output.Z[layer]);
            }

            // output is the index with the greatest value in An
            var max = Single.MinValue;
            output.Probabilities = new float[output.A[output.A.Length - 1].Length];
            for(int i=0; i < output.A[output.A.Length-1].Length; i++)
            {
                // copy all probabilities
                output.Probabilities[i] = output.A[output.A.Length - 1][i];
                // find the largest
                if (max < output.A[output.A.Length - 1][i])
                {
                    max = output.A[output.A.Length - 1][i];
                    output.Result = i;
                }
            }

            return output;
        }

        public void Learn(NeuralOutput output, int preferredResult)
        {
            // backward propogate

            // validate
            if (preferredResult < 0 || preferredResult >= OutputNumber) throw new Exception("invalid preferred result");
            if (output.Result < 0 || output.Result >= OutputNumber ||
                output.Input == null || output.A == null || output.Z == null ||
                output.Input.Length != InputNumber) throw new Exception("invalid output");
 
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
            float[] dZlast;

            // gather update data
            var update = new NeuralUpdate()
            {
                dW = new float[Weight.Length][][],
                dB = new float[Weight.Length][]
            };

            // create the output array with the right element set
            var Y = new float[output.A[layer].Length];
            Y[preferredResult] = 1f;

            // float[] dZ = 2 * (A - Y)
            dZlast = Multiply(2f, Subtract(output.A[layer], Y));

            // float[][] dW = dZ * A(-1)
            update.dW[layer] = DotSecondParamT(dZlast, output.A[layer - 1]);

            // float dB = dZ
            update.dB[layer] = new float[dZlast.Length];
            for (int i = 0; i < update.dB[layer].Length; i++) update.dB[layer][i] = dZlast[i];

            // compute the rest in context of these values, backwards
            for (layer = Weight.Length - 2; layer >= 0; layer--)
            {
                // float[] dZcurrent = W(+1).T dot dZlast * g`(Z)
                var dZcurrent = new float[Weight[layer].Length];
                dZcurrent = DotFirstParamT(Weight[layer + 1], dZlast);
                dZcurrent = Multiply(dZcurrent, dOfReLU(output.Z[layer]));

                // float[][] dW = dZ dot A(-1)
                update.dW[layer] = DotSecondParamT(dZcurrent, (layer == 0) ? output.Input : output.A[layer - 1]);

                // float dB = dZ
                update.dB[layer] = new float[dZcurrent.Length];
                for (int i = 0; i < update.dB[layer].Length; i++) update.dB[layer][i] = dZcurrent[i];

                // pass dZcurrent calculation to next layer
                dZlast = dZcurrent;
            }

            // add update
            lock(Updates)
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
                        dW = new float[Weight.Length][][],
                        dB = new float[Bias.Length][]
                    };

                    // average the updates
                    foreach(var u in Updates)
                    {
                        for (var layer = 0; layer < Weight.Length && layer < Bias.Length; layer++)
                        {
                            // bias'
                            agg.dB[layer] = new float[u.dB[layer].Length];
                            for (var j = 0; j < u.dB[layer].Length; j++) agg.dB[layer][j] += u.dB[layer][j];

                            // weights
                            agg.dW[layer] = new float[u.dW[layer].Length][];
                            for (var j = 0; j < u.dW[layer].Length; j++)
                            {
                                agg.dW[layer][j] = new float[u.dW[layer][j].Length];
                                for (var k = 0; k < u.dW[layer][j].Length; k++)
                                {
                                    agg.dW[layer][j][k] += u.dW[layer][j][k];
                                }
                            }
                        }
                    }

                    // update the weights and bias'
                    for (var layer = 0; layer < Weight.Length; layer++)
                    {
                        for (int neuron = 0; neuron < Weight[layer].Length; neuron++)
                        {
                            // W = W - alpha * dW
                            Weight[layer][neuron] = Subtract(Weight[layer][neuron], Multiply(LearningRate / (float)Updates.Count, agg.dW[layer][neuron]));

                            // B = B - alpha * dB
                            Bias[layer][neuron][0] = Bias[layer][neuron][0] - ((LearningRate / (float)Updates.Count) * agg.dB[layer][neuron]);
                        }
                    }

                    // clear
                    Updates.Clear();
                } // lock(Updates)
            } // if (Updates.Count > 0)
        }

        public void Save(string filename)
        {
            // force an update first
            ForceUpdate();

            // serialize to disk
            using (var writer = File.CreateText(filename))
            {
                writer.WriteLine($"LearningRate\t{LearningRate}");
                writer.WriteLine($"InputNumber\t{InputNumber}");
                writer.WriteLine($"OutputNumber\t{OutputNumber}");
                writer.WriteLine($"MinibatchCount\t{MinibatchCount}");
                writer.WriteLine($"Weight\t{Weight.Length}");
                for (int i = 0; i < Weight.Length; i++)
                {
                    writer.WriteLine($"Weight\t{i}\t{Weight[i].Length}");
                    for (int j = 0; j < Weight[i].Length; j++)
                    {
                        writer.Write($"WValues\t{i}\t{j}");
                        for (int k = 0; k < Weight[i][j].Length; k++)
                        {
                            writer.Write($"\t{Weight[i][j][k]}");
                        }
                        writer.WriteLine();
                    }
                }
                writer.WriteLine($"Bias\t{Bias.Length}");
                for (int i = 0; i < Bias.Length; i++)
                {
                    writer.WriteLine($"Bias\t{i}\t{Bias[i].Length}");
                    for (int j = 0; j < Bias[i].Length; j++)
                    {
                        writer.Write($"BValues\t{i}\t{j}");
                        for (int k = 0; k < Bias[i][j].Length; k++)
                        {
                            writer.Write($"\t{Bias[i][j][k]}");
                        }
                        writer.WriteLine();
                    }
                }
            }
        }

        public static NeuralNetwork Load(string filename)
        {
            // load
            var network = new NeuralNetwork();

            using (var reader = File.OpenText(filename))
            {
                int l0, l1, i, j, k, len;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var parts = line.Split('\t');
                    if (parts.Length < 2) throw new Exception("line too short");
                    switch (parts[0])
                    {
                        case "LearningRate": network.LearningRate = Convert.ToSingle(parts[1]); break;
                        case "InputNumber": network.InputNumber = Convert.ToInt32(parts[1]); break;
                        case "OutputNumber": network.OutputNumber = Convert.ToInt32(parts[1]); break;
                        case "MinibatchCount": network.MinibatchCount = Convert.ToInt32(parts[1]); break;
                        case "Weight":
                            // init for Weight
                            if (parts.Length == 2)
                            {
                                l0 = Convert.ToInt32(parts[1]);
                                network.Weight = new float[l0][][];
                            }
                            else if (parts.Length == 3)
                            {
                                i = Convert.ToInt32(parts[1]);
                                l1 = Convert.ToInt32(parts[2]);
                                network.Weight[i] = new float[l1][];
                            }
                            else throw new Exception("unknown array dimension");
                            break;
                        case "WValues":
                            // values for Weight
                            if (parts.Length < 3) throw new Exception("invalid array");
                            i = Convert.ToInt32(parts[1]);
                            j = Convert.ToInt32(parts[2]);
                            len = parts.Length - 3;
                            network.Weight[i][j] = new float[len];
                            for (k = 0; k < len; k++) network.Weight[i][j][k] = Convert.ToSingle(parts[k + 3]);
                            break;
                        case "Bias":
                            // init for Bias
                            if (parts.Length == 2)
                            {
                                l0 = Convert.ToInt32(parts[1]);
                                network.Bias = new float[l0][][];
                            }
                            else if (parts.Length == 3)
                            {
                                i = Convert.ToInt32(parts[1]);
                                l1 = Convert.ToInt32(parts[2]);
                                network.Bias[i] = new float[l1][];
                            }
                            else throw new Exception("unknown array dimension");
                            break;
                        case "BValues":
                            // values for Bias
                            if (parts.Length < 3) throw new Exception("invalid array");
                            i = Convert.ToInt32(parts[1]);
                            j = Convert.ToInt32(parts[2]);
                            len = parts.Length - 3;
                            network.Bias[i][j] = new float[len];
                            for (k = 0; k < len; k++) network.Bias[i][j][k] = Convert.ToSingle(parts[k + 3]);
                            break;
                        default:
                            throw new Exception($"unknown token : {parts[0]}");
                    }
                }
            }

            return network;
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
        public float[][][] Weight;
        public float[][][] Bias;

        // batch up updates to apply (the average smooths out learning)
        struct NeuralUpdate
        {
            public float[][][] dW;
            public float[][] dB;
        }
        private List<NeuralUpdate> Updates;

        private float GetRandom(RandomNumberGenerator rand)
        {
            var int32buffer = new byte[4];
            rand.GetNonZeroBytes(int32buffer);
            // ensure positive
            int32buffer[3] &= 0x7f;
            var number = BitConverter.ToInt32(int32buffer);
            // get a random float between -0.5 and 0.5
            return ((float)number / (float)Int32.MaxValue) - 0.5f;
        }

        private float[] ReLU(float[] a)
        {
            // if x <= 0 : 0
            //    x >  0 : x
            var result = new float[a.Length];
            for (int i = 0; i < a.Length; i++) result[i] = Math.Max(0, a[i]);
            return result;
        }

        private float[] dOfReLU(float[] a)
        {
            // deriviative of ReLU
            // if x <= 0 : 0
            //    x >  0 : 1
            var result = new float[a.Length];
            for (int i = 0; i < a.Length; i++) result[i] = a[i] > 0 ? 1 : 0;
            return result;
        }

        private float[] Softmax(float[] a)
        {
            // = foreach(var x in a) r += e^(x-max) / sum(e^x-max)
            var sum = 0f;
            var max = Single.MinValue;
            var result = new float[a.Length];

            // max
            for (int i = 0; i < a.Length; i++) if (max < a[i]) max = a[i];
            // sum(e^(x-max))
            for (int i = 0; i < a.Length; i++)
            {
                result[i] = (float)Math.Pow(Math.E, a[i] - max);
                sum += result[i];
            }
            // e^(x-max) / sum(e^x-max)
            for (int i = 0; i < a.Length; i++) result[i] = result[i] / sum;

            return result;
        }

        private float Dot(float[] a, float[] b)
        {
            if (a.Length != b.Length) throw new Exception("lengths must match");
            var result = 0f;
            var sign = 1;
            for(int i=0; i<a.Length && i<b.Length; i++)
            {
                result += (a[i] * b[i]);

                // check where the result was heading
                if (!Double.IsNaN(result) && !Double.IsInfinity(result)) sign = result < 0 ? -1 : 1;
            }

            // cap
            if (Double.IsPositiveInfinity(result) || (Double.IsNaN(result) && sign > 0)) result = Single.MaxValue;
            else if (Double.IsNegativeInfinity(result) || (Double.IsNaN(result) && sign < 0)) result = Single.MinValue;

            return result;
        }

        private float[] DotFirstParamT(float[][] a, float[] b)
        {
            if (a == null || a.Length == 0 ||
                a.Length != b.Length) throw new Exception("invalid array lengths");

            // fake [[,,,],[,,,]] dot [[],[],[]]
            // first parameter is transposed
            var result = new float[a[0].Length];
            for (int i = 0; i < a[0].Length; i++)
            {
                var sign = 1;
                for (int j = 0; j < b.Length; j++)
                {
                    // keeping the column stable (eg. i) while
                    // walking the row (eg. j)
                    result[i] += a[j][i] * b[j];

                    // check where the result was heading
                    if (!Double.IsNaN(result[i]) && !Double.IsInfinity(result[i])) sign = result[i] < 0 ? -1 : 1;
                }

                // cap
                if (Double.IsPositiveInfinity(result[i]) || (Double.IsNaN(result[i]) && sign > 0)) result[i] = Single.MaxValue;
                else if (Double.IsNegativeInfinity(result[i]) || (Double.IsNaN(result[i]) && sign < 0)) result[i] = Single.MinValue;
            }
            return result;
        }

        private float[][] DotSecondParamT(float[] a, float[] b)
        {
            // fake [[],[],[]] dot [[,,,]]
            // second parameter is transposed
            var result = new float[a.Length][];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new float[b.Length];
                for (int j = 0; j < result[i].Length; j++)
                {
                    result[i][j] = a[i] * b[j];
                }
            }
            return result;
        }

        private float[] Subtract(float[] a, float[] b)
        {
            if (a.Length != b.Length) throw new Exception("lengths must match");
            var result = new float[a.Length];
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                result[i] = (a[i] - b[i]);
            }
            return result;
        }

        private float[] Multiply(float value, float[] a)
        {
            var result = new float[a.Length];
            for(int i=0; i<a.Length; i++)
            {
                result[i] = a[i] * value;
            }
            return result;
        }

        private float[] Multiply(float[] a, float[] b)
        {
            if (a.Length != b.Length) throw new Exception("lengths must match");
            var result = new float[a.Length];
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                result[i] = a[i] * b[i];
            }
            return result;
        }
        #endregion
    }
}
