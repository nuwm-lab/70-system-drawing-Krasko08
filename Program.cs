using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

// ррррргринг

namespace LabWork
{
    public class GraphForm : Form
    {
        private readonly List<PointF> _points;

        private const double XStart = 4.5;
        private const double XEnd = 16.4;
        private const double Step = 2.2;

        public GraphForm()
        {
            Text = "Графік y = (x^3 - 2) / (3 ln(x))";
            BackColor = Color.White;
            DoubleBuffered = true;

            _points = CalculatePoints();

            Resize += (s, e) => Invalidate();
        }

        private List<PointF> CalculatePoints()
        {
            var points = new List<PointF>();

            for (double x = XStart; x <= XEnd + 0.001; x += Step)
            {
                double y = (Math.Pow(x, 3) - 2) / (3 * Math.Log(x));
                points.Add(new PointF((float)x, (float)y));
            }

            return points;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_points.Count < 2)
                return;

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            DrawAxes(g);
            DrawGraph(g);
        }

        private void DrawAxes(Graphics g)
        {
            using Pen pen = new Pen(Color.Black, 2);

            // OX
            g.DrawLine(pen, 40, Height - 40, Width - 20, Height - 40);

            // OY
            g.DrawLine(pen, 40, 20, 40, Height - 40);
        }

        private void DrawGraph(Graphics g)
        {
            float minX = (float)XStart;
            float maxX = (float)XEnd;

            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (var p in _points)
            {
                minY = Math.Min(minY, p.Y);
                maxY = Math.Max(maxY, p.Y);
            }

            float graphWidth = Width - 80;
            float graphHeight = Height - 80;

            using Pen pen = new Pen(Color.Red, 2);

            for (int i = 0; i < _points.Count - 1; i++)
            {
                float x1 = 40 + ((_points[i].X - minX) / (maxX - minX)) * graphWidth;
                float y1 = Height - 40 - ((_points[i].Y - minY) / (maxY - minY)) * graphHeight;

                float x2 = 40 + ((_points[i + 1].X - minX) / (maxX - minX)) * graphWidth;
                float y2 = Height - 40 - ((_points[i + 1].Y - minY) / (maxY - minY)) * graphHeight;

                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }
    }

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new GraphForm());
        }
    }
}
