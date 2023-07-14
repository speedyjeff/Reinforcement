using System;

namespace TeeGame
{
    public enum PlayerType { Human = 0, QLearn = 1, NeuralNetwork = 2 };

    class Options
    {
        public bool Help { get; set; }
        public int Iterations { get; set; }
        public PlayerType PlayerType { get; set; }
        public bool IsQuiet { get; set; }
        public string SankeyInput { get; set; }
        public int SankeyLimit { get; set; }

        public static void DisplayHelp()
        {
            Console.WriteLine("./teegame");
            Console.WriteLine("  (-h)elp       - display help");
            Console.WriteLine("  (-i)terations - number of iterations (default 1)");
            Console.WriteLine("  (-p)layer     - 0 = human (default), 1 = QLearning, 2 = NeuralNetwork");
            Console.WriteLine("  (-q)uiet      - minimize output");
            Console.WriteLine(" input files for Sankey data:");
            Console.WriteLine("  (-s)ankey     - file that contains the 'moves: f-a ' data");
            Console.WriteLine("  (-l)imit      - limit the number of data inputs to read (default 500)");
        }

        public static Options Parse(string[] args)
        {
            var options = new Options()
            {
                Help = false,
                Iterations = 1,
                PlayerType = PlayerType.Human,
                IsQuiet = false,
                SankeyInput = "",
                SankeyLimit = 500
            };

            for(int i=0; i<args.Length; i++)
            {
                if (args[i].StartsWith("-h", StringComparison.OrdinalIgnoreCase))
                {
                    options.Help = true;
                }
                else if (args[i].StartsWith("-i", StringComparison.OrdinalIgnoreCase))
                {
                    if (++i < args.Length) options.Iterations = Convert.ToInt32(args[i]);
                }
                else if (args[i].StartsWith("-p", StringComparison.OrdinalIgnoreCase))
                {
                    if (++i < args.Length) options.PlayerType = (PlayerType)Convert.ToInt32(args[i]);
                }
                else if (args[i].StartsWith("-q", StringComparison.OrdinalIgnoreCase))
                {
                    options.IsQuiet = true;
                }
                else if (args[i].StartsWith("-s", StringComparison.OrdinalIgnoreCase))
                {
                    if (++i < args.Length) options.SankeyInput = args[i];
                }
                else if (args[i].StartsWith("-l", StringComparison.OrdinalIgnoreCase))
                {
                    if (++i < args.Length) options.SankeyLimit = Convert.ToInt32(args[i]);
                }
                else
                {
                    Console.WriteLine($"unknown option : {args[i]}");
                }
            }

            if (options.Iterations <= 0)
            {
                Console.WriteLine("invalid iterations");
                options.Help = true;
            }
            if (options.PlayerType != PlayerType.Human && options.PlayerType != PlayerType.QLearn && options.PlayerType != PlayerType.NeuralNetwork)
            {
                Console.WriteLine("invalid player");
                options.Help = true;
            }
            if (!string.IsNullOrWhiteSpace(options.SankeyInput) && !File.Exists(options.SankeyInput))
            {
                Console.WriteLine("sankey input file not found");
                options.Help = true;
            }
            if (options.SankeyLimit < 0)
            {
                Console.WriteLine("must be a positive limit");
                options.Help = true;
            }

            return options;
        }
    }
}
