using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

using Learning;
using Learning.LanguageModel;

namespace TinyGPT
{
    struct ShakespeareOptions
    {
        public string InputFile = "";
        public float PercentOfText = 0.1f;
        public int TokenizerIterations = 10;
        public int SequenceLength = 32;
        public int MinTokenCount = 1;
        public bool StaticStartingPoint = false;
        public int[] HiddenLayers = null;
        public float LearningFactor = 0.00001f;
        public TokenizerNormalization NormalizeTokens = TokenizerNormalization.None;
        public NeuralWeightInitialization WeightInitialization = NeuralWeightInitialization.Random_Uniform_NegHalf_PosHalf;
        public NeuralBiasInitialization BiasInitialization = NeuralBiasInitialization.Random_Uniform_NegHalf_PosHalf;
        public float Temperature = 0.8f;
        public int TopK = 10;
        public int RepetitionPenaltyWindow = 20;
        public string Prompt = "";
        public int StreamLength = 1000;

        public ShakespeareOptions() { }
    }

    class Stats
    {
        public Stats(int sequenceLength, int topLength)
        {
            InferenceCorrect = new long[sequenceLength];
            InferenceCount = new long[sequenceLength];
            TopNCorrect = new long[topLength];
        }

        public long[] InferenceCorrect;
        public long[] InferenceCount;
        public long[] TopNCorrect;
    }

    class Shakespeare
    {
        public Shakespeare(ShakespeareOptions options)
        {
            // init
            Options = options;
            Rand = RandomNumberGenerator.Create();
            Debug_InferenceStats = new Stats(options.SequenceLength, topLength: TopN);

            // read in text from the Shakespeare text
            var text = File.ReadAllText(options.InputFile);

            // truncate the text if requested
            if (options.PercentOfText > 0 && options.PercentOfText < 1)
            {
                var length = (int)(text.Length * options.PercentOfText);
                var start = (int)Math.Floor(GetRandom() * (text.Length - length));
                text = text.Substring(start, length);
            }

            Console.WriteLine($"Text Length: {text.Length}");

            Console.WriteLine($"Text: '{Normalize(text)}'");

            // create the tokens
            Tokenizer = Tokenizer.Create(
                text,
                new TokenizerOptions()
                {
                    Iterations = options.TokenizerIterations,
                    Normalization = options.NormalizeTokens,
                    DefaultVocab = TokenizerDefaultVocab.Padding
                });

            Console.WriteLine($"Tokens: {Tokenizer.Tokens.Count}");

            // encode the text
            Encoded = Tokenizer.Encode(text);

            // create the hidden layers
            var hiddenLayerNum = new int[]
                {
                    Tokenizer.Tokens.Count * 4,
                    Tokenizer.Tokens.Count * 4,
                    Tokenizer.Tokens.Count * 4,
                    Tokenizer.Tokens.Count * 4,
                    Tokenizer.Tokens.Count * 4,
                    Tokenizer.Tokens.Count * 4,
                    Tokenizer.Tokens.Count * 4,
                    Tokenizer.Tokens.Count * 4
                };
            if (options.HiddenLayers != null && options.HiddenLayers.Length > 0)
            {
                // user defined
                hiddenLayerNum = new int[options.HiddenLayers.Length];
                for (int i = 0; i < options.HiddenLayers.Length; i++)
                {
                    if (options.HiddenLayers[i] <= 0) throw new ArgumentException($"Hidden layer {i} must be greater than 0");
                    hiddenLayerNum[i] = Tokenizer.Tokens.Count * options.HiddenLayers[i];
                }
            }

            var tokenCount = Tokenizer.Tokens.Count;
            var inputSize = options.SequenceLength * tokenCount;

            // create the model
            Model = new TinyLanguageModel(
                new NeuralOptions()
                {
                    InputNumber = inputSize,
                    OutputNumber = tokenCount,
                    HiddenLayerNumber = hiddenLayerNum,
                    LearningRate = options.LearningFactor,
                    MinibatchCount = 1,
                    ParallizeExecution = false,
                    WeightInitialization = options.WeightInitialization,
                    BiasInitialization = options.BiasInitialization
                },
                paddingToken: Tokenizer.Tokens[Tokenizer.Padding],
                tokenCount: tokenCount);

            Console.WriteLine($"Network: input={inputSize} hidden=[{string.Join(",", hiddenLayerNum)}] output={tokenCount}");
        }

        public Stats Debug_InferenceStats;

