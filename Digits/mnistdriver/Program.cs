using Learning;
using mnist;
using System;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.Reflection.Emit;

namespace mnistdriver
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // parse arguments
            var options = Options.Parse(args);

            if (options.ShowHelp)
            {
                Options.Help();
                return -1;
            }

            // read in the data
            var images = mnist.Dataset.Read(options.ImagePath);
            var labels = mnist.Dataset.Read(options.LabelPath);

            // check that these are in the right slot
            if (images.MagicNumber == 0x00000801 && labels.MagicNumber == 0x00000803)
            {
                // swap
                var tmp = images;
                images = labels;
                labels = tmp;
            }
            if (images.MagicNumber != 0x00000803 || labels.MagicNumber != 0x00000801)
            {
                Console.WriteLine("err! invalid input");
                return -2;
            }

            // either load or initalize the model
            NeuralNetwork network = null;
            if (File.Exists(options.ModelPath))
            {
                try
                {
                    network = NeuralNetwork.Load(options.ModelPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"failed to load model : {e.Message}");
                }

                // check that the model matches the input parameters
                if (network != null)
                {
                    // todo hidden network checks
                    if (network.LearningRate != options.LearningRate) Console.WriteLine("warn! the loaded models learning rate does not match");
                    if (network.MinibatchCount != options.MinibatchSize) Console.WriteLine("warn! the loadd models mini batch size does not match");
                    if (network.InputNumber != (images.Rows * images.Columns) ||
                        network.OutputNumber != 10)
                    {
                        Console.WriteLine($"err! the model does not match the input data: {network.InputNumber} != {images.Rows * images.Columns}, {network.OutputNumber} != 10");
                        return -3;
                    }
                }
            }
            if (network == null)
            {
                network = new NeuralNetwork(
                    new NeuralOptions()
                    {
                        InputNumber = images.Rows * images.Columns,
                        OutputNumber = 10,
                        HiddenLayerNumber = options.HiddenLayers,
                        LearningRate = options.LearningRate,
                        MinibatchCount = options.MinibatchSize
                    });
            }

            // get all indexes
            var indexes = new int[labels.Count];
            for (int i = 0; i < labels.Count; i++) indexes[i] = i;
            if (options.Shuffle) Shuffle(ref indexes);

            // split the list of indexes
            int[] splitindexes = null;
            if (options.Split > 0)
            {
                // ensure the indexes are shuffled
                Shuffle(ref indexes);

                var len = (int)(options.Split * indexes.Length);
                // split the arrays into two chunks
                splitindexes = new int[len];
                var newindexes = new int[labels.Count - len];
                for(int i=0;i<indexes.Length; i++)
                {
                    if (i < len) splitindexes[i] = indexes[i];
                    else newindexes[i-len] = indexes[i];
                }
                indexes = newindexes;

                // if not shuffle, unsort
                if (!options.Shuffle)
                {
                    Array.Sort<int>(splitindexes);
                    Array.Sort<int>(indexes);
                }
            }

            // run the model
            var stats = Run(options, network, images, labels, indexes);

            // display stats
            if (!options.Quiet) stats.Display();

            // run with the remaining indexes
            if (splitindexes != null && splitindexes.Length > 0)
            {
                // do ont train this round (only 1 iteration necessary)
                options.NoTrain = true;
                options.Iterations = 1;

                // run
                stats = Run(options, network, images, labels, splitindexes);

                // display stats
                if (!options.Quiet) stats.Display();
            }

            // complete the run
            if (!options.NoSave) network.Save(options.ModelPath);

            return (int)stats.Pass;
        }

        #region private
        struct Stats
        {
            public Stats()
            {
                Total = 0;
                Pass = 0;
                Fail = 0;
                Times = new List<float>();
            }

            public long Total;
            public long Pass;
            public long Fail;

            public void Timings(out float min, out float max, out float avg)
            {
                var sum = 0f;
                min = Single.MaxValue;
                max = Single.MinValue;
                foreach(var t in Times)
                {
                    sum += t;
                    if (t < min) min = t;
                    if (t > max) max = t;
                }

                avg = sum / Times.Count;
            }

            public void Display()
            {
                Timings(out float min, out float max, out float avg);
                Console.WriteLine($"total : {Total}");
                Console.WriteLine($"pass  : {Pass} {(float)(Pass*100)/(float)Total}%");
                Console.WriteLine($"fail  : {Fail} {(float)(Fail*100) / (float)Total}%");
                Console.WriteLine($"min   : {min}");
                Console.WriteLine($"max   : {max}");
                Console.WriteLine($"avg   : {avg}");
            }

            #region internal
            internal List<float> Times;
            #endregion
        }

        private static Stats Run(Options options, NeuralNetwork network, Dataset images, Dataset labels, int[] indexes)
        {
            var stats = new Stats();

            // this round stats
            var ltotal = 0;
            var lpass = 0;

            // execute iterations times
            for (int iteration = 0; iteration < options.Iterations; iteration++)
            {
                var timer = new Stopwatch();

                // shuffle the indexes
                if (options.Shuffle) Shuffle(ref indexes);

                // run
                timer.Start();
                foreach (var i in indexes)
                {
                    // run
                    var result = network.Evaluate(images.Data[i]);

                    // train
                    if (!options.NoTrain) network.Learn(result, (int)labels.Data[i][0]);

                    // stats
                    stats.Total++;
                    ltotal++;
                    if (result.Result == (int)labels.Data[i][0]) { stats.Pass++; lpass++; }
                    else stats.Fail++;
                }
                timer.Stop();
                stats.Times.Add((float)timer.ElapsedMilliseconds / (float)images.Count);

                if (!options.Quiet) Console.WriteLine($"{iteration} : {lpass} {(float)(lpass*100) / (float)ltotal}% {(float)timer.ElapsedMilliseconds / (float)images.Count}ms");
            }

            // flush any pending learning
            network.ForceUpdate();

            return stats;
        }

        private static void Shuffle(ref int[] indexes)
        {
            var rand = new Random();

            // shuffle the list
            for (int i = 0; i < indexes.Length; i++)
            {
                var idx1 = rand.Next() % indexes.Length;
                var idx2 = idx1;
                while (idx1 == idx2) idx2 = rand.Next() % indexes.Length;
                // swap
                var tmp = indexes[idx1];
                indexes[idx1] = indexes[idx2];
                indexes[idx2] = tmp;
            }
        }
        #endregion
    }
}