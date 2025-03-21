using System;
using mnist;
using Learning;
using System.Text;
using System.Net.WebSockets;

namespace Learning.Tests
{
    internal class NeuralNetworkTests
    {
        public static void EndToEnd()
        {
            var tmpimages = Path.GetTempFileName();
            var tmplabels = Path.GetTempFileName();
            var tmpmodel = Path.GetTempFileName();
            try
            {
                var minput = 9;
                var mhidden = 5;
                var moutput = 5;

                // init
                CreateTrainingData(tmpimages, tmplabels);
                var images = Dataset.Read(tmpimages);
                var labels = Dataset.Read(tmplabels);
                minput = images.Rows * images.Columns;

                var nn = CreateNeuralNetwork(minput, mhidden, moutput);


                // train
                for(int i=0; i<100; i++)
                {
                    var output = nn.Evaluate(images.Data[0]);
                    nn.Learn(output, (int)labels.Data[0][0]);
                }

                // save
                nn.Save(tmpmodel);

                // validate
                ReadNeuralNetwork(tmpmodel, out List<float[]> weights, out List<float[]> bias);

                // basic checks
                if (weights == null || weights.Count != (mhidden + moutput)) throw new Exception("invalid weights");
                if (bias == null || bias.Count != (mhidden + moutput)) throw new Exception("invalid bias");

                // validate bias'
                for(int i=0; i<bias.Count; i++)
                {
                    if (bias[i].Length != 1) throw new Exception("invalid array length");
                    if (i < 5) if (bias[i][0] != -0.5f) throw new Exception("invalid bias");
                    else if (i >= 5 && i != 8) if (bias[i][0] != -1.83582079f) throw new Exception("invalid bias");
                    else if (bias[i][0] != 4.843281f) throw new Exception("invalid bias");
                }

                // validate weights
                for(int i=0; i<weights.Count; i++)
                {
                    for(int j=0; j < weights[i].Length; j++)
                    {
                        if (weights[i][j] != 0.5f) throw new Exception("invalid weight");
                    }
                }
            }
            finally
            {
                if (File.Exists(tmpimages)) File.Delete(tmpimages);
                if (File.Exists(tmplabels)) File.Delete(tmplabels);
                if (File.Exists(tmpmodel)) File.Delete(tmpmodel);
            }
        }

        public static void HiddenLayers()
        {
            // create a series of neural networks that have various hidden
            // layer topologies

            var tmpimages = Path.GetTempFileName();
            var tmplabels = Path.GetTempFileName();

            try
            {
                // init
                CreateTrainingData(tmpimages, tmplabels);
                var images = Dataset.Read(tmpimages);
                var labels = Dataset.Read(tmplabels);
                var minput = images.Rows * images.Columns;
                var moutput = 5;
                var rand = new Random();

                foreach (var hidden in new[]
                {
                    new int[] { 10 },
                    new int[] { 10, 10, 10, 10, 10},
                    new int[] {5},
                    new int[] {20},
                    new int[] {3,10,2,5}
                })
                {
                    var nn = new NeuralNetwork(
                        new NeuralOptions()
                        {
                            InputNumber = minput,
                            OutputNumber = moutput,
                            HiddenLayerNumber = hidden,
                            MinibatchCount = 1,
                            LearningRate = 0.8f
                        });

                    // train & learn
                    for (int i = 0; i < 1000; i++)
                    {
                        var output = nn.Evaluate(images.Data[0]);
                        nn.Learn(output, (int)labels.Data[0][rand.Next() % labels.Data[0].Length]);
                    }
                }
            }
            finally
            {
                if (File.Exists(tmpimages)) File.Delete(tmpimages);
                if (File.Exists(tmplabels)) File.Delete(tmplabels);
            }
        }

