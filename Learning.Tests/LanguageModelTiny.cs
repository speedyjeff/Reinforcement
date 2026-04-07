using System;
using System.Collections.Generic;

using Learning.LanguageModel;

namespace Learning.Tests
{
    internal class LanguageModelTiny
    {
        private struct AlphabetFixture
        {
            public Tokenizer Tokenizer;
            public TinyLanguageModel Model;
            public List<int> Tokens;
            public string Input;
        }

        // tests that cover Train and Inference (not NeuralNetwork tests)
        public static void Converge()
        {
            var fixture = CreateAlphabetFixture();

            // give it a few iterations to train
            var trained = false;
            for (var iter = 0; iter<250; iter++)
            {
                fixture.Model.Train(fixture.Tokens, minTokenCount: 6);

                // infer
                var infer = new List<int>();
                for (var i = 0; i < fixture.Tokens.Count / 2; i++) infer.Add(fixture.Tokens[i]);
                for (var i = fixture.Tokens.Count / 2; i < fixture.Tokens.Count; i++)
                {
                    var token = fixture.Model.Inference(infer).Result;
                    infer.Add(token);
                }
                // check the result
                var result = fixture.Tokenizer.Decode(infer);
                if (iter % 10 == 0) Console.WriteLine($"{iter}: {fixture.Input} -> {result}");
                if (result.Equals(fixture.Input))
                {
                    Console.WriteLine($"trained after {iter} iterations");
                    trained = true;
                    break;
                }
            }

            if (!trained) throw new Exception("failed to train the model");
        }

        public static void InferenceRejectsInvalidInput()
        {
            var fixture = CreateAlphabetFixture();

            try
            {
                fixture.Model.Inference(null!);
                throw new Exception("expected ArgumentException for null tokens");
            }
            catch (ArgumentException)
            {
            }

            try
            {
                fixture.Model.Inference(new List<int>());
                throw new Exception("expected ArgumentException for empty tokens");
            }
            catch (ArgumentException)
            {
            }
        }

        public static void InferenceResultMatchesTopProbability()
        {
            var fixture = CreateAlphabetFixture(train: true, trainingIterations: 32);
            var output = fixture.Model.Inference(GetSeedTokens(fixture.Tokens));

            ValidateProbabilities(output, fixture.Tokenizer.Tokens.Count, "default inference");
            if (output.Result != GetMaxIndex(output.Probabilities)) throw new Exception("inference result does not match the highest probability");
        }

        public static void InferenceDefaultTemperatureMatchesExplicitOne()
        {
            var fixture = CreateAlphabetFixture(train: true, trainingIterations: 32);
            var seed = GetSeedTokens(fixture.Tokens);

            var defaultOutput = fixture.Model.Inference(seed);
            var explicitOutput = fixture.Model.Inference(seed, 1.0f);

            ValidateProbabilities(defaultOutput, fixture.Tokenizer.Tokens.Count, "default temperature");
            ValidateProbabilities(explicitOutput, fixture.Tokenizer.Tokens.Count, "temperature=1.0");

            if (defaultOutput.Result != explicitOutput.Result) throw new Exception("default inference and temperature=1.0 produced different results");
            AssertProbabilitiesEqual(defaultOutput.Probabilities, explicitOutput.Probabilities, 0.000001f, "temperature=1.0");
        }

        public static void InferenceTemperatureScalingProducesValidProbabilities()
        {
            var fixture = CreateAlphabetFixture();
            var seed = GetSeedTokens(fixture.Tokens);

            var baseline = fixture.Model.Inference(seed);
            var scaled = fixture.Model.Inference(seed, 3.0f);

            ValidateProbabilities(baseline, fixture.Tokenizer.Tokens.Count, "baseline inference");
            ValidateProbabilities(scaled, fixture.Tokenizer.Tokens.Count, "scaled inference");

            if (scaled.Result != GetMaxIndex(scaled.Probabilities)) throw new Exception("scaled inference result does not match the highest probability");

            var changed = false;
            for (var i = 0; i < baseline.Probabilities.Length; i++)
            {
                if (Math.Abs(baseline.Probabilities[i] - scaled.Probabilities[i]) > 0.0001f)
                {
                    changed = true;
                    break;
                }
            }

            if (!changed) throw new Exception("temperature scaling did not change the probability distribution");
        }

