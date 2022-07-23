using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Learning.Batch
{
    // class the represents a rectangular 2d matrix of any dimensions
    public class Matrix
    {
        public Matrix()
        {
            Rows = Columns = 0;
            Values = new double[0][];
        }

        public static Matrix Create(int rows, int columns, Func<int,int,double> initialize)
        {
            if (rows <= 0 || columns <= 0) throw new Exception("must provide positive row,column");
            if (initialize == null) throw new Exception("must provide an initialization function");

            // initialize
            var matrix = new Matrix();
            matrix.Rows = rows;
            matrix.Columns = columns;

            // fill in the values
            matrix.Values = new double[rows][];
            for(int r=0; r<rows; r++)
            {
                matrix.Values[r] = new double[columns];
                for(int c = 0; c<columns; c++)
                {
                    matrix.Values[r][c] = initialize(r, c);
                }
            }

            return matrix;
        }

        public static Matrix Create(Matrix m)
        {
            return Create(m.Values);
        }

        public static Matrix Create(double[][] values)
        {
            if (values == null || values.Length <= 0 ||
                values[0] == null || values[0].Length <= 0) throw new Exception("must provide a rectangular input matrix of values");

            // intialize
            var matrix = new Matrix();
            matrix.Rows = values.Length;
            matrix.Columns = values[0].Length; // verified as rectangular below

            // fill in the values
            matrix.Values = new double[values.Length][];
            for (int r = 0; r < matrix.Values.Length; r++)
            {
                if (values[0].Length != values[r].Length) throw new Exception("must provide a rectangular input matrix of values");
                matrix.Values[r] = new double[values[r].Length];
                for (int c = 0; c < matrix.Values[r].Length; c++)
                {
                    matrix.Values[r][c] = values[r][c];
                }
            }

            return matrix;
        }

        public int Rows { get; private set; }
        public int Columns { get; private set; }

        public Matrix Foreach(Func<int,int,double,double> update)
        {
            // apply the update function to every value
            if (update == null) throw new Exception("must provide a valid update function");
            var values = new double[Values.Length][];
            for (int r = 0; r < values.Length; r++)
            {
                values[r] = new double[Values[r].Length];
                for (int c = 0; c < values[r].Length; c++)
                {
                    values[r][c] = update(r, c, Values[r][c]);
                }
            }

            return Matrix.Create(values);
        }

        public Matrix Transpose()
        {
            // swap the columns with rows
            //
            // eg. T([[1,2,3], == [[1,4],
            //       [4,5,6]])     [2,5],
            //                     [3,6]]
            var tValues = new double[Values[0].Length][];
            for(int r=0; r<tValues.Length; r++)
            {
                tValues[r] = new double[Values.Length];
                for(int c=0; c < tValues[r].Length; c++)
                {
                    tValues[r][c] = Values[c][r];
                }
            }

            return Create(tValues);
        }

        public Matrix Dot(Matrix m)
        {
            if (Columns == m.Rows && m.Columns == 1)
            {
                // hadamard product - element wise multiplation
                //
                // eg. [[3,5,7], x [[1], = [[3*1 + 5*0 + 7*2],
                //      [4,9,8]]    [0],    [4*1 + 9*0 + 8*2]]
                //                  [2]]   
                var hvalues = new double[Rows][];
                for (int r = 0; r < hvalues.Length; r++)
                {
                    hvalues[r] = new double[m.Columns];
                    for (int c = 0; c < hvalues[r].Length; c++)
                    {
                        hvalues[r][0] += Values[r][c] * m.Values[c][0];
                    }
                }

                return Matrix.Create(hvalues);
            }
            else if (Columns == 1 && m.Rows == 1)
            {
                // hadamard product
                //
                // [[1],                 [[1,2,3,4],
                //  [2], x [[1,2,3,4]] =  [2,4,6,8],
                //  [3]]                  [3,6,9,12]]
                var hvalues = new double[Rows][];
                for(int r=0; r<hvalues.Length; r++)
                {
                    hvalues[r] = new double[m.Columns];
                    for(int c=0; c < hvalues[r].Length; c++)
                    {
                        hvalues[r][c] = Values[r][0] * m.Values[0][c];
                    }
                }

                return Matrix.Create(hvalues);
            }
            else if (Rows == m.Rows && Columns == m.Columns)
            {
                // matrix multiplipicaiton
                //
                // eg. [[1,2,3], * [[3,3,3], = [[3,6,9],
                //      [2,2,2]]    [1,2,3]]    [2,4,6]]
                var mvalues = new double[Rows][];
                for (int r = 0; r < mvalues.Length; r++)
                {
                    mvalues[r] = new double[Columns];
                    for (int c = 0; c < mvalues[r].Length; c++)
                    {
                        mvalues[r][c] = Values[r][c] * m.Values[r][c];
                    }
                }

                return Matrix.Create(mvalues);
            }
            else throw new Exception("wrong shape");
        }

        public Matrix Subtract(Matrix m)
        {
            // subtract two matrices
            //
            // eg. [[1,2,3], - [[0,1,2], = [[1,1,1],
            //      [3,4,5]]    [5,4,3]]    [-2,0,2]]
            if (m.Rows != Rows || m.Columns != Columns) throw new Exception("must have the same dimensions");
            var svalues = new double[Values.Length][];
            for (int r = 0; r < svalues.Length; r++)
            {
                svalues[r] = new double[Values[r].Length];
                for (int c = 0; c < svalues[r].Length; c++)
                {
                    svalues[r][c] = Values[r][c] - m.Values[r][c];
                }
            }

            return Matrix.Create(svalues);
        }

        public Matrix Addition(Matrix m)
        {
            // subtract two matrices
            //
            // eg. [[1,2,3], - [[0,1,2], = [[1,1,1],
            //      [3,4,5]]    [5,4,3]]    [-2,0,2]]
            if (m.Rows != Rows || m.Columns != Columns) throw new Exception("must have the same dimensions");
            var avalues = new double[Values.Length][];
            for (int r = 0; r < avalues.Length; r++)
            {
                avalues[r] = new double[Values[r].Length];
                for (int c = 0; c < avalues[r].Length; c++)
                {
                    avalues[r][c] = Values[r][c] + m.Values[r][c];
                }
            }

            return Matrix.Create(avalues);
        }

        public Matrix Multiply(double value)
        {
            // multiply value throughout the matrix
            //
            // eg. [[1,2,3]] * 2 = [[2,4,6]]
            var svalues = new double[Values.Length][];
            for (int r = 0; r < svalues.Length; r++)
            {
                svalues[r] = new double[Values[r].Length];
                for (int c = 0; c < svalues[r].Length; c++)
                {
                    svalues[r][c] = Values[r][c] * value;
                }
            }

            return Matrix.Create(svalues);
        }

        #region private
        private double[][] Values;
        #endregion
    }
}
