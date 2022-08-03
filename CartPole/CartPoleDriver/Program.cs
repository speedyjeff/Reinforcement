using CartPole;
using System;

namespace CartPoleDriver
{
    public class CartPoleDriver
    {
        public static int Main(string[] args)
        {
            var options = Options.Parse(args);

            if (options.ShowHelp)
            {
                options.DisplayHelp();
                return -1;
            }

            // get the model
            IModel model = null;
            IModel[] otherModels = null;
            switch (options.Algorithm)
            {
                case AlgorithmType.Manual: model = new ManualModel(); break;
                case AlgorithmType.Random: model = new RandomModel(); break;
                case AlgorithmType.Q: model = new QModel((QStatePolicy)options.QPolicy); break;
                case AlgorithmType.Neural: 
                    model = new NeuralModel(
                        options.Iterations, 
                        options.NNSplit,
                        options.NNLearning,
                        options.NNHidden,
                        options.NNMinibatch); 
                    break;
                case AlgorithmType.All:
                    model = new ManualModel();
                    otherModels = new IModel[]
                    {
                        new RandomModel(),
                        new QModel((QStatePolicy)options.QPolicy),
                        new NeuralModel(
                            options.Iterations,
                            options.NNSplit,
                            options.NNLearning,
                            options.NNHidden,
                            options.NNMinibatch)
                    };
                    break;
                default: throw new Exception("unknown algorithm");
            }

            // if running All, train the models first
            if (otherModels != null && otherModels.Length > 0)
            {
                // train
                for(int i=0; i<otherModels.Length; i++)
                {
                    Console.WriteLine($"{otherModels[i].Type}");
                    options.Algorithm = otherModels[i].Type;
                    options.NNSplit = 1f; // make it all training
                    // will run Iterations times
                    Run(options, otherModels[i], others: null);
                }

                // fix up the options
                options.Algorithm = AlgorithmType.All;
                options.NNSplit = 0f; // make it no training
                options.Iterations = options.AllIterations;
            }

            // run with the choosen model
            Run(options, model, otherModels);

            return 0;
        }

        #region private
        private const int CountGoal = 200;

        private static void Run(Options options, IModel model, IModel[] others)
        {
            var cartpole = new CartPolePhysics();
            var stats = new Dictionary<int /*cartpole count*/, int /*count*/>();
            var actions = new Dictionary<CartPoleAction, int /*count*/>();

            // validate
            if (model == null || (options.Algorithm == AlgorithmType.All && others == null)) throw new Exception("invalid configuration");

            // run Iterations times
            for (int iteration = 0; iteration < options.Iterations; iteration++)
            {
                // show indicator
                if (!options.Quiet && 
                    (iteration % 10000 == 0 ||
                    (iteration % 1000 == 0 && options.Algorithm == AlgorithmType.Neural))) Console.Write(".");

                // for neural networks, show only the non-training data
                if (options.Algorithm == AlgorithmType.Neural &&
                    iteration == (int)(options.NNSplit * options.Iterations))
                {
                    stats.Clear();
                    actions.Clear();
                }

                // reset
                cartpole.Reset();

                // signal start
                model.StartIteration(cartpole.Mpole, cartpole.Mcart, cartpole.L * 2,
                    cartpole.Xmin, cartpole.Xmax,
                    cartpole.Thmin, cartpole.Thmax);

                while (!cartpole.IsDone)
                {
                    // dipaly the board (if necessary)
                    if (options.Algorithm == AlgorithmType.Manual ||
                        options.Algorithm == AlgorithmType.All) Display(cartpole);

                    // get everyone's choice
                    if (others != null && others.Length > 0)
                    {
                        for (int i = 0; i < others.Length; i++)
                        {
                            var action = others[i].MakeChoice(cartpole.State);
                            Console.Write($" {others[i].Type}:{action}");
                        }
                        Console.WriteLine();
                    }

                    // get a choice from the model
                    var dir = model.MakeChoice(cartpole.State);
                    cartpole.Step(dir);

                    // give feedback
                    model.EndChoice(cartpole.State, success: !cartpole.IsDone);

                    // capture choice
                    if (!actions.ContainsKey(dir)) actions.Add(dir, 1);
                    else actions[dir]++;
                }

                // signal end
                model.EndIteration(cartpole.Count);

                // capture stats about runs
                if (!stats.ContainsKey(cartpole.Count)) stats.Add(cartpole.Count, 1);
                else stats[cartpole.Count]++;
            }

            // display the stats
            if (!options.Quiet)
            {
                Console.WriteLine();

                var maxkey = 0;
                var count = 0;
                var sum = 0;
                var countOfGoal = 0;
                foreach (var kvp in stats)
                {
                    if (kvp.Key > maxkey) maxkey = kvp.Key;
                    count += kvp.Value;
                    sum += (kvp.Key*kvp.Value);
                    if (kvp.Key > CountGoal) countOfGoal += kvp.Value;
                }
                Console.WriteLine($"count : {count}");
                Console.WriteLine($"max   : {maxkey}");
                Console.WriteLine($"avg   : {(float)sum / (float)count:f2}");
                Console.WriteLine($">200  : {countOfGoal}");
                Console.WriteLine($"stat  : {model.Stat()}");
                Console.WriteLine("actions :");
                foreach (var kvp in actions.OrderByDescending(v => v.Value))
                {
                    Console.Write($" [{kvp.Key}:{kvp.Value}]");
                }
                Console.WriteLine();
                Console.WriteLine("runs :");
                foreach (var kvp in stats.OrderByDescending(v => v.Value))
                {
                    if (kvp.Key == maxkey) Console.Write($" **");
                    Console.Write($" [{kvp.Key}:{kvp.Value}]");
                }
                Console.WriteLine();
            }
        }

        private static void Display(CartPolePhysics system)
        {
            Console.WriteLine($"");
            Console.WriteLine($"[{system.Count}] L:{system.L:f2} Mass of cart:{system.Mcart:f2} Mass of pole:{system.Mpole:f2}");
            Console.WriteLine($"X  | {system.State.X:f4} u\t| {system.State.dX:f4} u/s\t [{system.Xmin},{system.Xmin}]");
            Console.WriteLine($"Th | {ToDegrees(system.State.Th):f4} d\t| {ToDegrees(system.State.dTh):f4} d/s\t [{ToDegrees(system.Thmin)} d,{ToDegrees(system.Thmax)} d]");
            Console.WriteLine($"Th | {system.State.Th:f4} r\t| {system.State.dTh:f4} r/s\t [{system.Thmin} r,{system.Thmax} r]");
        }

        private static float ToDegrees(float rad)
        {
            return rad * 180f / (float)Math.PI;
        }
        #endregion

    }
}