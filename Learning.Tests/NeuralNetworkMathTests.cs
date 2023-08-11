using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learning.Tests
{
    class NeuralNetworkMathTests : Learning.NeuralNetwork
    {
        //
        // ReLu
        //
        public static void ReLuTest()
        {
            var input = new float[] { 0f, 1f, -1f, 0.5f, -0.5f };
            var expectedOutput = new float[input.Length];
            for (int i = 0; i < input.Length; i++) expectedOutput[i] = (input[i] > 0f ? input[i] : 0f);
            var output = ReLU(input);
            for (int i=0; i<output.Length; i++)
            {
                if (output[i] != expectedOutput[i]) throw new Exception("invalid ReLu");
            }
        }

        public static void ReLuTestPerf(int iterations)
        {
            var input = new float[64];
            for (var i = 0; i < input.Length; i++) input[i] = 0.5f;

            for (var i = 0; i < iterations; i++)
            {
                var output = ReLU(input);
            }
        }

        //
        // dOfReLu
        //
        public static void dOfReLuTest()
        {
            var input = new float[] { 0f, 1f, -1f, 0.5f, -0.5f };
            var expectedOutput = new float[input.Length];
            for (int i = 0; i < input.Length; i++) expectedOutput[i] = (input[i] > 0f ? 1f : 0f);
            var output = dOfReLU(input);
            for (int i = 0; i < output.Length; i++)
            {
                if (output[i] != expectedOutput[i]) throw new Exception("invalid dOfReLu");
            }
        }

        public static void dOfReLuTestPerf(int iterations)
        {
            var input = new float[64];
            for (var i = 0; i < input.Length; i++) input[i] = 0.5f;

            for (var i = 0; i < iterations; i++)
            {
                var output = dOfReLU(input);
            }
        }

        //
        // softmax
        //
        public static void SoftmaxTestPerf(int iterations)
        {
            var input = new float[] { 0f, 1f, 2f, 3f, -0.5f, 0f, 5f, -2f };

            for(int i=0; i<iterations; i++)
            {
                var output = Softmax(input);
            }
        }

        public static void SoftmaxTest()
        {
            var input = new float[] { 0f, 1f, -1f, 0.5f, -0.5f, 0.25f, 0.9f, -0.25f };
            var expectedOutput = new float[] { 0.09f, 0.25f, 0.3f, 0.15f, 0.5f, 0.11f, 0.22f, 0.07f };
            var output = Softmax(input);
            for(int i=0; i<output.Length; i++)
            {
                if (output[i] - expectedOutput[i] > 0.01f) throw new Exception("invalid softmax");
            }
        }

        public static void SoftmaxTest2()
        {
            var input = new float[10];
            var rand = new Random();
            for (int i = 0; i < input.Length; i++) input[i] = (float)rand.NextDouble();
            var output = Softmax(input);
            var sum = 0f;
            for (int i = 0; i < output.Length; i++) sum += output[i];
            if (Math.Abs(sum - 1f) > 0.01f) throw new Exception("invalid softmax");
        }

        //
        // Dot
        //
        public static void DotTest()
        {
            var input = new float[] { 0f, 1f, -1f, 0.5f, -0.5f };
            var expectedOutput = new float[input.Length];
            for (int i = 0; i < input.Length; i++) expectedOutput[i] = (input[i] > 0f ? input[i] : 0f);
            var output = ReLU(input);
            for (int i = 0; i < output.Length; i++)
            {
                if (output[i] != expectedOutput[i]) throw new Exception("invalid ReLu");
            }
        }

        public static void DotTestPerf(int iterations)
        {
            var rand = new Random();
            var a = new float[64];
            var b = new float[64];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = (float)(rand.NextDouble() % 100) - 50f;
                b[i] = (float)(rand.NextDouble() % 100) - 50f;
            }

            for (var i = 0; i < iterations; i++)
            {
                var output = Dot(a, b);
            }
        }

        //
        // DotFirstParamT
        //
        public static void DotFirstParamTTest()
        {
            // [1,2,3]    [4]
            // [4,5,6]T . [5]
            // [7,8,9]    [6]
            // =
            // [1,4,7]   [4]
            // [2,5,8] . [5]
            // [3,6,9]   [6]
            // =
            // [1*4+4*5+7*6]
            // [2*4+5*5+8*6]
            // [3*4+6*5+9*6]
            // =
            // [66]
            // [81]
            // [96]
            var m = new float[][]
            {
                new float[] {1,2,3} ,
                new float[] {4,5,6 },
                new float[] {7,8,9 }
            };
            var v = new float[] { 4, 5, 6 };
            var expectedOutput = new float[] { 66, 81, 96 };

            var output = DotFirstParamT(m, v);

            for(var i = 0; i<output.Length; i++)
            {
                if (output[i] != expectedOutput[i]) throw new Exception("invalid DotFirstParamT");
            }
        }

        public static void DotFirstParamTTestPerf(int iterations)
        {
            var dim = 8;
            var m = new float[dim][];
            var v = new float[dim];
            var rand = new Random();
            for (int i = 0; i < m.Length; i++)
            {
                m[i] = new float[dim];
                for(int j=0; j < m[i].Length; j++)
                {
                    m[i][j] = (float)(rand.NextDouble() % 100) - 50f;
                }
                v[i] = (float)(rand.NextDouble() % 100) - 50f;
            }


            for (var i = 0; i < iterations; i++)
            {
                var output = DotFirstParamT(m, v);
            }
        }

        //
        // DotSecondParamT
        //
        public static void DotSecondParamTTest()
        {
            // [1,2,3,4,5,6] . [4,5,6]T
            // =
            // [1,2,3,4,5,6] . [4]
            //                 [5]
            //                 [6]
            // =
            // [1*4,1*5,1*6]
            // [2*4,2*5,2*6]
            // [3*4,3*5,3*6]
            // [4*4,4*5,4*6]
            // [5*4,5*5,5*6]
            // [6*4,6*5,6*6]
            // =
            // [4,5,6]
            // [8,10,12]
            // [12,15,18]
            // [16,20,24]
            // [20,25,30]
            // [24,30,36]
            var m = new float[] {1,2,3,4,5,6 };
            var v = new float[] { 4, 5, 6 };
            var output = new float[m.Length][];
            for(int i=0; i<output.Length; i++) output[i] = new float[v.Length];
            var expectedOutput = new float[][]
            {
                new float[] {4,5,6 },
                new float[] {8,10,12 },
                new float[] {12,15,18 },
                new float[] {16,20,24 },
                new float[] {20,25,30 },
                new float[] {24,30,36 }
            };

            DotSecondParamT(m, v, ref output);

            for(int i=0; i<output.Length; i++)
            {
                for(int j=0; j < output[i].Length; j++)
                {
                    if (output[i][j] != expectedOutput[i][j]) throw new Exception("invalid DotSecondParamT");
                }
            }
        }

        public static void DotSecondParamTTestPerf(int iterations)
        {
            var m = new float[16];
            var v = new float[7];
            var output = new float[m.Length][];
            for (int i = 0; i < output.Length; i++) output[i] = new float[v.Length];
            var rand = new Random();
            for (int i = 0; i < m.Length; i++) m[i] = (float)(rand.NextDouble() % 100) - 50f;
            for (int i = 0; i < v.Length; i++) v[i] = (float)(rand.NextDouble() % 100) - 50f;

            for (int i= 0; i < iterations; i++)
            {
                DotSecondParamT(m, v, ref output);
            }
        }

        //
        // subtract
        //
        public static void SubtractTest()
        {
            var a = new float[] { 1, 2, 3, 4, 5, 6 };
            var output = Subtract(a, a);
            for(int i=0; i<a.Length; i++)
            {
                if (output[i] != 0f) throw new Exception("invalid Subtract");
            }
        }

        public static void SubtractTest2()
        {
            var a = new float[] { 1, 2, 3, 4, 5, 6 };
            var b = new float[] { 1, 2, 3, 4, 5, 6 };
            Subtract(ref a, b);
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != 0f) throw new Exception("invalid Subtract");
            }
        }

        public static void SubtractTestPerf(int iterations)
        {
            var a = new float[24];
            var b = new float[24];
            var rand = new Random();
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = (float)(rand.NextDouble() % 100) - 50f;
                b[i] = (float)(rand.NextDouble() % 100) - 50f;
            }

            for (int i = 0; i < iterations; i++)
            {
                var output = Subtract(a, b);
            }
        }

        //
        // multiply
        //
        public static void MultiplyTest()
        {
            var a = new float[] { 1, 2, 3, 4, 5, 6 };
            var org = new float[] { 1, 2, 3, 4, 5, 6 };
            Multiply(2f, ref a);
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != (2f * org[i])) throw new Exception("invalid Multiply");
            }

            a = new float[] { 1, 2, 3, 4, 5, 6 };
            Multiply(ref a, org);
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != (org[i] * org[i])) throw new Exception("invalid Multiply");
            }
        }

        public static void MultiplyTestPerf(int iterations)
        {
            var a = new float[24];
            var rand = new Random();
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = (float)(rand.NextDouble() % 100) - 50f;
            }

            for (int i = 0; i < iterations; i++)
            {
                Multiply(10f, ref a);
            }
        }

        public static void MultiplyTestPerf2(int iterations)
        {
            var a = new float[24];
            var b = new float[24];
            var rand = new Random();
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = (float)(rand.NextDouble() % 100) - 50f;
                b[i] = (float)(rand.NextDouble() % 100) - 50f;
            }

            for (int i = 0; i < iterations; i++)
            {
                Multiply(ref a, b);
            }
        }
    }
}
