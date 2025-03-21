using System;
using System.Collections.Generic;

using Learning.LanguageModel;

namespace Learning.Tests
{
    internal class LanguageModelTiny
    {
        // tests that cover Train and Inference (not NeuralNetwork tests)
        public static void Converge()
        {
            // build a quick TinyLanguageModel which should converge quickly
            var tokenizer = Tokenizer.Create("ABC", new TokenizerOptions()
            {
                Iterations = 0,
                Normalization = TokenizerNormalization.None,
                DefaultVocab = TokenizerDefaultVocab.Padding
            });


            var model = new TinyLanguageModel(
                new NeuralOptions()
                {
                    InputNumber = 12,
                    OutputNumber = 3,
                    HiddenLayerNumber = new int[] { 48 },
                    LearningRate = 0.1f,
                    MinibatchCount = 1,
                    ParallizeExecution = false,
                    WeightInitialization = NeuralWeightInitialization.LeCun,
                    BiasInitialization = NeuralBiasInitialization.SmallConstant_OneTenth
                },
                paddingToken: tokenizer.Tokens[Tokenizer.Padding]);

            // train the model with the following input
            var input = "ABCAABAAABBC";
            var tokens = tokenizer.Encode(input);

            // give it a few iterations to train
            var trained = false;
            for (var iter = 0; iter<250; iter++)
            {
                model.Train(tokens, minTokenCount: 6);

                // infer
                var infer = new List<int>();
                for (var i = 0; i < tokens.Count / 2; i++) infer.Add(tokens[i]);
                for (var i = tokens.Count / 2; i < tokens.Count; i++)
                {
                    var token = model.Inference(infer);
                    infer.Add(token);
                }
                // check the result
                var result = tokenizer.Decode(infer);
                if (iter % 10 == 0) Console.WriteLine($"{iter}: {input} -> {result}");
                if (result.Equals(input))
                {
                    Console.WriteLine($"trained after {iter} iterations");
                    trained = true;
                    break;
                }
            }

            if (!trained) throw new Exception("failed to train the model");
        }
    }
}
