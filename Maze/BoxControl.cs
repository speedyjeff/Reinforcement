using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Maze
{
    public class BoxControl : UserControl
    {
        public BoxControl(Direction[] directions)
        {
            this.Width = 100;
            this.Height = 100;
            IsTerminal = false;

            if (directions.Length == 0) this.BackColor = Color.Black;
            else this.BackColor = Color.White;

            Label top = new Label();
            top.Text = "-------------------------";
            top.Left = 0;
            top.Top = 0;
            this.Controls.Add(top);

            Positions = new Label[(int)Direction.MAX];

            if (directions.Contains(Direction.Up))
            {
                Positions[(int)Direction.Up] = new Label();
                Positions[(int)Direction.Up].Text = "0.00";
                Positions[(int)Direction.Up].Left = 40;
                Positions[(int)Direction.Up].Top = 25;
                this.Controls.Add(Positions[(int)Direction.Up]);
            }

            if (directions.Contains(Direction.Down))
            {
                Positions[(int)Direction.Down] = new Label();
                Positions[(int)Direction.Down].Text = "0.00";
                Positions[(int)Direction.Down].Left = 40;
                Positions[(int)Direction.Down].Top = 80;
                this.Controls.Add(Positions[(int)Direction.Down]);
            }

            if (directions.Contains(Direction.Left))
            {
                Positions[(int)Direction.Left] = new Label();
                Positions[(int)Direction.Left].Text = "0.00";
                Positions[(int)Direction.Left].Width = 40;
                Positions[(int)Direction.Left].Left = 0;
                Positions[(int)Direction.Left].Top = 50;
                this.Controls.Add(Positions[(int)Direction.Left]);
            }

            if (directions.Contains(Direction.Right))
            {
                Positions[(int)Direction.Right] = new Label();
                Positions[(int)Direction.Right].Text = "0.00";
                Positions[(int)Direction.Right].Left = 70;
                Positions[(int)Direction.Right].Top = 50;
                this.Controls.Add(Positions[(int)Direction.Right]);
            }

            if (directions.Contains(Direction.Center))
            {
                Positions[(int)Direction.Center] = new Label();
                Positions[(int)Direction.Center].Text = "0.00";
                Positions[(int)Direction.Center].Left = 40;
                Positions[(int)Direction.Center].Top = 50;
                this.Controls.Add(Positions[(int)Direction.Center]);

                IsTerminal = true;
            }

            if (IsTerminal) BackColor = Color.Gray;
        }

        public string this[Direction d]
        {
            set
            {
                if ((int)d < 0 || (int)d >= (int)Direction.MAX) throw new Exception("Direction out of bounds");
                if (Positions[(int)d] != null) Positions[(int)d].Text = value;
            }

        }

        public void SetActive()
        {
            this.BackColor = Color.Red;
        }

        public void SetInactive()
        {
            if (IsTerminal) this.BackColor = Color.Gray;
            else this.BackColor = Color.White;
        }

        public void HotPath(Direction d)
        {
            if ((int)d < 0 || (int)d >= (int)Direction.MAX) throw new Exception("Direction out of bounds");

            // clear the current color
            for (int i = (int)Direction.Up; i < (int)Direction.MAX; i++)
            {
                if (Positions[i] != null) Positions[i].ForeColor = Color.LightGray;
            }

            if (Positions[(int)d] != null) Positions[(int)d].ForeColor = Color.Green;
        }

        #region private
        private Label[] Positions;
        public bool IsTerminal;
        #endregion
    }
}
