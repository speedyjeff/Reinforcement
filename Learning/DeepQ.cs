using System;
using System.Collections.Generic;

// https://towardsdatascience.com/deep-q-learning-tutorial-mindqn-2a4c855abffc/
// https://en.wikipedia.org/wiki/Q-learning

namespace Learning
{
    public struct DeepQOptions
    {
        // context    : number of context items (default 4)
        public int ContextNum { get; set; }
        // action     : number of actions (default 4)
        public int ActionNum { get; set; }
        // reward     : cost of taking an action (default -0.04)
        public float Reward { get; set; }
        // learning   : 0 [no learning/rely on prior knowledge] ... [1] only most recent info (default 0.001)
        public float Learning { get; set; }
        // discount   : 0 [short term rewards] ... 1 [long term rewards] (default 1)  (eg. gamma)
        public float Discount { get; set; }
        // epsilon    : exploration factor (default 1)
        public float Epsilon { get; set; }
        // memory     : number of previous moves to remember (default 1000)
        public int MemoryMaxSize { get; set; }
        // target update frequency (default 100)
        public int TargetUpdateFreq { get; set; }
        // batch size (default 32)
        public int BatchSize { get; set; }

        public DeepQOptions()
        {
            ContextNum = 4;
            ActionNum = 4;
            Reward = -0.04f;
            Learning = 0.001f;
            Discount = 1f;
            Epsilon = 1f;
            MemoryMaxSize = 1000;
            TargetUpdateFreq = 100;
            BatchSize = 32;
        }
    }

    public class DeepQ
    {
        public DeepQ(DeepQOptions options)
        {
            // init
            Rand = new Random();
            Options = options;
            Epsilon = options.Epsilon;
            Memory = new List<MemoryDetails>();
            Iteration = 0;
            Reward = options.Reward;
            Learning = options.Learning;
            Discount = options.Discount;

            // basic neural network options
            var neuralOptions = new NeuralOptions()
            {
                LearningRate = options.Learning,
                InputNumber = options.ContextNum,
                HiddenLayerNumber = new int[] { (options.ContextNum+options.ActionNum) * 10 },
                OutputNumber = options.ActionNum,
                MinibatchCount = 1,
                ParallizeExecution = false,
                WeightInitialization = NeuralWeightInitialization.He,
                BiasInitialization = NeuralBiasInitialization.Zero
            };

            // create the main model
            MainModel = new NeuralNetwork(neuralOptions);

            // copy to the target model
            TargetModel = NeuralNetwork.Load(MainModel);
        }

        public float Reward { get; set; }
        public float Learning { get; set; }
        public float Discount { get; set; }

        public int ChooseAction(int context, List<int> actions)
        {
            var action = 0;
            if (Epsilon > 0f && Rand.NextDouble() < Epsilon)
            {
                // explore
                action = actions[Rand.Next(actions.Count)];
            }
            else
            {
                // exploit
                var input = new float[Options.ContextNum];
                input[context] = 1f;
                var output = MainModel.Evaluate(input);

                // based on the available actions, choose the one with the highest probability
                var max = float.MinValue;
                for (var i = 0; i < actions.Count; i++)
                {
                    if (output.Probabilities[actions[i]] > max)
                    {
                        max = output.Probabilities[actions[i]];
                        action = actions[i];
                    }
                }
            }

            return action;
        }

        public void Remember(int context, int action, int nextContext, float reward)
        {
            // increment iteration
            Iteration++;

            // store in memory
            Memory.Add(new MemoryDetails()
            {
                Context = context,
                Action = action,
                NextContext = nextContext,
                Reward = reward
            });

            // check if over memory size
            if (Memory.Count > Options.MemoryMaxSize) Memory.RemoveAt(0);

            // check if we should replay
            if (Memory.Count >= Options.BatchSize && (Iteration % Options.BatchSize) == 0)
            {
                // get a random selection from memory and train
                var seen = new HashSet<int>();
                for(var i = 0; i < Options.BatchSize; i++)
                {
                    // update that a model update has occurred
                    ModelUpdateIteration++;

                    // get a unique random index
                    var index = 0;
                    do
                    {
                        index = Rand.Next(Memory.Count);
                    }
                    while (seen.Contains(index));
                    seen.Add(index);

                    // get current prediction
                    var mainInput = new float[Options.ContextNum];
                    mainInput[Memory[index].Context] = 1f;
                    var mainOutput = MainModel.Evaluate(mainInput);

                    // get target prediction
                    var targetInput = new float[Options.ContextNum];
                    targetInput[Memory[index].NextContext] = 1f;
                    var targetOutput = TargetModel.Evaluate(targetInput);

                    // update the preferred action - the target network is used to predict the future reward
                    // choose the probability with the best action to influence the main model (the actual action in the target is not interesting, just the probability of it being good)
                    mainOutput.Probabilities[Memory[index].Action] = Memory[index].Reward + (Options.Discount * targetOutput.Probabilities[targetOutput.Result]);

                    // update the main model
                    MainModel.Learn(mainOutput, mainOutput.Probabilities);
                }

                // check if we need to update the target model
                if (ModelUpdateIteration >= Options.TargetUpdateFreq)
                {
                    TargetModel = NeuralNetwork.Load(MainModel);
                    ModelUpdateIteration = 0;
                }
            }

            // adjust epsilon (on a successful run)
            if (reward > 0 && Epsilon > 0f)
            {
                Epsilon = Math.Max(Epsilon * EpsilonDecay, EpsilonMin);
            }
        }

        public float[] GetProbabilities(int context)
        {
            var input = new float[Options.ContextNum];
            input[context] = 1f;
            var output = MainModel.Evaluate(input);
            return output.Probabilities;
        }

        #region private
        private NeuralNetwork MainModel;
        private NeuralNetwork TargetModel;
        private readonly Random Rand;
        private DeepQOptions Options;
        private float Epsilon;
        private int Iteration;
        private int ModelUpdateIteration;

        private const float EpsilonDecay = 0.995f;
        private const float EpsilonMin = 0.1f;

        // replay memory
        private class MemoryDetails
        {
            public int Context;
            public int Action;
            public int NextContext;
            public float Reward;
        }
        private List<MemoryDetails> Memory;
        #endregion
    }
}