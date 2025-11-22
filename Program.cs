using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GraphPlotter
{
    public partial class Form1 : Form
    {
        private double xMin = 1.2;
        private double xMax = 6.3;
        private double step = 0.01;

        private double yMin;
        private double yMax;

        private const int margin = 40;

        public Form1()
        {
            InitializeComponent();

            // Увімкнення подвійного буфера та перерисовки при resize
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);

            UpdateStyles();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ComputeYBounds();
            DrawGraph(e.Graphics);
        }

        // ------------------ ОСНОВНЕ МАЛЮВАННЯ ------------------------
        private void DrawGraph(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            using var axisPen = new Pen(Color.Black, 2);
            using var gridPen = new Pen(Color.LightGray, 1) { DashStyle = DashStyle.Dash };
            using var graphPen = new Pen(Color.Blue, 2);
            using var labelBrush = new SolidBrush(Color.Black);
            using var labelFont = new Font("Segoe UI", 9);

            DrawGrid(g, gridPen);
            DrawAxes(g, axisPen, labelBrush, labelFont);
            DrawFunction(g, graphPen);
        }

        // ------------------ СІТКА ------------------------
        private void DrawGrid(Graphics g, Pen gridPen)
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            int div = 10;

            for (int i = 0; i <= div; i++)
            {
                float x = margin + i * (w - 2 * margin) / (float)div;
                g.DrawLine(gridPen, x, margin, x, h - margin);
            }

            for (int i = 0; i <= div; i++)
            {
                float y = margin + i * (h - 2 * margin) / (float)div;
                g.DrawLine(gridPen, margin, y, w - margin, y);
            }
        }

        // ------------------ ОСІ ТА ПІДПИСИ ------------------------
        private void DrawAxes(Graphics g, Pen axisPen, Brush labelBrush, Font font)
        {
            PointF x1 = new PointF(WorldToScreenX(xMin), WorldToScreenY(0));
            PointF x2 = new PointF(WorldToScreenX(xMax), WorldToScreenY(0));
            PointF y1 = new PointF(WorldToScreenX(0), WorldToScreenY(yMin));
            PointF y2 = new PointF(WorldToScreenX(0), WorldToScreenY(yMax));

            g.DrawLine(axisPen, x1, x2);
            g.DrawLine(axisPen, y1, y2);

            DrawAxisTicks(g, labelBrush, font);
        }

        private void DrawAxisTicks(Graphics g, Brush brush, Font font)
        {
            int ticks = 8;

            for (int i = 0; i <= ticks; i++)
            {
                double x = xMin + i * (xMax - xMin) / ticks;
                float sx = WorldToScreenX(x);

                g.DrawLine(Pens.Black, sx, WorldToScreenY(0) - 4, sx, WorldToScreenY(0) + 4);

                var s = g.MeasureString(x.ToString("F2"), font);
                g.DrawString(x.ToString("F2"), font, brush, sx - s.Width / 2, WorldToScreenY(0) + 6);
            }

            for (int i = 0; i <= ticks; i++)
            {
                double y = yMin + i * (yMax - yMin) / ticks;
                float sy = WorldToScreenY(y);

                g.DrawLine(Pens.Black, WorldToScreenX(0) - 4, sy, WorldToScreenX(0) + 4, sy);

                var s = g.MeasureString(y.ToString("F2"), font);
                g.DrawString(y.ToString("F2"), font, brush, WorldToScreenX(0) - s.Width - 6, sy - s.Height / 2);
            }
        }

        // ------------------ МАЛЮВАННЯ ФУНКЦІЇ ------------------------
        private void DrawFunction(Graphics g, Pen pen)
        {
            var segments = ComputeSegments();

            foreach (var seg in segments)
            {
                if (seg.Count >= 2)
                    g.DrawLines(pen, seg.ToArray());
                else if (seg.Count == 1)
                    g.FillEllipse(Brushes.Blue, seg[0].X - 2, seg[0].Y - 2, 4, 4);
            }
        }

        private List<List<PointF>> ComputeSegments()
        {
            var segments = new List<List<PointF>>();
            var current = new List<PointF>();

            PointF? prev = null;

            for (double x = xMin; x <= xMax; x += step)
            {
                double y = ComputeFunction(x);

                if (double.IsNaN(y) || double.IsInfinity(y))
                {
                    if (current.Count > 0)
                    {
                        segments.Add(new List<PointF>(current));
                        current.Clear();
                    }
                    prev = null;
                    continue;
                }

                PointF p = WorldToScreen(x, y);

                if (prev.HasValue && Math.Abs(p.Y - prev.Value.Y) > 150)
                {
                    segments.Add(new List<PointF>(current));
                    current.Clear();
                }

                current.Add(p);
                prev = p;
            }

            if (current.Count > 0)
                segments.Add(current);

            return segments;
        }

        // ------------------ ОБЧИСЛЕННЯ ------------------------
        private double ComputeFunction(double x)
        {
            double denom = Math.Pow(x + 3, 2);
            if (denom < 1e-6)
                return double.NaN;

            double t = Math.Tan(x + 7);
            if (Math.Abs(t) > 1e6)
                return double.NaN;

            return 5 * t / denom;
        }

        private void ComputeYBounds()
        {
            var values = new List<double>();

            for (double x = xMin; x <= xMax; x += step)
            {
                double y = ComputeFunction(x);

                if (!double.IsNaN(y) && !double.IsInfinity(y) && Math.Abs(y) < 10000)
                    values.Add(y);
            }

            if (values.Count == 0)
            {
                yMin = -10;
                yMax = 10;
                return;
            }

            double min = double.MaxValue;
            double max = double.MinValue;

            foreach (double v in values)
            {
                if (v < min) min = v;
                if (v > max) max = v;
            }

            double pad = (max - min) * 0.1;
            yMin = min - pad;
            yMax = max + pad;
        }

        // ------------------ КООРДИНАТИ ------------------------
        private float WorldToScreenX(double x)
        {
            double w = ClientSize.Width - 2 * margin;
            return margin + (float)((x - xMin) * (w / (xMax - xMin)));
        }

        private float WorldToScreenY(double y)
        {
            double h = ClientSize.Height - 2 * margin;
            return margin + (float)((yMax - y) * (h / (yMax - yMin)));
        }

        private PointF WorldToScreen(double x, double y)
        {
            return new PointF(WorldToScreenX(x), WorldToScreenY(y));
        }
    }
}
