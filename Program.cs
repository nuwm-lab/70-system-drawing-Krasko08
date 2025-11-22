using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GraphPlotter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Resize += (s, e) => this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawGraph(e.Graphics, this.ClientSize);
        }

        private void DrawGraph(Graphics g, Size clientSize)
        {
            g.Clear(Color.White);

            Pen axisPen = new Pen(Color.Black, 2);
            Pen graphPen = new Pen(Color.Blue, 2);

            int centerX = clientSize.Width / 2;
            int centerY = clientSize.Height / 2;

            g.DrawLine(axisPen, 0, centerY, clientSize.Width, centerY); // X-axis
            g.DrawLine(axisPen, centerX, 0, centerX, clientSize.Height); // Y-axis

            List<PointF> points = new List<PointF>();

            for (double x = 1.2; x <= 6.3; x += 0.2)
            {
                double y = 5 * Math.Tan(x + 7) / Math.Pow(x + 3, 2);

                float px = centerX + (float)(x * 50); // масштаб X
                float py = centerY - (float)(y * 50); // масштаб Y

                if (float.IsInfinity(py) || float.IsNaN(py)) continue;

                points.Add(new PointF(px, py));
            }

            if (points.Count > 1)
            {
                g.DrawLines(graphPen, points.ToArray());
            }
        }
    }
}
