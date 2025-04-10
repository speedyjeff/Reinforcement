using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using Learning;

namespace Maze
{   
    public class MyForm : Form
    {
        public MyForm()
        {
            // initialize
            var rows = 6;
            var columns = 8;
            GridLocation.MaxColumn = columns;

            // create the maze
            Maze = new Maze(ModelType.Q, rows, columns);

            // form details
            Width = 1000;
            Height = 700;

            // setup grid
            Grid = new Dictionary<GridLocation, BoxControl>();
            int top = 0;
            for (int r = 0; r < Maze.Rows; r++)
            {
                int left = 80;

                for (int c = 0; c < Maze.Columns; c++)
                {
                    var loc = new GridLocation() { Row = r, Column = c };
                    BoxControl box;

                    // choose box
                    if (!Maze.IsValid(loc)) box = new BoxControl(new Direction[0]);
                    else if (Maze.TryGetTerminal(loc, out double termValue)) box = new BoxControl(new Direction[] { Direction.Center });
                    else box = new BoxControl(new Direction[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right });

                    // add box
                    box.Top = top;
                    box.Left = left;
                    this.Controls.Add(box);
                    Grid.Add(loc, box);

                    // advance
                    left += 100;
                }
                top += 100;
            }

            // setup the advance button
            var step = new Button();
            step.Left = 0;
            step.Top = 0;
            step.Text = "Step";
            step.Click += MakeMove;
            this.Controls.Add(step);

            // counter label
            CounterLabel = new Label();
            CounterLabel.Left = 0;
            CounterLabel.Top = 50;
            CounterLabel.Text = "0";
            this.Controls.Add(CounterLabel);
            Counter = 0;

            CountToEndLabel = new Label();
            CountToEndLabel.Left = 0;
            CountToEndLabel.Top = 100;
            CountToEndLabel.Text = "0";
            this.Controls.Add(CountToEndLabel);
            CountToEnd = 0;

            AvgCountToEndLabel = new Label();
            AvgCountToEndLabel.Left = 0;
            AvgCountToEndLabel.Top = 150;
            AvgCountToEndLabel.Text = "0";
            this.Controls.Add(AvgCountToEndLabel);
            TotalCountToEnd = 0;
            MadeItToEnd = 0;

            // setup timer
            UiTimer = new System.Windows.Forms.Timer();
            UiTimer.Tick += MakeMove;
            UiTimer.Interval = 100;

            // buttons
            var start = new Button();
            start.Left = 0;
            start.Top = 200;
            start.Text = "Start";
            start.Click += delegate (object? sender, EventArgs e) { UiTimer.Start(); };
            this.Controls.Add(start);

            var stop = new Button();
            stop.Left = 0;
            stop.Top = 250;
            stop.Text = "Stop";
            stop.Click += delegate (object? sender, EventArgs e) { UiTimer.Stop(); };
            this.Controls.Add(stop);

            // set current as active
            Current = RandomStart(Maze.Rows, Maze.Columns);
            Grid[Current].SetActive();
        }

        public static void Main(string[] args)
        {
            MyForm f = new MyForm();
            Application.Run(f);
        }

        #region private
        private Dictionary<GridLocation, BoxControl> Grid;
        private Label CounterLabel;
        private Label CountToEndLabel;
        private Label AvgCountToEndLabel;
        private System.Windows.Forms.Timer UiTimer;
        private int Counter;
        private int CountToEnd;
        private double TotalCountToEnd;
        private double MadeItToEnd;
        private Maze Maze;
        private GridLocation Current;

        private void MakeMove(object? sender, System.EventArgs e)
        {
            // update counter
            CounterLabel.Text = (++Counter).ToString();
            CountToEndLabel.Text = (++CountToEnd).ToString();
            if (MadeItToEnd > 0) AvgCountToEndLabel.Text = (TotalCountToEnd / MadeItToEnd).ToString("n2");

            // adjust the previous box
            UpdateBox(Current);
            Grid[Current].SetInactive();

            // make move
            Current = Maze.MakeMove(Current);

            // set the current as active
            UpdateBox(Current);
            Grid[Current].SetActive();

            // check for terminal
            if (Maze.TryGetTerminal(Current, out double termvalue))
            {
                // set inactive
                Grid[Current].SetInactive();

                // book keeping
                TotalCountToEnd += CountToEnd;
                MadeItToEnd++;

                // reset
                CountToEnd = 0;
                Current = RandomStart(Maze.Rows, Maze.Columns);
            }
        }

        private void UpdateBox(GridLocation loc)
        {
            var max = double.MinValue;
            var max_d = Direction.Up;

            foreach (var d in new Direction[] { Direction.Down, Direction.Left, Direction.Right, Direction.Up, Direction.Center })
            {
                var qvalue = 0d;
                if (Maze.TryGetQValue(loc, d, out qvalue))
                {
                    Grid[loc][(Direction)d] = qvalue.ToString("n2");

                    if (qvalue > max)
                    {
                        max = qvalue;
                        max_d = d;
                    }
                }
            }

            // set the hottest path
            Grid[loc].HotPath(max_d);
        }

        private GridLocation RandomStart(int rows, int columns)
        {
            var rand = new Random();
            var row = rand.Next() % rows;
            var column = rand.Next() % columns;
            if (rand.Next() % 2 == 0)
            {
                // right to left
                if (rand.Next() % 2 == 0) row = 0;
                else row = rows - 1;
            }
            else
            {
                // top to bottom
                if (rand.Next() % 2 == 0) column = 0;
                else column = columns - 1;
            }
            return new GridLocation() { Row = row, Column = column }; // starting position
        }
        #endregion
    }
}