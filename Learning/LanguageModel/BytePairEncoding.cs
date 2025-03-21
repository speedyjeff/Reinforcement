using System;
using System.Collections.Generic;

namespace Learning.LanguageModel
{
    // https://en.wikipedia.org/wiki/Byte_pair_encoding
    class BytePairEncoding
    {
        // BPE is an iterative algorithm that merges the most frequent pair of bytes
        public static HashSet<string> ParseTokens(string text, TokenizerOptions options)
        {
            if (options.Iterations < 0) throw new ArgumentException("Iterations must be greater than zero");

            // convert chars to an array of normalized strings
            var input = new string[text.Length];
            for(int i=0; i<input.Length; i++)
            {
                var c = text[i];
                switch (options.Normalization)
                {
                    case TokenizerNormalization.Lowercase:
                        input[i] = $"{char.ToLower(c)}";
                        break;
                    case TokenizerNormalization.Uppercase:
                        input[i] = $"{char.ToUpper(c)}";
                        break;
                    case TokenizerNormalization.None:
                        input[i] = $"{c}";
                        break;
                    default: throw new ArgumentException("normalization unknown");
                }
            }

            // iterate the BPE algorithm
            var iteration = 0;
            while (iteration++ < options.Iterations)
            {
                // count the frequency of each pair of strings
                var pairFreq = CountPairFrequency(input);
                if (options.Verbose) Console.WriteLine($"{iteration} - number of pairs = {pairFreq.Count}");

                // get the most frequent pair and merge it into the input
                // favor the pair that has the shortest length
                var max = Int32.MinValue;
                var maxPair = "";
                var maxPairLength = Int32.MaxValue;
                foreach (var kvp in pairFreq)
                {
                    // greater than, or the same with a shorter length
                    if (kvp.Value > max || (kvp.Value == max && kvp.Key.Length < maxPairLength))
                    {
                        max = kvp.Value;
                        maxPair = kvp.Key;
                        maxPairLength = kvp.Key.Length;
                    }
                }

                // check if the algorithm should exit early
                if (max <= 1)
                {
                    if (options.Verbose) Console.WriteLine($"Exiting early at iteration {iteration}");
                    break;
                }

                // merge the pair in place
                MergePairInPlace(ref input, maxPair);
            }

            // get the frequency count of every string
            return GetUnique(input);
        }

        #region private
        private const string RemovedString = "\0";

        private static int SkipRemovedStrings(string[] input, int i)
        {
            while (i < input.Length && input[i] == RemovedString) i++;
            if (i >= input.Length) return -1;
            return i;
        }

        private static Dictionary<string, int> CountPairFrequency(string[] input)
        {
            var frequency = new Dictionary<string, int>();
            var i = 0;
            while (i < (input.Length - 1))
            {
                if (input[i] != RemovedString)
                {
                    // first pair is at input[i]
                    // skip over any removed strings
                    var j = SkipRemovedStrings(input, i + 1);
                    if (j < 0) break;
                    // second pair is at input[j]

                    // count the frequency of the pair
                    var pair = $"{input[i]}{input[j]}";
                    if (!frequency.ContainsKey(pair)) frequency.Add(pair, 1);
                    else frequency[pair]++;

                    // set i to j
                    i = j;
                }
                else
                {
                    // advance
                    i++;
                }
            }

            return frequency;
        }

        private static HashSet<string> GetUnique(string[] input)
        {
            var unique = new HashSet<string>();
            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] != RemovedString) unique.Add(input[i]);
            }

            return unique;
        }

        private static void MergePairInPlace(ref string[] input, string pair)
        {
            // replace instances of 'pair' (across 2 elements) with 'pair' and a removed string
            var i = 0;
            while (i < (input.Length - 1))
            {
                if (input[i] != RemovedString)
                {
                    // first pair is at input[i]
                    // skip over any removed strings
                    var j = SkipRemovedStrings(input, i + 1);
                    if (j < 0) break;
                    // second pair is at input[j]

                    // check if there is a match
                    if (pair.Equals($"{input[i]}{input[j]}"))
                    {
                        input[i] = pair;
                        input[j] = RemovedString;
                    }

                    // set i to j
                    i = j;
                }
                else
                {
                    // advance
                    i++;
                }
            }
        }
        #endregion
    }
}
