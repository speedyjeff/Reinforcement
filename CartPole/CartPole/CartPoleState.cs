using System;
using System.Collections.Generic;
using System.Text;

namespace CartPole
{
    public class CartPoleState
    {
        // cart location
        public float X { get; set; }
        // cart velocity
        public float dX { get; set; }
        // angle of the pole from the veritcal
        public float Th { get; set; }
        // velocity of the angle changing
        public float dTh { get; set; }
    }
}
