using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Globalization;

// http://yann.lecun.com/exdb/mnist/

namespace mnist
{
    public class Dataset
    {
        // data is flattened to a 1 dim array, but the original rows, and columns are persisted
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int Count { get; set; }
        public float[][] Data { get; set; }
        public int MagicNumber { get; set; }

        public static Dataset Read(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename)) throw new Exception("unknown file");

            var dataset = new Dataset();

            // read the stream
            var inputstream = new MemoryStream();
            using (FileStream binary = new FileStream(filename, FileMode.Open))
            {
                if (filename.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                {
                    using (GZipStream gz = new GZipStream(binary, CompressionMode.Decompress))
                    {
                        gz.CopyTo(inputstream);
                    }
                }
                else
                {
                    binary.CopyTo(inputstream);
                }
            }

            // seek the inputstream to 0
            inputstream.Seek(offset: 0, SeekOrigin.Begin);

            if (inputstream.Length <= 4) throw new Exception("invalid input stream");

            // parse the data
            var int32buffer = new byte[4];
            var normalize = false;

            // read in the magic number
            if (inputstream.Read(int32buffer, offset: 0, count: 4) != int32buffer.Length) throw new Exception("no header");
            dataset.MagicNumber = BitConverter.ToInt32(int32buffer.Reverse<byte>().ToArray());

            // read the count
            if (inputstream.Read(int32buffer, offset: 0, count: 4) != int32buffer.Length) throw new Exception("no header");
            dataset.Count = BitConverter.ToInt32(int32buffer.Reverse<byte>().ToArray());

            if (dataset.Count <= 0) throw new Exception($"input needs a positive number of elements");

            // setup output
            dataset.Data = new float[dataset.Count][];

            // read in data
            if (dataset.MagicNumber == 0x00000801)
            {
                // labels
                dataset.Rows = dataset.Columns = 1;
                normalize = false;
            }
            else if (dataset.MagicNumber == 0x00000803)
            {
                // images
                if (inputstream.Read(int32buffer, offset: 0, count: 4) != int32buffer.Length) throw new Exception("no header");
                dataset.Rows = BitConverter.ToInt32(int32buffer.Reverse<byte>().ToArray());

                if (inputstream.Read(int32buffer, offset: 0, count: 4) != int32buffer.Length) throw new Exception("no header");
                dataset.Columns = BitConverter.ToInt32(int32buffer.Reverse<byte>().ToArray());

                // transform the data into [0.0-1.0]
                normalize = true;
            }
            else
            {
                throw new Exception($"unknown file type : 0x{dataset.MagicNumber:x}");
            }

            if (dataset.Rows <= 0 || dataset.Columns <= 0) throw new Exception($"invalid row,column : {dataset.Rows},{dataset.Columns}");

            // read in all the data
            for(int i = 0; i<dataset.Count; i++)
            {
                // read
                var ldata = new byte[dataset.Rows * dataset.Columns];
                if (inputstream.Read(ldata, offset: 0, count: dataset.Rows * dataset.Columns) != (dataset.Rows * dataset.Columns)) 
                    throw new Exception("not enough data");

                // copy
                dataset.Data[i] = new float[dataset.Rows * dataset.Columns];
                for (int j = 0; j < ldata.Length; j++)
                {
                    if (normalize) dataset.Data[i][j] = (float)ldata[j] / 255f;
                    else dataset.Data[i][j] = (float)ldata[j];
                }
            }

            return dataset;
        }
    }
}
