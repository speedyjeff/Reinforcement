using System;
using System.Collections.Generic;
using System.IO;

namespace Learning.Helpers
{
    public class OutcomeStats
    {
        public OutcomeStats(int maxLength = -1)
        {
            Data = new List<Details>();
            MaxLength = maxLength;
        }

        public void Add(int iteration, string context, int choice, float[] probabilities = null)
        {
            // retain the data
            Data.Add(new Details()
            {
                Iteration = iteration,
                Context = context,
                Choice = choice,
                Probabilities = probabilities
            });

            // drop the first added
            while (MaxLength > 0 && Data.Count >= MaxLength) Data.RemoveAt(0);
        }

        public void ToFile(string filename)
        {
            using (var output = File.CreateText(filename))
            {
                foreach (var d in Data)
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
        private int MaxLength;
        private List<Details> Data;
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