        public static void InferenceHandlesShortAndLongInputs()
        {
            var fixture = CreateAlphabetFixture(train: true, trainingIterations: 16);

            var shortOutput = fixture.Model.Inference(new List<int>() { fixture.Tokens[0] });
            ValidateProbabilities(shortOutput, fixture.Tokenizer.Tokens.Count, "short input");

            var longTokens = new List<int>();
            while (longTokens.Count < 32) longTokens.AddRange(fixture.Tokens);
            var longOutput = fixture.Model.Inference(longTokens);
            ValidateProbabilities(longOutput, fixture.Tokenizer.Tokens.Count, "long input");
        }

        #region private
        private static AlphabetFixture CreateAlphabetFixture(bool train = false, int trainingIterations = 0)
        {
            // build a quick TinyLanguageModel which should converge quickly
            var tokenizer = Tokenizer.Create("ABC", new TokenizerOptions()
            {
                Iterations = 0,
                Normalization = TokenizerNormalization.None,
                DefaultVocab = TokenizerDefaultVocab.Padding
            });

            var tokenCount = tokenizer.Tokens.Count;
            var model = new TinyLanguageModel(
                new NeuralOptions()
                {
                    InputNumber = 12 * tokenCount,
                    OutputNumber = tokenCount,
                    HiddenLayerNumber = new int[] { 48 },
                    LearningRate = 0.1f,
                    MinibatchCount = 1,
                    ParallizeExecution = false,
                    WeightInitialization = NeuralWeightInitialization.LeCun,
                    BiasInitialization = NeuralBiasInitialization.SmallConstant_OneTenth
                },
                paddingToken: tokenizer.Tokens[Tokenizer.Padding],
                tokenCount: tokenCount);

            var input = "ABCAABAAABBC";
            var tokens = tokenizer.Encode(input);

            for (var iter = 0; iter < trainingIterations; iter++)
            {
                model.Train(tokens, minTokenCount: 6);
            }

            return new AlphabetFixture()
            {
                Tokenizer = tokenizer,
                Model = model,
                Tokens = tokens,
                Input = input
            };
        }

        private static List<int> GetSeedTokens(List<int> tokens)
        {
            var seed = new List<int>();
            for (var i = 0; i < tokens.Count / 2; i++) seed.Add(tokens[i]);
            return seed;
        }

        private static void ValidateProbabilities(NeuralOutput output, int expectedLength, string scenario)
        {
            if (output.Probabilities == null) throw new Exception($"{scenario}: probabilities should not be null");
            if (output.Probabilities.Length != expectedLength) throw new Exception($"{scenario}: expected {expectedLength} probabilities");
            if (output.Result < 0 || output.Result >= output.Probabilities.Length) throw new Exception($"{scenario}: result is out of range");

            var sum = 0f;
            for (var i = 0; i < output.Probabilities.Length; i++)
            {
                var probability = output.Probabilities[i];
                if (float.IsNaN(probability) || float.IsInfinity(probability)) throw new Exception($"{scenario}: probability {i} is invalid");
                if (probability < 0f || probability > 1f) throw new Exception($"{scenario}: probability {i} is out of range");
                sum += probability;
            }

            if (Math.Abs(sum - 1.0f) > 0.001f) throw new Exception($"{scenario}: probabilities do not sum to 1");
        }

        private static int GetMaxIndex(float[] values)
        {
            var index = 0;
            var max = values[0];
            for (var i = 1; i < values.Length; i++)
            {
                if (values[i] > max)
                {
                    max = values[i];
                    index = i;
                }
            }
            return index;
        }

        private static void AssertProbabilitiesEqual(float[] expected, float[] actual, float epsilon, string scenario)
        {
            if (expected.Length != actual.Length) throw new Exception($"{scenario}: probability lengths do not match");
            for (var i = 0; i < expected.Length; i++)
            {
                if (Math.Abs(expected[i] - actual[i]) > epsilon) throw new Exception($"{scenario}: probability {i} differs");
            }
        }
        #endregion
    }
}
