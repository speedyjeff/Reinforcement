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
    }
}