using mnist;
using System;
using System.IO;

public class MnistTests
{
    public static void Main()
    {
        Labels();
        Images();
    }

    /*
     * Rules from http://yann.lecun.com/exdb/mnist/
     * 
      TRAINING SET LABEL FILE (train-labels-idx1-ubyte):
      [offset] [type]          [value]          [description]
      0000     32 bit integer  0x00000801(2049) magic number (MSB first)
      0004     32 bit integer  60000            number of items
      0008     unsigned byte   ??               label
      0009     unsigned byte   ??               label
      ........
      xxxx     unsigned byte   ??               label
      The labels values are 0 to 9.
    */

    public static void Labels()
    {
        // write to disk and validate output
        var tmpfile = Path.GetTempFileName();
        try
        {
            // create data
            var initdata = new Dataset()
            {
                MagicNumber = 0x00000801,
                Rows = 1,
                Columns = 1,
                Count = 20
            };
            initdata.Data= new float[initdata.Count][];
            for (int i = 0; i < initdata.Data.Length; i++)
            {
                initdata.Data[i] = new float[1];
                initdata.Data[i][0] = (byte)(i % 10);
            }

            // write the data
            Dataset.Write(tmpfile, initdata);

            // read in the data
            var dataset = mnist.Dataset.Read(tmpfile);

            if (dataset.MagicNumber != initdata.MagicNumber) throw new Exception("wrong magic number");
            if (dataset.Count != initdata.Count) throw new Exception("invalid length");

            // validate
            for(int i=0; i<dataset.Count; i++)
            {
                if (dataset.Data[i].Length != 1) throw new Exception("invalid data length");
                if ((byte)dataset.Data[i][0] != initdata.Data[i][0]) throw new Exception("invalid data");
            }
        }
        finally
        {
            if (File.Exists(tmpfile)) File.Delete(tmpfile);
        }
    }

    /*
     * Rules from http://yann.lecun.com/exdb/mnist/
     * 
      TRAINING SET IMAGE FILE (train-images-idx3-ubyte):
      [offset] [type]          [value]          [description]
      0000     32 bit integer  0x00000803(2051) magic number
      0004     32 bit integer  60000            number of images
      0008     32 bit integer  28               number of rows
      0012     32 bit integer  28               number of columns
      0016     unsigned byte   ??               pixel
      0017     unsigned byte   ??               pixel
      ........
      xxxx     unsigned byte   ??               pixel
    */

    public static void Images()
    {
        // write to disk and validate output
        var tmpfile = Path.GetTempFileName();
        try
        {
            // create data
            var initdata = new Dataset()
            {
                MagicNumber = 0x00000803,
                Rows = 3,
                Columns = 3,
                Count = 2
            };
            initdata.Data = new float[initdata.Count][];
            var constdata = new byte[] { 0, 255, 100, 125, 254, 1, 34, 42, 255 };
            for (int r = 0; r < initdata.Data.Length; r++)
            {
                initdata.Data[r] = new float[initdata.Rows*initdata.Columns];
                for(int i=0; i < initdata.Data[r].Length; i++)
                {
                    initdata.Data[r][i] = constdata[i];
                }
            }

            // write the data
            Dataset.Write(tmpfile, initdata);

            // read in the data
            var dataset = mnist.Dataset.Read(tmpfile);

            if (dataset.MagicNumber != initdata.MagicNumber) throw new Exception("wrong magic number");
            if (dataset.Count != initdata.Data.Length) throw new Exception("invalid length");
            if (dataset.Rows != initdata.Rows) throw new Exception("invalid rows");
            if (dataset.Columns != initdata.Columns) throw new Exception("invalid columns");

            // validate
            for (int i = 0; i < dataset.Count; i++)
            {
                if (dataset.Data[i].Length != initdata.Rows*initdata.Columns) throw new Exception("invalid data length");
                for (int j = 0; j < dataset.Data[i].Length; j++)
                {
                    if (dataset.Data[i][j] != (float)initdata.Data[i][j]/255f) throw new Exception("invalid data");
                }
            }
        }
        finally
        {
            if (File.Exists(tmpfile)) File.Delete(tmpfile);
        }
    }
}