using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartPoleDriver
{
    class Options
    {

        public bool ShowHelp { get; set; }
        public AlgorithmType Algorithm { get; set; }
        public int Iterations { get; set; }
        public bool Quiet { get; set; }
        public int QPolicy { get; set; }
        public float NNSplit { get; set; }
        public float NNLearning { get; set; }
        public int[] NNHidden { get; set; }
        public int NNMinibatch { get; set; }
        public int AllIterations { get; set; }
        public int NNWeightInit { get; set; }
        public int NNBiasInit { get; set; }

        public void DisplayHelp()
        {
            Console.WriteLine("./cartpoledriver");
            Console.WriteLine("  -help               - this help");
            Console.WriteLine("  -quiet              - minimal/no output");
            Console.WriteLine("  -iterations         - number of iterations (default 1)");
            Console.WriteLine("  -algorithm [Manual|Random|Q|Neural|All] ");
            Console.WriteLine("                      - choose the model (default Manual)");
            Console.WriteLine("                        Manual - interactively try your best with left and right arrow to balance the pole");
            Console.WriteLine("                        Random - model randomly chooses right or left");
            Console.WriteLine("                        Neural - model uses a neural network to choose right or left");
            Console.WriteLine("                        All    - interactively try and see the output from the other models");
            Console.WriteLine("");
            Console.WriteLine("  -qpolicy [0,1,2,3]  - Q only: policy that Q uses to define the environment (default 0)");
            Console.WriteLine("  -nnsplit ###        - Neural only: split of iteratoins used to train v apply (default 0.8)");
            Console.WriteLine("  -nnlearning #.##    - Neural only: learning rate (default 0.15)");
            Console.WriteLine("  -nnhidden ##[,##,.] - Neural only: list of dimentions for the hidden layers (default 16,16)");
            Console.WriteLine("  -nnminibatch ###    - Neural only: mini batch size (default 100)");
            Console.WriteLine("  -nnweightinit ###     - Neural only: weight initialization (default 0)");
            Console.WriteLine("  -nnbiasinit ###       - Neural only: bias initialization (default 0)");
            Console.WriteLine("  -alliterations      - All only: the models are trained with iteration iterations, this is how many to watch (default 1)");
        }

        public static Options Parse(string[] args)
        {
            var options = new Options()
            {
                ShowHelp = false,
                Algorithm = AlgorithmType.Manual,
                Iterations = 1,
                AllIterations = 1,
                QPolicy = 0,
                NNSplit = 0.8f,
                NNLearning = 0.15f,
                NNHidden = new int[] {16,16},
                NNMinibatch = 100,
                NNWeightInit = 0,
                NNBiasInit = 0
            };

            for(int i=0; i<args.Length; i++)
            {
                if (args[i].StartsWith("-h", StringComparison.OrdinalIgnoreCase))
                {
                    options.ShowHelp = true;
                }
                else if (args[i].StartsWith("-alg", StringComparison.OrdinalIgnoreCase))
                {
                    if ((i + 1) < args.Length) options.Algorithm = (AlgorithmType)Enum.Parse(typeof(AlgorithmType), args[++i], ignoreCase: true);
                }
                else if (args[i].StartsWith("-qu", StringComparison.OrdinalIgnoreCase))
                {
                    options.Quiet = true;
                }
                else if (args[i].StartsWith("-i", StringComparison.OrdinalIgnoreCase))
                {
                    if ((i + 1) < args.Length) options.Iterations = Convert.ToInt32(args[++i]);
                }
                else if (args[i].StartsWith("-qp", StringComparison.OrdinalIgnoreCase))
                {
                    if ((i + 1) < args.Length) options.QPolicy = Convert.ToInt32(args[++i]);
                }
                else if (args[i].StartsWith("-nns", StringComparison.OrdinalIgnoreCase))
                {
                    if ((i + 1) < args.Length) options.NNSplit = Convert.ToSingle(args[++i]);
                }
                else if (args[i].StartsWith("-nnl", StringComparison.OrdinalIgnoreCase))
                {
                    if ((i + 1) < args.Length) options.NNLearning = Convert.ToSingle(args[++i]);
                }
                else if (args[i].StartsWith("-nnh", StringComparison.OrdinalIgnoreCase))
                {
                    if ((i + 1) < args.Length)
                    {
                        var parts = args[++i].Split(',');
                        options.NNHidden = new int[parts.Length];
                        for (int j = 0; j < parts.Length; j++) options.NNHidden[j] = Convert.ToInt32(parts[j]);
                    }
                }
                else if (args[i].StartsWith("-nnm", StringComparison.OrdinalIgnoreCase))
                {
                    if ((i + 1) < args.Length) options.NNMinibatch = Convert.ToInt32(args[++i]);
                }
                else if (args[i].StartsWith("-nnw", StringComparison.OrdinalIgnoreCase))
                {
                    if ((i + 1) < args.Length) options.NNWeightInit = Convert.ToInt32(args[++i]);
                }
                else if (args[i].StartsWith("-nnb", StringComparison.OrdinalIgnoreCase))
                {
                    if ((i + 1) < args.Length) options.NNBiasInit = Convert.ToInt32(args[++i]);
                }
                else if (args[i].StartsWith("-all", StringComparison.OrdinalIgnoreCase))
                {
                    if ((i + 1) < args.Length) options.AllIterations = Convert.ToInt32(args[++i]);
                }
                else
                {
                    Console.WriteLine($"unknown option : {args[i]}");
                }
            }

            if (options.Algorithm == AlgorithmType.None)
            {
                Console.WriteLine("an algorithm must be provided");
                options.ShowHelp = true;
            }
            if (options.QPolicy < 0 || options.QPolicy > 4)
            {
                Console.WriteLine("must provide a valid q policy");
                options.ShowHelp = true;
            }
            if (options.AllIterations < 0f) options.AllIterations = 0;
            if (options.NNSplit < 0f) options.NNSplit = 0f;
            if (options.NNSplit > 1.0f) options.NNSplit = 1f;
            if (options.NNLearning <= 0f)
            {
                Console.WriteLine("neural network learning must be positive");
                options.ShowHelp = true;
            }
            if (options.NNHidden.Length >= 0)
            {
                if (options.NNHidden.Length == 0)
                {
                    Console.WriteLine("must provide a list of hidden layers");
                    options.ShowHelp = true;
                }
                for(int j=0; j<options.NNHidden.Length; j++)
                {
                    if (options.NNHidden[j] <= 0)
                    {
                        Console.WriteLine($"the {j}th hidden layer must be positive and non-zero");
                        options.ShowHelp = true;
                    }
                }
            }

            return options;
        }
    }
}