        public void Train(int iterations)
        {
            Train(Model, iterations);
        }

        public void TrainParallel(int iterations, int fitnessInferences, int instances = 1)
        {
            // split the model for training
            var models = new TinyLanguageModel[instances];
            for (int i = 0; i < models.Length; i++) models[i] = Model.Copy();

            // train the model in parallel
            var fitness = new float[instances];
            Parallel.For(0, instances, i =>
            {
                // train
                Train(models[i], iterations);
                // inference (for fitness)
                fitness[i] = (float)Inference(fitnessInferences, verbose: false);
            });

            // merge the models
            Model = TinyLanguageModel.Merge(models,
                new NeuralMergeOptions()
                {
                    Method = NeuralMergeMethod.WeightedAverage,
                    Fitness = fitness,
                    MutationProbability = 0.01f,
                    MutationStrength = 0.05f
                });
        }

        public float Inference(int iterations, bool verbose = false)
        {
            var correctCount = 0;
            var totalCount = 0;
            for (int i = 0; i < iterations; i++)
            {
                // get a random start to a sequence
                var start = (int)Math.Floor(GetRandom() * (Encoded.Count - Options.SequenceLength - 1));
                if (Options.StaticStartingPoint) start = 0;

                // get the sequence
                var tokens = new List<int>(Options.SequenceLength);
                
                for(int j=0; j<Options.MinTokenCount; j++) tokens.Add(Encoded[start + j]);

                // test the model
                var top = new int[TopN];
                for (int j = (Options.MinTokenCount-1); j < Options.SequenceLength; j++)
                {
                    var output = Model.Inference(tokens);
                    var result = output.Result;
                    FillTopResults(output.Probabilities, top);
                    var correct = Encoded[start + j + 1];

                    // check if any of the top results are correct
                    for (int k = 0; k < top.Length; k++)
                    {
                        if (top[k] == correct)
                        {
                            Debug_InferenceStats.TopNCorrect[k]++;
                            break;
                        }
                    }

                    // show progress
                    if (verbose)
                    {
                        Console.Write($"Input: '{Normalize(Tokenizer.Decode(tokens))}'");
                        Console.Write(" Output:");
                        for (int k = 0; k < top.Length; k++) Console.Write($" [{k}]'{Normalize(Tokenizer.Decode(top[k]))}'");
                        Console.WriteLine($" {(correct == result)}");
                    }

                    // test if correct
                    if (result == correct)
                    {
                        Debug_InferenceStats.InferenceCorrect[j]++;
                        correctCount++;
                    }
                    Debug_InferenceStats.InferenceCount[j]++;
                    totalCount++;

                    // add the next correct token
                    tokens.Add(correct);
                }
            }

            if (totalCount == 0) return 0f;
            return (float)correctCount / (float)totalCount;
        }

        public void InferStream(int length = 1000, string prompt = null)
        {
            var tokens = new List<int>();

            // seed from a user-provided prompt or a random corpus position
            if (!string.IsNullOrEmpty(prompt))
            {
                try
                {
                    tokens = Tokenizer.Encode(prompt);
                    while (tokens.Count > Options.SequenceLength) tokens.RemoveAt(0);
                }
                catch
                {
                    Console.WriteLine("prompt contains characters not in vocabulary, fall back to random seed");
                    tokens.Clear();
                }
            }

            if (tokens.Count == 0)
            {
                // seed with a full context window for best prediction quality
                var seedLength = Math.Min(Options.SequenceLength, Encoded.Count);
                var start = 0;
                if (!Options.StaticStartingPoint) start = (int)Math.Floor(GetRandom() * (Encoded.Count - seedLength));
                for (int i = 0; i < seedLength; i++) tokens.Add(Encoded[start + i]);
            }

            // display the seed
            Console.Write($"'{Normalize(Tokenizer.Decode(tokens))}");

            // track recently generated tokens for repetition penalty
            var recentTokens = new Queue<int>();

            // generate tokens one at a time
            for (int i = 0; i < length; i++)
            {
                // slide the window to stay within the context length
                while (tokens.Count > Options.SequenceLength) tokens.RemoveAt(0);

                // get temperature-scaled probability distribution from the model
                var probs = Model.Inference(tokens, Options.Temperature).Probabilities;

                // sample from top-K with repetition penalty
                var result = SampleWithProbabilities(probs, Options.TopK, recentTokens);

                // display and accumulate
                Console.Write($"{Normalize(Tokenizer.Decode(result))}");
                tokens.Add(result);

                // update repetition tracking
                if (Options.RepetitionPenaltyWindow > 0)
                {
                    recentTokens.Enqueue(result);
                    while (recentTokens.Count > Options.RepetitionPenaltyWindow) recentTokens.Dequeue();
                }
            }
            Console.WriteLine("'");
        }