        public static void Converge()
        {
            // make sure that a neural network eventually will predict the right 
            // outcome

            var tmpimages = Path.GetTempFileName();
            var tmplabels = Path.GetTempFileName();

            try
            {
                // init
                CreateTrainingData(tmpimages, tmplabels);
                var images = Dataset.Read(tmpimages);
                var labels = Dataset.Read(tmplabels);
                var minput = images.Rows * images.Columns;
                var moutput = 5;

               var nn = new NeuralNetwork(
                        new NeuralOptions()
                        {
                            InputNumber = minput,
                            OutputNumber = moutput,
                            HiddenLayerNumber = new int[] {10},
                            MinibatchCount = 1,
                            LearningRate = 0.8f
                        });

                var iteration = 0;
                while (true)
                {
                    // train & learn
                    var output = nn.Evaluate(images.Data[0]);
                    nn.Learn(output, (int)labels.Data[0][0]);

                    // loop until it predicts the right value

                    if (output.Result == (int)labels.Data[0][0]) break;
                    iteration++;
                }

                if (iteration > 1) throw new Exception("too many iterations");
            }
            finally
            {
                if (File.Exists(tmpimages)) File.Delete(tmpimages);
                if (File.Exists(tmplabels)) File.Delete(tmplabels);
            }
        }

        public static void ForceNaN()
        {
            // make neural network that forces the overflow behavior (NaN/Infinity)
            var network = new NeuralNetwork(new NeuralOptions()
            {
                InputNumber = 10,
                OutputNumber = 100,
                HiddenLayerNumber = new int[] { 64, 128, 256, 512, 256, 128, 64 },
                MinibatchCount = 1,
                LearningRate = 1f
            });
            var input = new float[] { 0.5f, 0.2f, 0.3f, 0f, 1f, 0.5f, -1f, -0.9f, 0.8f, -0.00001f };

            // simulating when a calulation results in NaN/infinity
            foreach (var v in new float[] { Single.NaN, Single.PositiveInfinity, Single.NegativeInfinity })
            {
                // change a value
                input[0] = v;

                // evaluate
                var output = network.Evaluate(input);
                network.Learn(output, 0);
            }
        }

        public static void Perf()
        {
            // make neural network that forces the overflow behavior (NaN/Infinity)
            var network = new NeuralNetwork(new NeuralOptions()
            {
                InputNumber = 32 * 5,
                OutputNumber = 32 * 4,
                HiddenLayerNumber = new int[] { 128, 256, 128 },
                MinibatchCount = 1,
                LearningRate = 0.00015f
            });
            var input = new float[network.InputNumber];
            var rand = new Random();
            for (int i = 0; i < input.Length; i++) input[i] = ((float)(rand.Next() % 2000) - 1000f) / 1000f;

            // simulating when a calulation results in NaN/infinity
            for (int i = 0; i < 1000; i++)
            {
                // evaluate
                var output = network.Evaluate(input);
                network.Learn(output, 0);
            }
        }

        public static void Initalizations()
        {
            // no deep validation, just ensuring all the init paths are executed

            // create networks with each of the initialization strategies and run a few training iterations
            var weightInit = new NeuralWeightInitialization[]
            {
                NeuralWeightInitialization.Random_Uniform_NegHalf_PosHalf,
                NeuralWeightInitialization.Random_Uniform_NegOne_PosOne,
                NeuralWeightInitialization.Xavier,
                NeuralWeightInitialization.He,
                NeuralWeightInitialization.LeCun
            };
            var biasInit = new NeuralBiasInitialization[]
            {
                NeuralBiasInitialization.Random_Uniform_NegHalf_PosHalf,
                NeuralBiasInitialization.Random_Uniform_NegOneTenth_PosOneTenth,
                NeuralBiasInitialization.Zero,
                NeuralBiasInitialization.SmallConstant_OneTenth,
                NeuralBiasInitialization.SmallConstant_OneHundredth
            };

            foreach (var wi in weightInit)
            {
                var rand = new Random();
                foreach (var bi in biasInit)
                {
                    var options = new NeuralOptions()
                    {
                        WeightInitialization = wi,
                        BiasInitialization = bi,
                        InputNumber = 10,
                        OutputNumber = 10,
                        HiddenLayerNumber = new int[] { 20 },
                        LearningRate = 0.001f
                    };

                    var network = new NeuralNetwork(options);

                    for(var i=0; i<10; i++)
                    {
                        // create random input
                        var input = new float[network.InputNumber];
                        for (int j = 0; j < input.Length; j++) input[j] = ((float)(rand.Next() % 1000)) / 1000f;
                        // evaluate
                        var output = network.Evaluate(input);
                        network.Learn(output, i);
                    }
                }
            }
        }

