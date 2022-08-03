using CartPole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartPoleDriver
{
    interface IModel
    {
        // executed at the beginning of the iteration
        public void StartIteration(float m, float M, float l, float minX, float maxX, float minTh, float maxTh);
        // request for the model to make a choice
        public CartPoleAction MakeChoice(CartPoleState state);
        // resulting state change from the choice and if it was successful
        public void EndChoice(CartPoleState state, bool success);
        // executed at the end of the iteratoin
        public void EndIteration(int count);

        // model specific metric on fitness
        public int Stat();

        // type of the model
        public AlgorithmType Type { get; }
    }
}
