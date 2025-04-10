using System;

namespace Learning.Tests
{
    internal static class DeepQTests
    {
        public static void EndToEnd()
        {
            // this test will build a DeepQ that has 5 inputs (consider a row) and 4 outputs (movement in cardinal directions)
            // with epsilon set to 1, the choices will be random and after BatchSize is exceeded it should be rewarded in one of the movements
            // the validation will be to check the probabilities and that movement should be the highest
            var batchSize = 20;
            var deepQ = new DeepQ(
                new DeepQOptions()
                {
                    ContextNum = 5,
                    ActionNum = 4,
                    BatchSize = batchSize,
                    Epsilon = 1f
                });

            var targetAction = 2;
            var soleContext = 3;
            var yesReward = 1f;
            var noReward = -0.04f;

            // get the initial prediction
            var beforePreds = deepQ.GetProbabilities(soleContext);

            // run training for BatchSize+1 and the model should be updated
            var actions = new List<int>() { 0, 1, 2, 3 };
            for(int i=0; i<(batchSize*3)+1; i++)
            {
                var action = deepQ.ChooseAction(soleContext, actions);
                deepQ.Remember(
                    soleContext,
                    action,
                    (action == targetAction) ? soleContext + 1 : soleContext - 1,
                    (action == targetAction) ? yesReward : noReward);
            }

            // get predictions again
            var afterPreds = deepQ.GetProbabilities(soleContext);

            for (int i = 0; i < beforePreds.Length; i++)
            {
                if (i == targetAction)
                {
                    // the desired action should be the highest
                    if (afterPreds[i] <= beforePreds[targetAction]) throw new Exception("is not training towards success");
                }
                else
                {
                    // all other actions should be lower
                    if (afterPreds[i] >= beforePreds[i]) throw new Exception("is not training towards success");
                }
            }
        }
    }
}
