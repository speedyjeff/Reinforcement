using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Viewer
{
    public partial class DrawDigit : UserControl
    {
        public DrawDigit()
        {
            InitializeComponent();
            DoubleBuffered = true;
            IsMouseDown = false;
            PreviousMouse = new Point() { X = -1, Y = -1 };

            // call backs
            Paint += DrawDigit_Paint;
            MouseDown += DrawDigit_MouseDown;
            MouseUp += DrawDigit_MouseUp;
            MouseMove += DrawDigit_MouseMove;
            Resize += DrawDigit_Resize;

            // drawig surface
            DrawingImage = new Bitmap(width: Width, height: Height);
            DrawingGraphics = Graphics.FromImage(DrawingImage);
            Clear();

            // start timer
            OnPaintTimer = new System.Windows.Forms.Timer();
            OnPaintTimer.Interval = 50; // ms
            OnPaintTimer.Tick += OnPaintTimer_Tick;
            OnPaintTimer.Start();
        }

        public void Clear()
        {
            DrawingGraphics.Clear(Color.Black);
        }

        public Bitmap GetImage(int desiredWidth, int desiredHeight)
        {
            // copy current image and set to desired size
            var bitmap = new Bitmap(desiredWidth, desiredHeight);
            using(var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(DrawingImage, 0, 0, desiredWidth, desiredHeight);
            }
            return bitmap;
        }

        #region private
        private Bitmap DrawingImage;
        private Graphics DrawingGraphics;
        private Timer OnPaintTimer;

        private bool IsMouseDown;

        private Point PreviousMouse;
        private Pen WhitePen = new Pen(Color.White, width: 5f);

        private void OnPaintTimer_Tick(object? sender, EventArgs e)
        {
            // repaint
            Refresh();
        }

        private void DrawDigit_MouseMove(object? sender, MouseEventArgs e)
        {
            // exit early if the mouse is not down
            if (!IsMouseDown) return;

            // draw on the image
            if (PreviousMouse.X >= 0 && PreviousMouse.Y >= 0)
            {
                // draw a line between the current and previous point
                //DrawingGraphics.DrawLine(WhitePen, x1: PreviousMouse.X, y1: PreviousMouse.Y, x2: e.X, y2: e.Y);
                DrawingGraphics.DrawEllipse(WhitePen, x: e.X, y: e.Y, width: 5f, height: 5f);

                //System.Diagnostics.Debug.WriteLine($"{PreviousMouse.X},{PreviousMouse.Y} to {e.X},{e.Y}");
            }
            
            // store previous
            PreviousMouse.X = e.X;
            PreviousMouse.Y = e.Y;
        }

        private void DrawDigit_MouseUp(object? sender, MouseEventArgs e)
        {
            IsMouseDown = false;
            PreviousMouse.X = -1;
            PreviousMouse.Y = -1;
        }

        private void DrawDigit_MouseDown(object? sender, MouseEventArgs e)
        {
            IsMouseDown = true;
            PreviousMouse.X = e.X;
            PreviousMouse.Y = e.Y;
        }

        private void DrawDigit_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(DrawingImage, 0, 0, Width, Height);
        }

        private void DrawDigit_Resize(object? sender, EventArgs e)
        {
            DrawingImage = new Bitmap(width: Width, height: Height);
            DrawingGraphics = Graphics.FromImage(DrawingImage);
            Clear();
        }
        #endregion
    }
}
