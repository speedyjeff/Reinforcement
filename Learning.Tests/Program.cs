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
                    NeuralNetworkTests.Initalizations();
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
            NeuralNetworkMathTests.DotTest();
            NeuralNetworkMathTests.DotFirstParamTTest();
            NeuralNetworkMathTests.DotSecondParamTTest();
            NeuralNetworkMathTests.SubtractTest();
            NeuralNetworkMathTests.SubtractTest2();
            NeuralNetworkMathTests.MultiplyTest();
            NeuralNetworkMathTests.AddTest();

            Console.WriteLine("neural network math SIMD tests (large arrays)...");
            NeuralNetworkMathTests.ReLuTestLargeArray();
            NeuralNetworkMathTests.dOfReLuTestLargeArray();
            NeuralNetworkMathTests.SoftmaxTestLargeArray();
            NeuralNetworkMathTests.DotTestLargeArray();
            NeuralNetworkMathTests.DotFirstParamTTestLargeArray();
            NeuralNetworkMathTests.SubtractTestLargeArray();
            NeuralNetworkMathTests.MultiplyTestLargeArray();
            NeuralNetworkMathTests.AddTestLargeArray();

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
                    NeuralNetworkMathTests.AddTestPerf(iterations);
                }
                timer.Stop();
                Console.WriteLine($"{timer.ElapsedMilliseconds} ms");
            }

            Console.WriteLine("language model tests...");
            LanguageModelTokenizer.TestCreateText();
            LanguageModelTokenizer.TestCreateOptions();
            LanguageModelTokenizer.TestNormalization();
            LanguageModelTokenizer.TestIterations();
            LanguageModelTokenizer.TestRoundTrip();

            Console.WriteLine("tiny language model tests...");
            LanguageModelTiny.Converge();

            Console.WriteLine("deep q tests...");
            DeepQTests.EndToEnd();
        }
    }
}