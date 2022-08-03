using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mnistdriver
{
    internal class Options
    {
        public bool ShowHelp { get; set; }
        public string ImagePath { get; set; }
        public string LabelPath { get; set; }
        public bool Shuffle { get; set; }
        public int[] HiddenLayers { get; set; }
        public float LearningRate { get; set; }
        public int MinibatchSize { get; set; }
        public string ModelPath { get; set; }
        public bool NoSave { get; set; }
        public bool NoTrain { get; set; }
        public int Iterations { get; set; }
        public bool Quiet { get; set; }
        public float Split { get; set; }
        public bool Unsupervised { get; set; }

        public static void Help()
        {
            Console.WriteLine("./mnistdriver");
            Console.WriteLine("   -help                   - display this help");
            Console.WriteLine("   -image <filepath>.gz    - path to the gziped mnist image data (eg. t10k-images-idx3-ubyte.gz)");
            Console.WriteLine("   -label <filepath>.gz    - path to the gziped mnist label data (eg. t10k-labels-idx1-ubyte.gz)");
            Console.WriteLine("   -shuffle                - shuffle the inputs while training (defualt false)");
            Console.WriteLine("   -hiddenlayers #[,#,...] - list of integers that specify how large and how many hidden layers (eg. 16,10) (default - 10)");
            Console.WriteLine("   -learningrate #.##      - learning rate for the model (default 0.15)");
            Console.WriteLine("   -minibatchsize ###      - size of batch for learning (default 100)");
            Console.WriteLine("   -modelpath <filepath>   - path to where to load/save the neural network");
            Console.WriteLine("   -nosave                 - do not save the trained model (default false)");
            Console.WriteLine("   -notrain                - run the model but do not train it");
            Console.WriteLine("   -iterations             - number of iterations to run (default 1)");
            Console.WriteLine("   -quiet                  - do not display the final stast (default false)");
            Console.WriteLine("   -split                  - percentage [0.0,1.0] to split the input to use as validation data (default 0.0)");
            Console.WriteLine("   -unsupervised           - run as an autoencoder, use the labels to validate grouping but not training");
        }

        public static Options Parse(string[] args)
        {
            var options = new Options()
            {
                HiddenLayers = new int[] { 10 },
                Shuffle = false,
                LearningRate = 0.15f,
                MinibatchSize = 100,
                ShowHelp = false,
                Iterations = 1,
                Quiet = false,
                Split = 0
            };

            // parse
            for(int i=0; i<args.Length; i++)
            {
                if (args[i].StartsWith("-he", StringComparison.OrdinalIgnoreCase))
                {
                    options.ShowHelp = true;
                }
                else if (args[i].StartsWith("-im", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.ImagePath = args[++i];
                }
                else if (args[i].StartsWith("-la", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.LabelPath = args[++i];
                }
                else if (args[i].StartsWith("-sh", StringComparison.OrdinalIgnoreCase))
                {
                    options.Shuffle = true;
                }
                else if (args[i].StartsWith("-hi", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        // parse #,#,#
                        var parts = args[++i].Split(',');
                        options.HiddenLayers = new int[parts.Length];
                        for (int j = 0; j < parts.Length; j++) options.HiddenLayers[j] = Convert.ToInt32(parts[j]);
                    }
                }
                else if (args[i].StartsWith("-le", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.LearningRate = Convert.ToSingle(args[++i]);
                }
                else if (args[i].StartsWith("-mi", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.MinibatchSize = Convert.ToInt32(args[++i]);
                }
                else if (args[i].StartsWith("-mo", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.ModelPath = args[++i];
                }
                else if (args[i].StartsWith("-nosa", StringComparison.OrdinalIgnoreCase))
                {
                    options.NoSave = true;
                }
                else if (args[i].StartsWith("-notr", StringComparison.OrdinalIgnoreCase))
                {
                    options.NoTrain = true;
                }
                else if (args[i].StartsWith("-q", StringComparison.OrdinalIgnoreCase))
                {
                    options.Quiet = true;
                }
                else if (args[i].StartsWith("-it", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.Iterations = Convert.ToInt32(args[++i]);
                }
                else if (args[i].StartsWith("-sp", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.Split = Convert.ToSingle(args[++i]);
                }
                else if (args[i].StartsWith("-u", StringComparison.OrdinalIgnoreCase))
                {
                    options.Unsupervised = true;
                }
                else
                {
                    Console.WriteLine($"unknown argument : {args[i]}");
                }
            }

            // validation
            if (options.Iterations <= 0)
            {
                Console.WriteLine("iterations must be positive");
                options.ShowHelp = true;
            }
            if (options.LearningRate <= 0)
            {
                Console.WriteLine("learningrate must be positive");
                options.ShowHelp = true;
            }
            if (options.MinibatchSize <= 0)
            {
                Console.WriteLine("minibatchsize must be positve");
                options.ShowHelp = true;
            }
            if (string.IsNullOrWhiteSpace(options.ImagePath) ||
                !File.Exists(options.ImagePath))
            {
                Console.WriteLine($"must provide a valid path to mnist image data ({options.ImagePath})");
                options.ShowHelp = true;
            }
            if (string.IsNullOrWhiteSpace(options.LabelPath) ||
                !File.Exists(options.LabelPath))
            {
                Console.WriteLine("must provide a valid path to mnist label data");
                options.ShowHelp = true;
            }
            if (options.HiddenLayers != null)
            {
                var invalid = false;

                if (options.HiddenLayers.Length == 0) invalid = true;
                else for (int j = 0; j < options.HiddenLayers.Length; j++) invalid |= (options.HiddenLayers[j] == 0);

                if (invalid)
                {
                    Console.WriteLine("the hidden layers must be positive values");
                    options.ShowHelp = true;
                }
            }
            if (options.Unsupervised && options.Split > 0)
            {
                Console.WriteLine("unsupervised is not supported with splitting the data");
                options.ShowHelp = true;
            }

            // fix incorrect parameters
            if (string.IsNullOrWhiteSpace(options.ModelPath)) options.NoSave = true;
            if (options.Split < 0) options.Split = 0f;
            if (options.Split > 1) options.Split = 1f;

            return options;
        }
    }
}