        #region private
        private Tokenizer Tokenizer;
        private List<int> Encoded;
        private TinyLanguageModel Model;
        private ShakespeareOptions Options;
        private RandomNumberGenerator Rand;
        private const int TopN = 4;

        private static void FillTopResults(float[] probabilities, int[] topResults)
        {
            if (topResults == null || topResults.Length == 0) return;

            var maxes = new float[topResults.Length];
            for (int i = 0; i < maxes.Length; i++)
            {
                maxes[i] = Single.MinValue;
                topResults[i] = -1;
            }

            for (int i = 0; i < probabilities.Length; i++)
            {
                for (int j = 0; j < maxes.Length; j++)
                {
                    if (probabilities[i] > maxes[j])
                    {
                        for (int k = maxes.Length - 1; k > j; k--)
                        {
                            topResults[k] = topResults[k - 1];
                            maxes[k] = maxes[k - 1];
                        }
                        topResults[j] = i;
                        maxes[j] = probabilities[i];
                        break;
                    }
                }
            }
        }

        // Selects a token by applying a repetition penalty to recently used tokens,
        // extracting the top-K highest-probability candidates, then either returning
        // the best candidate (greedy) or sampling from the normalized top-K distribution.
        // NOTE: this method modifies the probs array in place.
        private int SampleWithProbabilities(float[] probs, int topK, Queue<int> recentTokens)
        {
            topK = Math.Clamp(topK, 1, probs.Length);

            // apply repetition penalty in place
            if (recentTokens != null && recentTokens.Count > 0)
            {
                foreach (var recent in recentTokens)
                {
                    if (recent >= 0 && recent < probs.Length)
                        probs[recent] *= 0.1f;
                }
            }

            // find top-K indices
            var topIndices = new int[topK];
            var topProbs = new float[topK];
            for (int i = 0; i < topK; i++) topProbs[i] = Single.MinValue;

            for (int i = 0; i < probs.Length; i++)
            {
                for (int j = 0; j < topK; j++)
                {
                    if (probs[i] > topProbs[j])
                    {
                        // shift lower entries down
                        for (int k = topK - 1; k > j; k--)
                        {
                            topIndices[k] = topIndices[k - 1];
                            topProbs[k] = topProbs[k - 1];
                        }
                        topIndices[j] = i;
                        topProbs[j] = probs[i];
                        break;
                    }
                }
            }

            // greedy mode
            if (Options.Temperature <= 0.01f) return topIndices[0];

            // normalize top-K probabilities and sample
            var sum = 0f;
            for (int i = 0; i < topK; i++) sum += Math.Max(topProbs[i], 0f);
            if (sum <= 0f) return topIndices[0];

            var roll = GetRandom() * sum;
            var cumulative = 0f;
            for (int i = 0; i < topK; i++)
            {
                cumulative += Math.Max(topProbs[i], 0f);
                if (roll <= cumulative) return topIndices[i];
            }
            return topIndices[0];
        }

        private void Train(TinyLanguageModel model, int iterations)
        {
            // create a local copy of the tokens (+1 to include the last correct reply)
            var length = Options.SequenceLength + 1;
            var localTokens = new List<int>();
            for (int i = 0; i < length; i++) localTokens.Add(0);

            // train the model
            while (iterations-- > 0)
            {
                // get starting position
                var start = (int)Math.Floor(GetRandom() * (Encoded.Count - length));
                if (Options.StaticStartingPoint) start = 0;

                // get a local copy of these tokens
                for (int i = 0; i < localTokens.Count; i++) localTokens[i] = Encoded[start + i];

                // train the model
                model.Train(localTokens, Options.MinTokenCount);
            }
        }

        private float GetRandom()
        {
            Span<byte> int32buffer = stackalloc byte[4];
            Rand.GetNonZeroBytes(int32buffer);
            // ensure positive
            int32buffer[3] &= 0x7f;
            var number = BitConverter.ToInt32(int32buffer);
            // get a random float between 0.0 and 1.0
            return ((float)number / (float)Int32.MaxValue);
        }

        private static string Normalize(string text)
        {
            return text.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }
        #endregion
    }
}
