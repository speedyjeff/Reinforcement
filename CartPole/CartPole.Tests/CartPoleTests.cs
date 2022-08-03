using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartPole.Tests
{
    internal class CartPoleTests
    {
        public static void Step(CartPoleAction dir)
        {
            var cartpole = new CartPolePhysics();

            // step in the same direciton until it falls over
            while(!cartpole.IsDone)
            {
                cartpole.Step(dir);
            }

            if (cartpole.Count < 8 || cartpole.Count > 11) throw new Exception($"invalid count : {cartpole.Count}");
        }
    }
}
