using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using Learning;
using Learning.LanguageModel;

namespace TinyGPT
{
    struct AlphabetizeOptions
    {
        public string Vocabulary = "";
        public int SequenceLength = 0;
        public int[] HiddenLayers = null;
        public float LearningFactor = 0.0001f;
        public NeuralWeightInitialization WeightInitialization = NeuralWeightInitialization.Random_Uniform_NegHalf_PosHalf;
        public NeuralBiasInitialization BiasInitialization = NeuralBiasInitialization.Random_Uniform_NegHalf_PosHalf;

        public AlphabetizeOptions() { }
    }

    class Alphabetize
    {
        // the input is a series of 6 characters in  ['A', 'B', or 'C'] and
        // the model will sort the characters alphabetically (by predicting the next character that has it in alphabetical order)
        public Alphabetize(AlphabetizeOptions options)
        {
            // init
            Rand = RandomNumberGenerator.Create();
            SequenceLength = options.SequenceLength;
            Vocabulary = options.Vocabulary.ToCharArray();

            // create the tokenizer
            Tokenizer = Tokenizer.Create(
                new string(Vocabulary),
                new TokenizerOptions()
                {
                    Iterations = 0,
                    Normalization = TokenizerNormalization.None,
                    DefaultVocab = TokenizerDefaultVocab.Padding
                });

            // create the hidden layers
            var hiddenLayerNum = new int[] { SequenceLength * 4 }; 
            if (options.HiddenLayers != null && options.HiddenLayers.Length > 0)
            {
                // user defined
                hiddenLayerNum = new int[options.HiddenLayers.Length];
                for (int i = 0; i < options.HiddenLayers.Length; i++)
                {
                    if (options.HiddenLayers[i] <= 0) throw new ArgumentException($"Hidden layer {i} must be greater than 0");
                    hiddenLayerNum[i] = SequenceLength * options.HiddenLayers[i];
                }
            }

            // create the tinyLanguageModel
            Model = new TinyLanguageModel(
                new NeuralOptions()
                {
                    InputNumber = SequenceLength * 2, // the sequence and the correct output
                    OutputNumber = Vocabulary.Length,
                    HiddenLayerNumber = hiddenLayerNum,
                    LearningRate = options.LearningFactor,  //0.0001f
                    MinibatchCount = 1,
                    ParallizeExecution = true,
                    WeightInitialization = options.WeightInitialization,
                    BiasInitialization = options.BiasInitialization
                },
                paddingToken: Tokenizer.Tokens[Tokenizer.Padding]);
        }

        public void Train(int iterations)
        {
            for(int i = 0; i<iterations; i++)
            {
                // get a random sequence
                var sequence = GetRandomSequence(SequenceLength);

                // build the string along with the correct output
                var sb = new StringBuilder();
                sb.Append(sequence);
                Array.Sort(sequence);
                sb.Append(sequence);

                // tokenize the input
                var tokens = Tokenizer.Encode(sb.ToString());

                // train the model
                Model.Train(tokens, minTokenCount: SequenceLength);
            }
        }

        public int Inference(int iterations, bool verbose = false)
        {
            var correctCount = 0;
            for (int i = 0; i < iterations; i++)
            {
                // get a random sequence
                var sequence = GetRandomSequence(SequenceLength);
                if (verbose) Console.Write($"{new string(sequence)}\t");

                // tokenize the input
                var tokens = Tokenizer.Encode(new string(sequence));

                // get the correct output
                Array.Sort(sequence);
                var correct = Tokenizer.Encode(new string(sequence));

                // test the model
                var answer = new List<int>();
                for (int j=0; j<SequenceLength; j++)
                {
                    var result = Model.Inference(tokens);
                    if (verbose) Console.WriteLine($"Current: {Tokenizer.Decode(tokens)} Result: {Tokenizer.Decode(result)}");
                    tokens.Add(result);
                    answer.Add(result);
                }

                // check the result
                if (verbose) Console.Write($"{Tokenizer.Decode(answer)}\t{Tokenizer.Decode(correct)}\t");
                var match = true;
                for (int j = 0; j < correct.Count; j++)
                {
                    if (correct[j] != tokens[tokens.Count - SequenceLength + j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) correctCount++;
                if (verbose) Console.WriteLine(match ? "true" : "false");
            }

            return correctCount;
        }

        #region private
        private char[] Vocabulary;
        private RandomNumberGenerator Rand;
        private Tokenizer Tokenizer;
        private TinyLanguageModel Model;
        private int SequenceLength;

        private char[] GetRandomSequence(int length)
        {
            var sequence = new char[length];
            // need to clamp in the case that GetRandom() returns 1.0
            for (int i = 0; i < length; i++) sequence[i] = Vocabulary[(int)Math.Clamp(Math.Floor(GetRandom() * (Vocabulary.Length)), 0, 2)];
            return sequence;
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
        #endregion
    }
}
