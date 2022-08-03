using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartPole.Tests
{
    internal class NeuralTests
    {
        public static void Boundaries()
        {
             var network = Learning.NeuralNetwork.Load(@"neural16x16.txt");

            // boundaries
            //     | Min     | Max
            // -----------------------
            //  X  | -2.4    | 2.4
            // dX  | -Inf    | Inf
            //  Th | -0.2094 | 0.2094
            // dTh | -Inf    | Inf

            var stats = new Dictionary<CartPoleAction, int>()
            {
                { CartPoleAction.Left, 0 },
                { CartPoleAction.Right, 0 }
            };
            var input = new float[4];

            for(var X = -2.4f; X <= 2.4f; X+=1f)
            {
                for(var dX = -1f; dX <= 1f; dX+=0.5f)
                {
                    for(var Th = -0.209f; Th <= 0.209f; Th+=0.1f)
                    {
                        for(var dTh = -2f; dTh <= 2f; dTh+=1f)
                        {
                            input[0] = X;
                            input[1] = dX;
                            input[2] = Th;
                            input[3] = dTh;

                            var output = network.Evaluate(input);
                            stats[(CartPoleAction)output.Result]++;

                            Console.WriteLine($"{X:f2} {dX:f2} {Th:f2} {dTh:f2} {output.Result}");
                        }
                    }
                }
            }

            foreach(var kvp in stats)
            {
                Console.WriteLine($"{kvp.Key}\t{kvp.Value}");
            }

            // validate
            if (stats[CartPoleAction.Left] < 285 || stats[CartPoleAction.Left] > 295) throw new Exception("invalid lefts");
            if (stats[CartPoleAction.Right] < 330 || stats[CartPoleAction.Right] > 335) throw new Exception("invalid rights");
        }
    }
}
