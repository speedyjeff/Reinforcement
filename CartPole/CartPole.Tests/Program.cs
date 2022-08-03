using CartPole.Tests;
using System;

public class Program
{
    public static void Main()
    {
        NeuralTests.Boundaries();
        for (int i = 0; i < 100; i++)
        {
            CartPoleTests.Step(CartPole.CartPoleAction.Right);
            CartPoleTests.Step(CartPole.CartPoleAction.Left);
        }
    }
}