using System;
using System.Collections.Generic;

namespace Learning.LanguageModel
{
    public class TinyLanguageModel
    {
        public TinyLanguageModel(NeuralOptions options, int paddingToken)
        {
            // init
            PaddingToken = paddingToken;

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

            // prefill the network from 0 to minTokenCount - 1 and the rest with padding
            for (var i = 0; i < NetworkInput.Length; i++)
            {
                if (i < (minTokenCount - 1) && i < tokens.Count) NetworkInput[i] = (float)tokens[i];
                else NetworkInput[i] = (float)PaddingToken;
            }

            // train to all possible sequences
            for (int i = minTokenCount - 1; i < tokens.Count - 1; i++)
            {
                // include the next token
                NetworkInput[i] = (float)tokens[i];

                // train
                var output = Network.Evaluate(NetworkInput);

                // reinforce
                Network.Learn(output, preferredResult: tokens[i + 1]);
            }
        }

        public int Inference(List<int> tokens)
        {
            // return the top prediction
            var result = new int[0];
            return Inference(tokens, ref result);
        }

        public int Inference(List<int> tokens, ref int[] inferences)
        {
            // predict the next token
            if (tokens == null || tokens.Count == 0) throw new ArgumentException("tokens cannot be null or empty");
            if (tokens.Count > Network.InputNumber) throw new ArgumentException("tokens must match the model input number");

            // set the input based on the first i tokens
            for (var j = 0; j < NetworkInput.Length; j++)
            {
                if (j < tokens.Count) NetworkInput[j] = (float)tokens[j];
                else NetworkInput[j] = (float)PaddingToken;
            }

            // predict
            var output = Network.Evaluate(NetworkInput);

            // check if returning more than 1 result
            if (inferences != null && inferences.Length > 0)
            {
                if (inferences.Length == 1)
                {
                    // fill in the top probability
                    inferences[0] = output.Result;
                }
                else
                {
                    // fill in the top probabilities
                    var top = new TopN(inferences.Length);
                    for (var i = 0; i < output.Probabilities.Length; i++) top.Add(output.Probabilities[i], i, ref inferences);
                }
            }

            return output.Result;
        }

        #region private
        private NeuralNetwork Network;
        private float[] NetworkInput;
        private int PaddingToken;

        struct TopN
        {
            public TopN(int size)
            {
                Maxes = new float[size];
                for(int i=0; i < Maxes.Length; i++) Maxes[i] = Single.MinValue;
            }

            public void Add(float probability, int index, ref int[] indexes)
            {
                // check if greater than the max, if so shift and add
                for (var i = 0; i < Maxes.Length; i++)
                {
                    if (probability > Maxes[i])
                    {
                        Shift(i, ref indexes);
                        indexes[i] = index;
                        Maxes[i] = probability;
                        break;
                    }
                }
            }

            #region private
            private float[] Maxes;

            private void Shift(int index, ref int[] indexes)
            {
                // shift the index
                for (var i = Maxes.Length - 1; i > index; i--)
                {
                    indexes[i] = indexes[i - 1];
                    Maxes[i] = Maxes[i - 1];
                }
            }
            #endregion
        }
        #endregion
    }
}
