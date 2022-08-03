using CartPole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace CartPoleDriver
{
    class RandomModel : IModel
    {
        public RandomModel()
        {
            Rand = new Random();
        }

        public AlgorithmType Type => AlgorithmType.Random;

        public void StartIteration(float m, float M, float l, float minX, float maxX, float minTh, float maxTh)
        {
        }

        public CartPoleAction MakeChoice(CartPoleState state)
        {
            return (Rand.Next() % 2 == 0) ? CartPoleAction.Left : CartPoleAction.Right;
        }

        public void EndChoice(CartPoleState state, bool success)
        {
        }

        public void EndIteration(int count)
        {
        }

        public int Stat()
        {
            return 0;
        }

        #region private
        private Random Rand;
        #endregion
    }
}
