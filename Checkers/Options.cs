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
        public string PlayerWhite { get; private set; }
        public string PlayerBlack { get; private set; }
        public int MaxTurns { get; private set; }
        public string InitBoardFile { get; private set; }
        public bool RetainUniqueGames { get; private set; }

        public static void ShowHelp()
        {
            Console.WriteLine("./checkers [-dimensions #] [-reward #.#] [-learning #.#] [-discount #.#] [-help] [-showboards] [-showinsights] [-playerwhite C] [-playerblack C] [-iterations #] [-maxturns #] [-initboardfile <file>] [-retainunique]");
            Console.WriteLine("  -(dim)ensions   : [4-8]");
            Console.WriteLine("  -(rew)ard       : cost of taking an action (default -0.04)");
            Console.WriteLine("  -(l)earning     : 0 [no learning/rely on prior knowledge] ... [1] only most recent info (default 0.5)");
            Console.WriteLine("  -(dis)count     : 0 [short term rewards] ... 1 [long term rewards] (default 1)");
            Console.WriteLine("  -(h)elp         : show this help");
            Console.WriteLine("  -(showb)oards   : display the boards while iterating");
            Console.WriteLine("  -(showi)nsights : show the learning matrix for the computer");
            Console.WriteLine("  -(playerw)hite");
            Console.WriteLine("  -(playerb)lack  : specifier for which player type to choose (bypassing user input)");
            Console.WriteLine("  -(it)erations   : number of iterations (bypassing user input)");
            Console.WriteLine("  -(m)axturns     : max turns until the game is called cats (default 50)");
            Console.WriteLine("  -(in)itboardfile");
            Console.WriteLine("              : a file containing -wWbB indicating the initial placement of pieces");
            Console.WriteLine("                e.g.");
            Console.WriteLine("                  ---w----");
            Console.WriteLine("                  b-w---W-");
            Console.WriteLine("                  --------");
            Console.WriteLine("                  --b-----");
            Console.WriteLine("                  -b------");
            Console.WriteLine(" -(ret)ainunique : print a list of unique series of moves which games happened");
        }

        public static Options Parse(string[] args)
        {
            var options = new Options()
            {
                Dimension = 8,
                Reward = -0.04d,
                Learning = 0.5d,
                Discount = 1.0d,
                Help = false,
                ShowBoards = false,
                ShowInsights = false,
                PlayerWhite = "",
                PlayerBlack = "",
                Iterations = -1,
                MaxTurns = 50,
                InitBoardFile = ""
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
                else if (args[i].StartsWith("-l", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.Learning = Convert.ToDouble(args[++i]);
                }
                else if (args[i].StartsWith("-dis", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) options.Discount = Convert.ToDouble(args[++i]);
                }
                else if (args[i].StartsWith("-h", StringComparison.OrdinalIgnoreCase) || args[i].StartsWith("-?"))
                {
                    options.Help = true;
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
                else if (args[i].StartsWith("-m", StringComparison.OrdinalIgnoreCase))
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

            return options;
        }

    }
}
