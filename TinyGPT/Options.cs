using System;
using System.IO;

using Learning;

namespace TinyGPT
{
    public enum DatasetType
    {
        None = 0,
        Alphabet = 1,
        Shakespeare = 2
    }

    class Options
    {
        public DatasetType Dataset { get; set; }
        public bool Help { get; set; }
        public int TrainingIterations { get; set; }
        public int IterationsPerTraining { get; set; }
        public int Inferences { get; set; }
        public float SuccessCriteria { get; set; }
        public int StopAfterNumSuccesses { get; set; }
        public int MinTrainSequenceLength { get; set; }
        public int[] LayerMultipliers { get; set; }
        public float LearningFactor { get; set; }
        public NeuralWeightInitialization WeightInitialization { get; set; }
        public NeuralBiasInitialization BiasInitialization { get; set; }
        public string Vocabulary { get; set; }
        public int SequenceLength { get; set; }
        public string ShakespeareFilepath { get; set; }
        public float PercentOfText { get; set; }
        public int TokenizerIterations { get; set; }
        public int NormalizeTokens { get; set; }
        public bool Verbose { get; set; }

        public static void ShowHelp()
        {
            Console.WriteLine("Usage: tinygpt -d[ataset] <dataset> [options]");
            Console.WriteLine("  -d[ataset] <dataset>      - dataset to use (1 = alphabet, 2 = Shakespeare)");
            Console.WriteLine("Options:");
            Console.WriteLine("  -tr[ainingIterations]     - number of training iterations");
            Console.WriteLine("  -it[erations]             - number of training per iteration");
            Console.WriteLine("  -in[ferences]             - number of inferences to make");
            Console.WriteLine("  -su[ccessCriteria] <real> - success criteria (default is 1.0 or 0.95)");
            Console.WriteLine("  -st[opAfterNumSuccesses]  - stop after N successful inferences in a row (default is 10)");
            Console.WriteLine("  -w[eightInitialization]   - weight initialization (default is 0)");
            Console.WriteLine("  -b[iasInitialization]     - bias initialization (default is 0)");
            Console.WriteLine("  -se[quenceLength] <len>   - sequence length to use (default is 6 or 16)");
            Console.WriteLine("  -la[yerMultipliers] <int>[,<int>,...] - multiplier used in the hidden layers of the network");
            Console.WriteLine("  -le[arningFactor] <real>   - learning factor (default is 0.0001 or 0.00001)");
            Console.WriteLine(" alphabet:");
            Console.WriteLine("   -vo[cabulary] <vocab>   - vocabulary to use (default is 'ABC')");
            Console.WriteLine(" shakespeare:");
            Console.WriteLine("   -sh[akespeareFilepath <path> - path to the file containing Shakespeare's work");
            Console.WriteLine("   -p[ercentOfText] <real>      - percent of text to use (default is 0.1)");
            Console.WriteLine("   -to[kenizerIterations] <int> - number of iterations to use for the tokenizer (default is 10)");
            Console.WriteLine("   -n[ormalizeTokens] <int>     - normalization to use for the tokenizer (default is 0)");
            Console.WriteLine("   -m[inTrainSequenceLength] <int> - minimum sequence length when training (default is 1)");
            Console.WriteLine("  -ve[rbose]               - verbose output");
            Console.WriteLine("  -h[elp]                  - show this help message");
        }

