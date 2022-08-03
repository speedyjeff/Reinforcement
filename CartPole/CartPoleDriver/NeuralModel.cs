using CartPole;
using Learning;
using System;

// A number of resources were used to fine tune how to train a neural network to learn how to do this task
//   https://www.youtube.com/watch?v=3zeg7H6cAJw
//   https://www.youtube.com/watch?v=G-KvpNGudLw&t=14s

namespace CartPoleDriver
{
    class NeuralModel : IModel
    {
        public AlgorithmType Type => AlgorithmType.Neural;

        public NeuralModel(int iterations, float split, float learning, int[] hidden, int minibatchsize)
        {
            // parameter init
            MaxIterations = iterations;
            StopTrainingIteration = (int)(split * MaxIterations);

            // init
            NumberTrainings = 0;
            WinThreshold = 50;
            Iteration = 0;
            Random = new RandomModel();
            Choices = new List<Tuple<float[], int>>();
            Network = new NeuralNetwork(
                new NeuralOptions()
                {
                    InputNumber = 4,
                    OutputNumber = 2,
                    HiddenLayerNumber = hidden,
                    LearningRate = learning,
                    MinibatchCount = minibatchsize
                });
        }

        public void StartIteration(float m, float M, float l, float minX, float maxX, float minTh, float maxTh)
        {
        }

        public CartPoleAction MakeChoice(CartPoleState state)
        {
            CartPoleAction action;
            var input = new float[]
                {
                    state.X,
                    state.dX,
                    state.Th,
                    state.dTh
                };

            // if still training, then make a random choice
            if (Iteration < StopTrainingIteration)
            {
                // random
                action = Random.MakeChoice(state);

                // only train with inputs from the random games
                // store state and action pairs
                Choices.Add(new Tuple<float[], int>(input, (int)action));
            }
            else
            {
                // use the neural network
                var output = Network.Evaluate(input);
                action = (CartPoleAction)output.Result;
            }

            return action;
        }

        public void EndChoice(CartPoleState state, bool success)
        {
        }

        public void EndIteration(int count)
        {
            // increment iteration
            Iteration++;

            // the strategy is to train the model only on successful iterations (over a specific threshold)
            if (count > WinThreshold && Choices.Count > 0)
            {
                NumberTrainings++;

                // train the model withe State/Action pairs
                // NOTE: do not train with the last input - as that one lead to the end
                for(var i=0; i<Choices.Count - 1; i++)
                {
                    var pair = Choices[i];
                    var output = Network.Evaluate(pair.Item1);
                    Network.Learn(output, (int)pair.Item2);
                }
            }

            // clear the choices
            Choices.Clear();
        }

        public int Stat()
        {
            return NumberTrainings;
        }

        #region private
        private int MaxIterations;
        private int WinThreshold;
        private int StopTrainingIteration;

        private int Iteration;
        private RandomModel Random;
        private NeuralNetwork Network;

        private int NumberTrainings;

        private List<Tuple<float[] /*state*/, int /*action*/>> Choices;
        #endregion
    }
}
