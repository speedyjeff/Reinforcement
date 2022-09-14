using System;
using System.Collections.Generic;
using System.IO;

namespace TicTacToe
{
    internal class NeuralNetworkComputerStats
    {
        public NeuralNetworkComputerStats()
        {
            All = new List<Details>();
        }

        public void Add(int iteration, string context, int choice, float[] probabilities = null)
        {
            All.Add(new Details()
            {
                Iteration = iteration,
                Context = context,
                Choice = choice,
                Probabilities = probabilities
            });
        }

        public void ToFile(string filename)
        {
            using (var output = File.CreateText(filename))
            {
                foreach (var d in All)
                {
                    output.Write($"{d.Iteration}\t{d.Context}\t{d.Choice}");
                    if (d.Probabilities != null)
                    {
                        for (int i = 0; i < d.Probabilities.Length; i++) output.Write($"\t{d.Probabilities[i]:f2}");
                    }
                    output.WriteLine();
                }
            }
        }

        #region private
        private List<Details> All;
        private struct Details
        {
            public int Iteration;
            public string Context;
            public int Choice;
            public float[] Probabilities;
        }
        #endregion
    }
}
