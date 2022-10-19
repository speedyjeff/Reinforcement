using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    class Options
    {
        public int Dimension { get; private set; }
        public double Reward { get; private set; }
        public double Learning { get; private set; }
        public double Discount { get; private set; }
        public bool Help { get; private set; }
        public bool ShowBoards { get; private set; }
        public bool ShowInsights { get; private set; }
        public int Iterations { get; private set; }
        public bool Quiet { get; private set; }
        public string PlayerWhite { get; private set; }
        public string PlayerBlack { get; private set; }
        public int MaxTurns { get; private set; }
        public string InitBoardFile { get; private set; }
        public bool RetainUniqueGames { get; private set; }
        public float MaxTrainingPct { get; private set; }
        public bool NoLearning { get; private set; }
        public bool DumpStats { get; private set; }
        public int[] LearningStyle { get; private set; }
        public int TrainingStyle { get; private set; }
        public int[] LearningSituations { get; private set; }
        public int Seed { get; private set; }

        public static void ShowHelp()
        {
            Console.WriteLine("./checkers [-dimensions #] [-reward #.#] [-learning #.#] [-discount #.#] [-help] [-showboards] [-showinsights] [-playerwhite C] [-playerblack C] [-iterations #] [-maxturns #] [-initboardfile <file>] [-retainunique]");
            Console.WriteLine("  -(dim)ensions   : [4-8]");
            Console.WriteLine("  -(h)elp         : show this help");
            Console.WriteLine("  -(q)uiet        : provide minimal output");
            Console.WriteLine("  -(seed)         : seed for psuedo random generators (default not set)");
            Console.WriteLine("  -(showb)oards   : display the boards while iterating");
            Console.WriteLine("  -(showi)nsights : show the learning matrix for the computer");
            Console.WriteLine("  -(playerw)hite");
            Console.WriteLine("  -(playerb)lack  : specifier for which player type to choose (bypassing user input)");
            Console.WriteLine("  -(it)erations   : number of iterations (bypassing user input)");
            Console.WriteLine("  -(maxtu)rns     : max turns until the game is called cats (default 50)");
            Console.WriteLine("  -(in)itboardfile");
            Console.WriteLine("              : a file containing -wWbB indicating the initial placement of pieces");
            Console.WriteLine("                e.g.");
            Console.WriteLine("                  ---w----");
            Console.WriteLine("                  b-w---W-");
            Console.WriteLine("                  --------");
            Console.WriteLine("                  --b-----");
            Console.WriteLine("                  -b------");
            Console.WriteLine(" -(ret)ainunique : print a list of unique series of moves which games happened");
            Console.WriteLine(" Q Learning");
            Console.WriteLine("  -(rew)ard      : cost of taking an action (default -0.1)");
            Console.WriteLine("  -(learni)ng    : 0 [no learning/rely on prior knowledge] ... [1] only most recent info (default 0.2)");
            Console.WriteLine("  -(dis)count    : 0 [short term rewards] ... 1 [long term rewards] (default 0.1)");
            Console.WriteLine(" Neural Network learning");
            Console.WriteLine("  -(maxtr)ainpct : maximum training percentage (0.0-1.0) (default 0.5)");
            Console.WriteLine("  -(n)olearning  : no learning (default false)");
            Console.WriteLine("  -(dum)pstats   : dump the training data (recommended not to go beyond 5k iterations)");
            Console.WriteLine("  -(learnst)yle #[,#]");
            Console.WriteLine("  -(learnsi)tuations  #[,#]");
            Console.WriteLine("                 : these values represent configuration for [white,black] (or if a single number, both)");
            Console.WriteLine("                 : learning styles [pos for succes: 1, neg for cats: 2, neg for loss: 4] (can be added to gether) (default: 1,1)");
            Console.WriteLine("                 : learning situations [neg for loss of piece: 1, pos for king: 2, pos for capture: 4, neg for oscillation: 8] (default: 12,12)");
            Console.WriteLine("  -(t)rainstyle  : training styles [random: 1, predictable: 2] (these cannot be combined) (default: 2)");
        }

        public static Options Parse(string[] args)
        {
            var options = new Options()
            {
                Dimension = 8,
                Reward = -0.1d,
                Learning = 0.2d,
                Discount = 0.1d,
                Help = false,
                Quiet = false,
                ShowBoards = false,
                ShowInsights = false,
                PlayerWhite = "",
                PlayerBlack = "",
                Iterations = -1,
                MaxTurns = 50,
                InitBoardFile = "",
                MaxTrainingPct = 0.5f,
                NoLearning = false,
                DumpStats = false,
                LearningStyle = new int[] {1,1 },
                LearningSituations = new int[] { 12, 12 },
                TrainingStyle = 2,
                Seed = 0,
            };

            for(int i=0; i<args.Length; i++)
            {
                if (args[i].StartsWith("-dim", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.Dimension = Convert.ToInt32(args[++i]);
                }
                else if (args[i].StartsWith("-rew", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.Reward = Convert.ToDouble(args[++i]);
                }
                else if (args[i].StartsWith("-learni", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.Learning = Convert.ToDouble(args[++i]);
                }
                else if (args[i].StartsWith("-dis", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.Discount = Convert.ToDouble(args[++i]);
                }
                else if (args[i].StartsWith("-seed", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.Seed = Convert.ToInt32(args[++i]);
                }
                else if (args[i].StartsWith("-h", StringComparison.OrdinalIgnoreCase) || args[i].StartsWith("-?"))
                {
                    options.Help = true;
                }
                else if (args[i].StartsWith("-q", StringComparison.OrdinalIgnoreCase))
                {
                    options.Quiet = true;
                }
                else if (args[i].StartsWith("-showb", StringComparison.OrdinalIgnoreCase))
                {
                    options.ShowBoards = true;
                }
                else if (args[i].StartsWith("-showi", StringComparison.OrdinalIgnoreCase))
                {
                    options.ShowInsights = true;
                }
                else if (args[i].StartsWith("-playerw", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.PlayerWhite = args[++i];
                }
                else if (args[i].StartsWith("-playerb", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.PlayerBlack = args[++i];
                }
                else if (args[i].StartsWith("-it", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.Iterations = Convert.ToInt32(args[++i]);
                }
                else if (args[i].StartsWith("-maxtu", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.MaxTurns = Convert.ToInt32(args[++i]);
                }
                else if (args[i].StartsWith("-in", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.InitBoardFile = args[++i];
                }
                else if (args[i].StartsWith("-ret", StringComparison.OrdinalIgnoreCase))
                {
                    options.RetainUniqueGames = true;
                }
                else if (args[i].StartsWith("-n", StringComparison.OrdinalIgnoreCase))
                {
                    options.NoLearning = true;
                }
                else if (args[i].StartsWith("-maxtr", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.MaxTrainingPct = Convert.ToSingle(args[++i]);
                }
                else if (args[i].StartsWith("-dum", StringComparison.OrdinalIgnoreCase) || args[i].StartsWith("-?"))
                {
                    options.DumpStats = true;
                }
                else if (args[i].StartsWith("-learnst", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.LearningStyle = ParseInt32Array(args[++i]);
                }
                else if (args[i].StartsWith("-learnsi", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.LearningSituations = ParseInt32Array(args[++i]);
                }
                else if (args[i].StartsWith("-t", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.TrainingStyle = Convert.ToInt32(args[++i]);
                }
                else
                {
                    Console.WriteLine($"Unknown option : {args[i]}");
                }
            }

            if (options.Dimension < 4 || options.Dimension > 8) options.Help = true;
            if (options.Learning < 0 || options.Learning > 1) options.Help = true;
            if (options.Discount < 0 || options.Discount > 1) options.Help = true;
            if (options.Reward < -1 || options.Reward >= 0) options.Help = true;
            if (options.MaxTurns <= 0) options.Help = true;
            if (options.MaxTrainingPct < 0) options.MaxTrainingPct = 0f;
            if (options.MaxTrainingPct > 1) options.MaxTrainingPct = 1f;
            if (options.LearningSituations == null || options.LearningSituations.Length != 2)
            {
                Console.WriteLine("must pass in valid learning situations");
                options.Help = true;
            }
            if (options.LearningStyle == null || options.LearningStyle.Length != 2)
            {
                Console.WriteLine("must pass in valid learning style");
                options.Help = true;
            }

            return options;
        }

        #region private
        private static int[] ParseInt32Array(string text)
        {
            // formats:
            //   #
            //   #,#
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            else if (Int32.TryParse(text, out int value))
            {
                // single value, apply to both
                return new int[] { value, value };
            }
            else if (text.Contains(','))
            {
                // split and parse
                var parts = text.Split(',');
                if (parts.Length < 2) return null;
                return new int[]
                {
                    Convert.ToInt32(parts[0]),
                    Convert.ToInt32(parts[1])
                };
            }
            else
            {
                return null;
            }
        }
        #endregion

    }
}