        #region private
        private static void CreateTrainingData(string imagesfile, string labelfile)
        {
            // create fake data that has 9 inputs that map to 5 outputs

            // images
            var idata = new Dataset()
            {
                MagicNumber = 0x00000803,
                Rows = 3,
                Columns = 3,
                Count = 1
            };
            idata.Data = new float[idata.Count][];
            for(int i=0; i<idata.Data.Length; i++)
            {
                idata.Data[i] = new float[idata.Rows * idata.Columns];
                for (int j = 0; j < idata.Data[i].Length; j++) idata.Data[i][j] = (byte)j;
            }
            Dataset.Write(imagesfile, idata);

            // labels
            var ldata = new Dataset()
            {
                MagicNumber = 0x00000801,
                Rows = 1,
                Columns = 1,
                Count = 1
            };
            ldata.Data = new float[ldata.Count][];
            ldata.Data[0] = new float[] { 3 };
            Dataset.Write(labelfile, ldata);
        }

        private static NeuralNetwork CreateNeuralNetwork(int input, int hidden, int output)
        {
            // create a random neural network
            var nn = new NeuralNetwork(
                new NeuralOptions()
                {
                    HiddenLayerNumber = new int[] { hidden },
                    LearningRate = 0.8f,
                    InputNumber = input,
                    OutputNumber = output,
                    MinibatchCount = 1
                });

            // save
            var tmpfile = Path.GetTempFileName();
            try
            {
                // save to disk
                nn.Save(tmpfile);

                // change to remove randomness
                ModifySave(tmpfile);

                // load the model and return
                return NeuralNetwork.Load(tmpfile);
            }
            finally
            {
                if (File.Exists(tmpfile)) File.Delete(tmpfile);
            }
        }

        private static void ModifySave(string filename)
        {
            // WARNING: this method is tied to the implementation of save/load in NeuralNetwork

            var lines = File.ReadAllLines(filename);

            using (var writer = File.CreateText(filename))
            {
                foreach(var line in lines)
                {
                    var isWeight = line.StartsWith("WValues", StringComparison.OrdinalIgnoreCase);
                    var isBias = line.StartsWith("BValues", StringComparison.OrdinalIgnoreCase);
                    if (isWeight || isBias)
                    {
                        // weight and bias values
                        // format: *Values\ti\tj\t[values]
                        var parts = line.Split('\t');
                        var newline = new StringBuilder();
                        if (parts.Length < 4) throw new Exception("invalid array length");
                        newline.Append($"{parts[0]}\t{parts[1]}\t{parts[2]}");
                        var val = isWeight ? 0.5f : -0.5;
                        for(var i = 3; i < parts.Length; i++)
                        {
                            newline.Append($"\t{val}");
                        }
                        newline.AppendLine();
                        // re-written values
                        writer.Write(newline.ToString());
                    }
                    else
                    {
                        // pass through
                        writer.WriteLine(line);
                    }
                }
            }
        }

        private static void ReadNeuralNetwork(string filename, out List<float[]> weights, out List<float[]> bias)
        {
            // WARNING: this method is tied to the implementation of save/load in NeuralNetwork

            // init
            weights = new List<float[]>();
            bias = new List<float[]>();

            // read the values
            foreach (var line in File.ReadAllLines(filename))
            {
                // weight and bias values
                // format: *Values\ti\tj\t[values]
                if (line.StartsWith("WValues", StringComparison.OrdinalIgnoreCase) || line.StartsWith("BValues", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split('\t');
                    var newline = new StringBuilder();
                    if (parts.Length < 4) throw new Exception("invalid array length");
                    var values = new float[parts.Length -  3];
                    for (var i = 3; i < parts.Length; i++) values[i-3] = Convert.ToSingle(parts[i]);

                    if (line.StartsWith("WValues", StringComparison.OrdinalIgnoreCase)) weights.Add(values);
                    else bias.Add(values);
                }
            }
        }
        #endregion
    }
}
