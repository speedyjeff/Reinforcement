using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

// A lot of materials were referenced to build this model
//   https://en.wikipedia.org/wiki/Inverted_pendulum
//   https://towardsdatascience.com/reinforcement-learning-concept-on-cart-pole-with-dqn-799105ca670
//   https://www.youtube.com/watch?v=Fo7kuUAHj3s
//   https://github.com/openai/gym/blob/master/gym/envs/classic_control/cartpole.py

namespace CartPole
{
    public class CartPolePhysics
    {
        //         . Y  |
        //    m  th.    |
        //     \   .   \/
        //    l \  .   g
        //       \ .
        //        \.
        //  |-----------|
        //  |     M     | -> F(t) X
        //  |-----------|

        // actions
        //  right (+x)
        //  left (-x)

        // rewards
        //  1 for still upright and within bounds

        // starting values [-0.05, 0.05]

        // termination
        //  pole angle > +/- 12 d (+/- 0.2 r)
        //  cart position > +/- 2.4
        //  episode length > 500

        // position of the mass m
        //  xm = X - l * sin(th)
        //  ym = l * cose(th)
        //  th = cos(xm/ym)

        // velocity of the mass m
        //  dxm = dX - l * dth * cos(th)
        //  xym = -1 * l * dth * sin(th)

        // equations of motoin (based on Lagrange's equations)
        //  ddX  = (M+m) * ddX - m * l * ddth * cos(th) + m * l * dth^2 * sin(th) = F(t)
        //       = (m * l * ddth * cos(th) - m * l * dth^2 * sin(th) + F(t)) / (M+m)
        //  ddth = l * ddth - ddX * cos(th) - g * sin(th) = 0
        //       = (g * sin(th) - ddX * cos(th)) / l

        public CartPolePhysics()
        {
            // variables
            L = 0.5f; // half
            Mpole = 0.1f;
            Mcart = 1.0f;
            dT = 0.02f;
            FMag = 10.0f;
            Countmax = 500;

            // const
            G = 9.8f;
            Xmin = -2.4f;
            Xmax = Xmin * -1;
            Thmin = -12f * 2f * (float)(Math.PI / 360);
            Thmax = Thmin * -1;
            State = new CartPoleState();
            Count = 0;

            // reset
            Reset();
        }

        public float G { get; private set; }
        public float L { get; private set; }
        public float Mpole { get; private set; }
        public float Mcart { get; private set; }
        public float dT { get; private set; }
        public float FMag { get; private set; }
        public float Xmin { get; private set; }
        public float Xmax { get; private set; }
        public float Thmin { get; private set; }
        public float Thmax { get; private set; }
        public int Count { get; private set; }
        public int Countmax { get; private set; }

        public CartPoleState State { get; private set; }

        public void Step(CartPoleAction action)
        {
            if (IsDone) throw new Exception("invalid to step on a done cart pole");

            // common calculations
            var force = FMag * ((action == CartPoleAction.Left) ? -1f : 1f);
            var costh = (float)Math.Cos(State.Th);
            var sinth = (float)Math.Sin(State.Th);

            // determine rate of change (https://coneural.org/florian/papers/05_cart_pole.pdf)
            var tmp = (force + (Mpole * L * (float)Math.Pow(State.dTh, 2) * sinth)) / (Mpole + Mcart);
            var ddTh = ((G * sinth) - (costh * tmp)) / (L * ((4.0f / 3.0f) - (Mpole * (float)Math.Pow(costh, 2) / (Mpole + Mcart))));
            var ddX = tmp - ((Mpole * L * ddTh * costh) / (Mpole + Mcart));

            // update state
            State.X += (dT * State.dX);
            State.dX += (dT * ddX);
            State.Th += (dT * State.dTh);
            State.dTh += (dT * ddTh);

            Count++;
        }

        public bool IsDone
        {
            get
            {
                // check if we are in a terminal state
                return (State.X < Xmin) ||
                    (State.X > Xmax) ||
                    (State.Th < Thmin) ||
                    (State.Th > Thmax) ||
                    (Count > Countmax);
            }
        }

        public void Reset()
        {
            // reset state
            var rand = RandomNumberGenerator.Create();
            State.X = GetRandom(rand);
            State.dX = GetRandom(rand);
            State.Th = GetRandom(rand);
            State.dTh = GetRandom(rand);

            // count
            Count = 0;
        }


        #region private
        private float GetRandom(RandomNumberGenerator rand)
        {
            var int32buffer = new byte[4];
            rand.GetNonZeroBytes(int32buffer);
            // ensure positive
            int32buffer[3] &= 0x7f;
            var number = BitConverter.ToInt32(int32buffer);
            // get a random float between -0.05 and 0.05
            return (((float)number / (float)Int32.MaxValue) * 0.1f) - 0.05f;
        }
        #endregion

    }
}
