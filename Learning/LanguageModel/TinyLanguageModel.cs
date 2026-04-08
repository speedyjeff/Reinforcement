using System;
using System.Collections.Generic;
using System.Numerics.Tensors;

namespace Learning.LanguageModel
{
    public class TinyLanguageModel
    {
        public TinyLanguageModel(NeuralOptions options, int paddingToken, int tokenCount)
        {
            // init
            PaddingToken = paddingToken;
            TokenCount = tokenCount;

            // create a neural network based on this tokenizer
            Network = new NeuralNetwork(options);

            // initialize the input buffers (will be filled with padding tokens)
            NetworkInput = new float[Network.InputNumber];
        }

        public void Train(List<int> tokens, int minTokenCount = 1)
        {
            // train the network on all the possible sequences
            //  input = [1,2,3,4,5]
            //     [1] -> [2]
            //     [1,2] -> [3]
            //     [1,2,3] -> [4]
            //     [1,2,3,4] -> [5]

            // the smallest min token count is 1
            if (minTokenCount <= 0) minTokenCount = 1;

            // one-hot encoding: each position gets a TokenCount-length binary vector
            int sequenceLength = NetworkInput.Length / TokenCount;
            Array.Clear(NetworkInput, 0, NetworkInput.Length);

            // prefill with tokens up to minTokenCount-1 (setting 1 token per window)
            for (int i = 0; i < minTokenCount - 1 && i < tokens.Count && i < sequenceLength; i++)
            {
                NetworkInput[(i * TokenCount) + tokens[i]] = 1.0f;
            }
            // padding for remaining positions (setting 1 token per window)
            for (int i = Math.Min(minTokenCount - 1, Math.Min(tokens.Count, sequenceLength)); i < sequenceLength; i++)
            {
                NetworkInput[(i * TokenCount) + PaddingToken] = 1.0f;
            }

            // progressively reveal tokens and train
            for (int i = minTokenCount - 1; i < tokens.Count - 1 && i < sequenceLength; i++)
            {
                // turn off the padding token
                NetworkInput[i * TokenCount + PaddingToken] = 0.0f;
                // include the next token
                NetworkInput[i * TokenCount + tokens[i]] = 1.0f;

                // evaluate and reinforce
                var output = Network.Evaluate(NetworkInput);
                Network.Learn(output, preferredResult: tokens[i + 1]);
            }
        }

        public NeuralOutput Inference(List<int> tokens, float temperature = 1.0f)
        {
            // predict the next token and optionally scale the probabilities by temperature
            if (tokens == null || tokens.Count == 0) throw new ArgumentException("tokens cannot be null or empty");
            
            // one-hot encoding: each position gets a TokenCount-length binary vector
            int sequenceLength = NetworkInput.Length / TokenCount;
            Array.Clear(NetworkInput, 0, NetworkInput.Length);
            for (int i = 0; i < tokens.Count && i < sequenceLength; i++)
            {
                NetworkInput[i * TokenCount + tokens[i]] = 1.0f;
            }
            for (int i = tokens.Count; i < sequenceLength; i++)
            {
                NetworkInput[i * TokenCount + PaddingToken] = 1.0f;
            }

            // predict
            var output = Network.Evaluate(NetworkInput);

            // use the network probabilities as-is for greedy or standard inference
            if (temperature <= 0.01f || Math.Abs(temperature - 1.0f) < 0.001f) return output;

            // re-apply softmax with temperature scaling on the pre-softmax logits
            output.Probabilities = ScaleProbabilities(output.Z[output.Z.Length - 1], temperature);
            return output;
        }

        public TinyLanguageModel Copy()
        {
            // create a copy of the model
            return new TinyLanguageModel()
            {
                Network = NeuralNetwork.Load(Network),
                PaddingToken = PaddingToken,
                TokenCount = TokenCount,
                NetworkInput = new float[NetworkInput.Length]
            };
        }

        public static TinyLanguageModel Merge(TinyLanguageModel[] models, NeuralMergeOptions options)
        {
            if (models == null || models.Length == 0) throw new ArgumentException("models cannot be null or empty");

            // create an array of the neural networks in these models
            var networks = new NeuralNetwork[models.Length];
            for (var i = 0; i < models.Length; i++) networks[i] = models[i].Network;

            // merge the models
            return new TinyLanguageModel()
            {
                Network = NeuralNetwork.Merge(networks, options),
                PaddingToken = models[0].PaddingToken,
                TokenCount = models[0].TokenCount,
                NetworkInput = new float[models[0].NetworkInput.Length]
            };
        }

        #region private
        private NeuralNetwork Network;
        private float[] NetworkInput;
        private int PaddingToken;
        private int TokenCount;

        private TinyLanguageModel() { }

        private static float[] ScaleProbabilities(float[] logits, float temperature)
        {
            var result = new float[logits.Length];
            TensorPrimitives.Divide(logits, temperature, result);
            TensorPrimitives.SoftMax(result, result);
            return result;
        }
        #endregion
    }
}
