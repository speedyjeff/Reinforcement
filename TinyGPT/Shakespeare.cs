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

            // create the model
            Model = new TinyLanguageModel(
                new NeuralOptions()
                {
                    InputNumber = options.SequenceLength,
                    OutputNumber = Tokenizer.Tokens.Count,
                    HiddenLayerNumber = hiddenLayerNum,
                    LearningRate = options.LearningFactor, // 0.00001f
                    MinibatchCount = 1,
                    ParallizeExecution = false,
                    WeightInitialization = options.WeightInitialization,
                    BiasInitialization = options.BiasInitialization
                },
                paddingToken: Tokenizer.Tokens[Tokenizer.Padding]);
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
                    var result = Model.Inference(tokens, ref top);
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

        public void InferStream(int length)
        {
            // start an inference with a random token and then continue to add tokens (and slide the window) until we hit length inferences
            var tokens = new List<int>();
            // get a random token to start with
            var start = (int)Math.Floor(GetRandom() * (Encoded.Count - Options.MinTokenCount));
            if (Options.StaticStartingPoint) start = 0;
            // add MinTokenCount tokens from start
            for(int i=0; i<Options.MinTokenCount; i++) tokens.Add(Encoded[start + i]);

            // display the token
            Console.Write($"'{Normalize(Tokenizer.Decode(tokens))}");

            // make an inference and add the result and repeat until length inferences
            for (int i = 0; i < length; i++)
            {
                // make sure that tokens.Count does not exceed the model input number
                if (tokens.Count > Options.SequenceLength) tokens.RemoveAt(0);

                // inference
                var result = Model.Inference(tokens);

                // display the token
                Console.Write($"{Normalize(Tokenizer.Decode(result))}");

                // add to the result
                tokens.Add(result);
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
