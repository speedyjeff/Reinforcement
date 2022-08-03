using CartPole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartPoleDriver
{
    class ManualModel : IModel
    {
        public AlgorithmType Type => AlgorithmType.Manual;

        public void StartIteration(float m, float M, float l, float minX, float maxX, float minTh, float maxTh)
        {
            Console.WriteLine("choose right (arrow) or left (arrow)... ");
        }

        public void EndIteration(int count)
        {
        }

        public CartPoleAction MakeChoice(CartPoleState state)
        {
            var dir = ConsoleKey.RightArrow;
            do
            {
                dir = Console.ReadKey().Key;
            }
            while (dir != ConsoleKey.RightArrow && dir != ConsoleKey.LeftArrow);
            return dir == ConsoleKey.RightArrow ? CartPoleAction.Right : CartPoleAction.Left;
        }

        public void EndChoice(CartPoleState state, bool success)
        {
        }

        public int Stat()
        {
            return 0;
        }
    }
}