        public static Options Parse(string[] args)
        {
            var options = new Options()
            {
                Dataset = DatasetType.None,
                TrainingIterations = 1000,
                IterationsPerTraining = 1000,
                Inferences = 100,
                WeightInitialization = NeuralWeightInitialization.Random_Uniform_NegHalf_PosHalf,
                BiasInitialization = NeuralBiasInitialization.Random_Uniform_NegHalf_PosHalf,
                StopAfterNumSuccesses = 10
            };

            // first option is choosing the dataset, this is necessary as the default are different between datasets
            var i = 0;
            if (args.Length >= 2 &&
                (args[0].StartsWith("-d", StringComparison.OrdinalIgnoreCase) || args[0].Equals("-dataset", StringComparison.OrdinalIgnoreCase)))
            { 
                options.Dataset = (DatasetType)int.Parse(args[1]);
                i += 2;
            }

            // set the defaults
            if (options.Dataset == DatasetType.Alphabet)
            {
                options.Vocabulary = "ABC";
                options.SequenceLength = 6;
                options.SuccessCriteria = 1.0f;
                options.LayerMultipliers = new int[] { 4 };
                options.LearningFactor = 0.0001f;
            }
            else if (options.Dataset == DatasetType.Shakespeare)
            {
                options.SequenceLength = 16;
                options.PercentOfText = 0.0001f;
                options.ShakespeareFilepath = "";
                options.TokenizerIterations = 0;
                options.NormalizeTokens = 0;
                options.SuccessCriteria = 0.95f;
                options.MinTrainSequenceLength = 1;
                options.LayerMultipliers = new int[] { 4, 4, 4, 4, 4, 4, 4, 4 };
                options.LearningFactor = 0.00001f;
            }

            for (; i < args.Length; i++)
            {
                var arg = args[i].ToLowerInvariant();
                switch (arg)
                {
                    case "-tr":
                    case "-trainingiterations":
                        if (++i >= args.Length) throw new ArgumentException("training iterations not specified");
                        options.TrainingIterations = int.Parse(args[i]);
                        break;
                    case "-it":
                    case "-iterations":
                        if (++i >= args.Length) throw new ArgumentException("iterations not specified");
                        options.IterationsPerTraining = int.Parse(args[i]);
                        break;
                    case "-in":
                    case "-inferences":
                        if (++i >= args.Length) throw new ArgumentException("inferences not specified");
                        options.Inferences = int.Parse(args[i]);
                        break;
                    case "-w":
                    case "-weightinitialization":
                        if (++i >= args.Length) throw new ArgumentException("weight initialization not specified");
                        options.WeightInitialization = (NeuralWeightInitialization)int.Parse(args[i]);
                        break;
                    case "-b":
                    case "-biasinitialization":
                        if (++i >= args.Length) throw new ArgumentException("bias initialization not specified");
                        options.BiasInitialization = (NeuralBiasInitialization)int.Parse(args[i]);
                        break;
                    case "-st":
                    case "-stoponnumsuccesses":
                        if (++i >= args.Length) throw new ArgumentException("stop on num successes not specified");
                        options.StopAfterNumSuccesses = int.Parse(args[i]);
                        break;
                    case "-su":
                    case "-successcriteria":
                        if (++i >= args.Length) throw new ArgumentException("success criteria not specified");
                        options.SuccessCriteria = float.Parse(args[i]);
                        break;
                    case "-vo":
                    case "-vocabulary":
                        if (++i >= args.Length) throw new ArgumentException("vocabulary not specified");
                        options.Vocabulary = args[i];
                        break;
                    case "-se":
                    case "-sequencelength":
                        if (++i >= args.Length) throw new ArgumentException("sequence length not specified");
                        options.SequenceLength = int.Parse(args[i]);
                        break;
                    case "-m":
                    case "-mintrainsequence":
                        if (++i >= args.Length) throw new ArgumentException("min train sequence length not specified");
                        options.MinTrainSequenceLength = int.Parse(args[i]);
                        break;
                    case "-la":
                    case "-layermultipliers":
                        if (++i >= args.Length) throw new ArgumentException("network layers not specified");
                        var layers = args[i].Split(',');
                        options.LayerMultipliers = new int[layers.Length];
                        for (var j = 0; j < layers.Length; j++)
                        {
                            options.LayerMultipliers[j] = int.Parse(layers[j]);
                        }
                        break;
                    case "-le":
                    case "-learningfactor":
                        if (++i >= args.Length) throw new ArgumentException("learning factor not specified");
                        options.LearningFactor = float.Parse(args[i]);
                        break;
                    case "-sh":
                    case "-shakespearefilepath":
                        if (++i >= args.Length) throw new ArgumentException("shakespeare filepath not specified");
                        options.ShakespeareFilepath = args[i];
                        break;
                    case "-p":
                    case "-percentoftext":
                        if (++i >= args.Length) throw new ArgumentException("percent of text not specified");
                        options.PercentOfText = float.Parse(args[i]);
                        break;
                    case "-to":
                    case "-tokenizeriterations":
                        if (++i >= args.Length) throw new ArgumentException("tokenizer iterations not specified");
                        options.TokenizerIterations = int.Parse(args[i]);
                        break;
                    case "-n":
                    case "-normalizetokens":
                        if (++i >= args.Length) throw new ArgumentException("normalize tokens not specified");
                        options.NormalizeTokens = int.Parse(args[i]);
                        break;
                    case "-ve":
                    case "-verbose":
                        options.Verbose = true;
                        break;
                    case "-h":
                    case "-help":
                        options.Help = true;
                        break;
                    default:
                        throw new ArgumentException($"unknown option '{arg}'");
                }
            }

            // check that a data set is provided and that iterations are valid, if not show help
            if (options.Dataset != DatasetType.Alphabet && options.Dataset != DatasetType.Shakespeare)
            {
                Console.WriteLine($"unknown dataset '{options.Dataset}'");
                options.Help = true;
            }
            if (options.TrainingIterations <= 0)
            {
                Console.WriteLine("training iterations must be greater than 0");
                options.Help = true;
            }
            if (options.IterationsPerTraining <= 0)
            {
                Console.WriteLine("iterations per training must be greater than 0");
                options.Help = true;
            }
            if (options.Inferences <= 0)
            {
                Console.WriteLine("inferences must be greater than 0");
                options.Help = true;
            }
            if (options.Dataset == DatasetType.Alphabet)
            {
                if (string.IsNullOrEmpty(options.Vocabulary))
                {
                    Console.WriteLine("vocabulary must be specified");
                    options.Help = true;
                }
                if (options.SequenceLength <= 0)
                {
                    Console.WriteLine("sequence length must be greater than 0");
                    options.Help = true;
                }
            }
            if (options.Dataset == DatasetType.Shakespeare)
            {
                if (options.PercentOfText <= 0.0f || options.PercentOfText > 1.0f)
                {
                    Console.WriteLine("percent of text must be between 0.0 and 1.0");
                    options.Help = true;
                }
                if (string.IsNullOrEmpty(options.ShakespeareFilepath) || !File.Exists(options.ShakespeareFilepath))
                {
                    Console.WriteLine("shakespeare filepath must be specified and exist");
                    options.Help = true;
                }
            }

            return options;
        }
    }
}
