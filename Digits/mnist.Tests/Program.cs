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
            var data = new byte[20];
            for (int i = 0; i < data.Length; i++) data[i] = (byte)(i % 10);

            // write the data
            using (var stream = File.Open(tmpfile, FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    // magic number
                    writer.Write(0x01080000); // MSB first (high endian) format 
                    // number of items
                    writer.Write(0x14000000); // MSB first(high endian) format
                    for (int i = 0; i < data.Length; i++) writer.Write(data[i]);
                }
            }

            // read in the data
            var dataset = mnist.Dataset.Read(tmpfile);

            if (dataset.MagicNumber != 0x00000801) throw new Exception("wrong magic number");
            if (dataset.Count != data.Length) throw new Exception("invalid length");

            // validate
            for(int i=0; i<dataset.Count; i++)
            {
                if (dataset.Data[i].Length != 1) throw new Exception("invalid data length");
                if ((byte)dataset.Data[i][0] != data[i]) throw new Exception("invalid data");
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
            var rows = 3;
            var columns = 3;
            var data = new byte[2][];
            var constdata = new byte[] { 0, 255, 100, 125, 254, 1, 34, 42, 255 };
            for (int r = 0; r < data.Length; r++)
            {
                data[r] = new byte[rows*columns];
                for(int i=0; i < data[r].Length; i++)
                {
                    data[r][i] = constdata[i];
                }
            }

            // write the data
            using (var stream = File.Open(tmpfile, FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    // magic number
                    writer.Write(0x03080000); // MSB first (high endian) format 
                    // number of items
                    writer.Write(0x02000000); // MSB first(high endian) format
                    // rows
                    writer.Write(0x03000000); // MSB first(high endian) format
                    // columns
                    writer.Write(0x03000000); // MSB first(high endian) format
                    for (int i = 0; i < data.Length; i++) writer.Write(data[i]);
                }
            }

            // read in the data
            var dataset = mnist.Dataset.Read(tmpfile);

            if (dataset.MagicNumber != 0x00000803) throw new Exception("wrong magic number");
            if (dataset.Count != data.Length) throw new Exception("invalid length");
            if (dataset.Rows != rows) throw new Exception("invalid rows");
            if (dataset.Columns != columns) throw new Exception("invalid columns");

            // validate
            for (int i = 0; i < dataset.Count; i++)
            {
                if (dataset.Data[i].Length != rows*columns) throw new Exception("invalid data length");
                for (int j = 0; j < dataset.Data[i].Length; j++)
                {
                    if (dataset.Data[i][j] != (float)data[i][j]/255f) throw new Exception("invalid data");
                }
            }
        }
        finally
        {
            if (File.Exists(tmpfile)) File.Delete(tmpfile);
        }
    }
}