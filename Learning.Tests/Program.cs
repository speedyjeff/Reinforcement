using Learning.Tests;
using System;
using System.Diagnostics;

namespace Learning
{
    public class LearningTests
    {
        public static void Main()
        {
            var timer = new Stopwatch();

            if (true)
            {
                Console.WriteLine("neural network tests...");
                timer.Start();
                {
                    NeuralNetworkTests.EndToEnd();
                    NeuralNetworkTests.HiddenLayers();
                    NeuralNetworkTests.Converge();
                    NeuralNetworkTests.ForceNaN();
                    NeuralNetworkTests.Perf();
                }
                timer.Stop();
                Console.WriteLine($"{timer.ElapsedMilliseconds} ms");
            }

            Console.WriteLine("neural network math tests...");
            NeuralNetworkMathTests.ReLuTest();
            NeuralNetworkMathTests.dOfReLuTest();
            NeuralNetworkMathTests.SoftmaxTest();
            NeuralNetworkMathTests.SoftmaxTest2();
            NeuralNetworkMathTests.DotFirstParamTTest();
            NeuralNetworkMathTests.DotSecondParamTTest();
            NeuralNetworkMathTests.SubtractTest();
            NeuralNetworkMathTests.SubtractTest2();
            NeuralNetworkMathTests.MultiplyTest();

            if (true)
            {
                Console.WriteLine("neural network math perf tests...");
                var iterations = 1_000_000;
                timer.Restart();
                {
                    NeuralNetworkMathTests.ReLuTestPerf(iterations);
                    NeuralNetworkMathTests.dOfReLuTestPerf(iterations);
                    NeuralNetworkMathTests.SoftmaxTestPerf(iterations);
                    NeuralNetworkMathTests.DotTestPerf(iterations);
                    NeuralNetworkMathTests.DotFirstParamTTestPerf(iterations);
                    NeuralNetworkMathTests.DotSecondParamTTestPerf(iterations);
                    NeuralNetworkMathTests.SubtractTestPerf(iterations);
                    NeuralNetworkMathTests.MultiplyTestPerf(iterations);
                    NeuralNetworkMathTests.MultiplyTestPerf2(iterations);
                }
                timer.Stop();
                Console.WriteLine($"{timer.ElapsedMilliseconds} ms");
            }
        }
    }
}