using Learning;
using mnist;
using System.Data;
using System.Windows.Forms;

namespace Viewer
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
            Width = 1024;
            Height = 600;

            // setup controls
            var table = new TableLayoutPanel();
            table.RowCount = 6;
            table.ColumnCount = DisplayNumber;
            table.Width = Width;
            table.Height = Height;
            Controls.Add(table);

            // image display
            DisplayImages = new PictureBox[DisplayNumber];
            DisplayLabels = new Label[DisplayNumber];
            for (int i = 0; i < DisplayImages.Length; i++)
            {
                DisplayImages[i] = new PictureBox() { Width = 50, Height = 50 };
                DisplayLabels[i] = new Label() { Width = 200, Height = 50 };

                table.Controls.Add(DisplayImages[i], column: i, row: 0);
                table.Controls.Add(DisplayLabels[i], column: i, row: 1);
            }

            DisplayText = new TextBox();
            DisplayText.Text = "0";
            table.Controls.Add(DisplayText, column: 0, row: 2);

            var button = new Button() { Text = "display", Height = 50, Width = 200 };
            button.Click += Display_Click;
            table.Controls.Add(button, column: 1, row: 2);

            button = new Button() { Text = "next", Height = 50, Width = 200 };
            button.Click += Next_Click;
            table.Controls.Add(button, column: 2, row: 2);

            // load data
            button = new Button() { Text = "load data", Height = 50, Width = 200 };
            button.Click += LoadData_Click;
            table.Controls.Add(button, column: 0, row: 3);

            // draw surface
            DrawingControl = new DrawDigit() { Width = 256, Height = 256};
            table.Controls.Add(DrawingControl, column: 0, row: 4);

            button = new Button() { Text = "clear", Height = 50, Width = 200 };
            button.Click += (sender, e) => { DrawingControl.Clear(); };
            table.Controls.Add(button, column: 1, row: 4);

            button = new Button() { Text = "predict", Height = 50, Width = 200 };
            button.Click += PredictButton_Click;
            table.Controls.Add(button, column: 2, row: 4);

            PredictionText = new Label() { Width = 200, Height = 50 };
            PredictionText.Text = "";
            table.Controls.Add(PredictionText, column: 3, row: 4);

            // load the model
            var tmppath = Path.GetTempFileName();
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                foreach (var resourceName in assembly.GetManifestResourceNames())
                {
                    if (resourceName.Contains("hidden10.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        // dump this file to disk
                        using (var stream = assembly.GetManifestResourceStream(resourceName))
                        {
                            using(var reader = new StreamReader(stream))
                            {
                                File.WriteAllText(tmppath, reader.ReadToEnd());
                            }
                        }
                    }
                }

                // load the model
                Network = NeuralNetwork.Load(tmppath);
            }
            finally
            {
                if (File.Exists(tmppath)) File.Delete(tmppath);
            }
        }

        #region private
        private PictureBox[] DisplayImages;
        private Label[] DisplayLabels;
        private TextBox DisplayText;
        private DrawDigit DrawingControl;
        private Label PredictionText;

        private Dataset Labels;
        private Dataset Images;

        private NeuralNetwork Network;

        private const int DisplayNumber = 5;

        private void LoadData_Click(object? sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (var filename in openFileDialog.FileNames)
                {
                    var dataset = mnist.Dataset.Read(filename);
                    if (dataset.MagicNumber == 0x00000801) Labels = dataset;
                    else if (dataset.MagicNumber == 0x00000803) Images = dataset;
                }
            }

            // if loaded
            if (Labels != null && Images != null)
            {
                // display data
                DisplayText.Text = "0";
                Display_Click(sender: null, e: null);
            }
        }

        private void Display_Click(object? sender, EventArgs e)
        {
            if (Labels == null || Images == null) return;

            // read the starting point from 
            if (Int32.TryParse(DisplayText.Text, out int start))
            {
                // display the first N images and labels
                for (int i = start; i < start + DisplayNumber && start < Labels.Count; i++)
                {
                    DisplayLabels[i - start].Text = $"{i} : {Labels.Data[i][0]}";
                    DisplayImages[i - start].Image = ToBitmap(Images.Data[i], Images.Rows, Images.Columns);
                }
            }
            else
            {
                DisplayText.Text = "<need valid number>";
            }
        }

        private void Next_Click(object? sender, EventArgs e)
        {
            // read the starting point from 
            if (Int32.TryParse(DisplayText.Text, out int start))
            {
                DisplayText.Text = $"{start + DisplayNumber}";
                Display_Click(sender: null, e: null);
            }
        }

        private Bitmap ToBitmap(float[] bytes, int rows, int columns)
        {
            // make an image with this data
            var bitmap = new Bitmap(columns, rows);
            // set the pixels
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    bitmap.SetPixel(r, c, Color.FromArgb(
                        red: (byte)(bytes[(r * rows) + c] * 255),
                        green: (byte)(bytes[(r * rows) + c] * 255),
                        blue: (byte)(bytes[(r * rows) + c] * 255))
                        );
                }
            }

            // flip to accomodate the pixel row format
            bitmap.RotateFlip(RotateFlipType.Rotate90FlipX);

            return bitmap;
        }


        private void PredictButton_Click(object? sender, EventArgs e)
        {
            var desiredWidth = 28;
            var desiredHieght = 28;

            // get the image
            var bitmap = DrawingControl.GetImage(desiredWidth, desiredHieght);

            // convert to gray scale float[] input
            var input = new float[desiredWidth * desiredHieght];
            for(var y=0; y<bitmap.Height; y++)
            {
                for(var x = 0; x<bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    input[(y * desiredWidth) + x] = (float)((byte)((pixel.R + pixel.G + pixel.B) / 3)) / 255f;
                }
            }

            // run the model
            var result = Network.Evaluate(input);

            // display the result
            PredictionText.Text = $"{result.Result}"; 
        }
        #endregion
    }
}