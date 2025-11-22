using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GraphPlotter
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize(); // .NET 6+ helper; safe to call in modern templates
            Application.Run(new Form1());
        }
    }

    public class Form1 : Form
    {
        // --- World coordinates (can be made user-editable later) ---
        private double xMin = 1.2;
        private double xMax = 6.3;
        private double initialXStep = 0.01; // initial sampling step (world units)

        // yMin/yMax will be computed automatically from sampled function values
        private double yMin = -10;
        private double yMax = 10;

        // --- UI / drawing params ---
        private const int margin = 40;
        private readonly Pen axisPen = new Pen(Color.Black, 2);
        private readonly Pen gridPen = new Pen(Color.LightGray, 1) { DashStyle = DashStyle.Dash };
        private readonly Pen graphPen = new Pen(Color.Blue, 2);
        private readonly Brush labelBrush = Brushes.Black;
        private readonly Font labelFont = new Font("Segoe UI", 9);

        // Controls
        private CheckBox chkPointMode;
        private Label lblRange;

        public Form1()
        {
            Text = "Графік функції: y = 5 * tan(x + 7) / (x + 3)^2";
            ClientSize = new Size(900, 650);

            // Double buffering + styles to reduce flicker
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            this.UpdateStyles();

            // Create simple UI controls
            chkPointMode = new CheckBox
            {
                Text = "Точковий режим",
                Checked = false,
                AutoSize = true,
                Location = new Point(10, 8)
            };
            chkPointMode.CheckedChanged += (s, e) => Invalidate();

            lblRange = new Label
            {
                Text = $"x ∈ [{xMin:F2}, {xMax:F2}]",
                AutoSize = true,
                Location = new Point(140, 9)
            };

            Controls.Add(chkPointMode);
            Controls.Add(lblRange);

            // Redraw on resize
            this.Resize += (s, e) => Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Compute y-bounds from function sampling (ignoring huge values and infinities)
            ComputeYBounds();

            // Draw grid, axes and graph
            DrawGrid(g);
            DrawAxes(g);
            DrawGraph(g);
        }

        private void DrawGrid(Graphics g)
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;

            // draw simple vertical/horizontal grid lines (5 divisions each)
            int divisionsX = 8;
            int divisionsY = 8;

            for (int i = 0; i <= divisionsX; i++)
            {
                float x = margin + i * (float)((w - 2 * margin) / (double)divisionsX);
                g.DrawLine(gridPen, x, margin, x, h - margin);
            }

            for (int j = 0; j <= divisionsY; j++)
            {
                float y = margin + j * (float)((h - 2 * margin) / (double)divisionsY);
                g.DrawLine(gridPen, margin, y, w - margin, y);
            }
        }

        private void DrawAxes(Graphics g)
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;

            // origin may lie outside the visible area; clamp axis drawing to client rect bounds
            PointF left = new PointF(margin, WorldToScreenY(0));
            PointF right = new PointF(w - margin, WorldToScreenY(0));
            g.DrawLine(axisPen, left, right); // X axis (at y=0 if visible)

            PointF top = new PointF(WorldToScreenX(0), margin);
            PointF bottom = new PointF(WorldToScreenX(0), h - margin);
            g.DrawLine(axisPen, top, bottom); // Y axis (at x=0 if visible)

            // Draw simple ticks and labels on axes
            DrawAxisTicksAndLabels(g);
        }

        private void DrawAxisTicksAndLabels(Graphics g)
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;

            // X ticks: 8 ticks across xMin..xMax
            int ticksX = 8;
            for (int i = 0; i <= ticksX; i++)
            {
                double x = xMin + i * (xMax - xMin) / ticksX;
                float sx = WorldToScreenX(x);
                float sy = WorldToScreenY(0);
                if (sx >= margin - 1 && sx <= w - margin + 1)
                {
                    g.DrawLine(Pens.Black, sx, sy - 4, sx, sy + 4);
                    string txt = x.ToString("F2");
                    var size = g.MeasureString(txt, labelFont);
                    g.DrawString(txt, labelFont, labelBrush, sx - size.Width / 2, sy + 6);
                }
            }

            // Y ticks: 8 ticks across yMin..yMax
            int ticksY = 8;
            for (int i = 0; i <= ticksY; i++)
            {
                double y = yMin + i * (yMax - yMin) / ticksY;
                float sy = WorldToScreenY(y);
                float sx = WorldToScreenX(0);
                if (sy >= margin - 1 && sy <= h - margin + 1)
                {
                    g.DrawLine(Pens.Black, sx - 4, sy, sx + 4, sy);
                    string txt = y.ToString("F2");
                    var size = g.MeasureString(txt, labelFont);
                    g.DrawString(txt, labelFont, labelBrush, Math.Max(2, sx - size.Width - 8), sy - size.Height / 2);
                }
            }
        }

        private void DrawGraph(Graphics g)
        {
            // Build segments (each segment = contiguous set of screen points)
            var segments = BuildPlotSegments();

            bool pointMode = chkPointMode.Checked;
            float pointSize = 3f;

            foreach (var seg in segments)
            {
                if (seg.Count == 0) continue;

                if (pointMode)
                {
                    foreach (var p in seg)
                    {
                        g.FillEllipse(Brushes.Blue, p.X - pointSize, p.Y - pointSize, pointSize * 2, pointSize * 2);
                    }
                }
                else
                {
                    if (seg.Count >= 2)
                    {
                        g.DrawLines(graphPen, seg.ToArray());
                    }
                    else if (seg.Count == 1)
                    {
                        // single point - draw small marker
                        var p = seg[0];
                        g.FillEllipse(Brushes.Blue, p.X - pointSize, p.Y - pointSize, pointSize * 2, pointSize * 2);
                    }
                }
            }
        }

        /// <summary>
        /// Build list of plot segments converting world samples to screen points.
        /// Splits into new segment when encountering NaN/Infinity or large vertical jump (asymptote).
        /// </summary>
        private List<List<PointF>> BuildPlotSegments()
        {
            var segments = new List<List<PointF>>();
            var current = new List<PointF>();
            PointF? prev = null;

            // sampling step
            double step = initialXStep;

            for (double x = xMin; x <= xMax + 1e-12; x += step)
            {
                double y = ComputeFunctionSafe(x);
                if (double.IsNaN(y) || double.IsInfinity(y))
                {
                    // break segment
                    if (current.Count > 0)
                    {
                        segments.Add(new List<PointF>(current));
                        current.Clear();
                        prev = null;
                    }
                    continue;
                }

                // Map to screen
                PointF sp = WorldToScreen(x, y);

                // If prev exists and vertical jump (in pixels) is large -> split segment
                if (prev.HasValue)
                {
                    float dy = Math.Abs(sp.Y - prev.Value.Y);
                    if (dy > 150f) // heuristic threshold (pixels) for detecting asymptote
                    {
                        if (current.Count > 0)
                        {
                            segments.Add(new List<PointF>(current));
                            current.Clear();
                        }
                    }
                }

                current.Add(sp);
                prev = sp;
            }

            if (current.Count > 0)
                segments.Add(new List<PointF>(current));

            return segments;
        }

        /// <summary>
        /// Compute y-bounds from sampling the function on [xMin, xMax].
        /// Ignores extreme outliers (very large |y|) and infinities. Adds padding.
        /// </summary>
        private void ComputeYBounds()
        {
            var vals = new List<double>();
            double step = initialXStep;
            const double OUTLIER_LIMIT = 1e5; // ignore |y| > limit as outlier/infinity proxy

            for (double x = xMin; x <= xMax + 1e-12; x += step)
            {
                double y = ComputeFunctionSafe(x);
                if (double.IsNaN(y) || double.IsInfinity(y)) continue;
                if (Math.Abs(y) > OUTLIER_LIMIT) continue;
                vals.Add(y);
            }

            if (vals.Count >= 2)
            {
                double min = double.MaxValue;
                double max = double.MinValue;
                foreach (var v in vals)
                {
                    if (v < min) min = v;
                    if (v > max) max = v;
                }

                // If min == max (flat), expand a bit
                if (Math.Abs(max - min) < 1e-6)
                {
                    min -= 1;
                    max += 1;
                }

                // add padding (10%)
                double padding = (max - min) * 0.1;
                yMin = min - padding;
                yMax = max + padding;

                // safety: if padding too small, ensure non-zero range
                if (Math.Abs(yMax - yMin) < 1e-6)
                {
                    yMin -= 1;
                    yMax += 1;
                }
            }
            else
            {
                // fallback to defaults
                yMin = -10;
                yMax = 10;
            }
        }

        /// <summary>
        /// Safe function evaluation with trivial guards (avoid denominator 0).
        /// Returns NaN if too close to singularity.
        /// </summary>
        private double ComputeFunctionSafe(double x)
        {
            double denom = Math.Pow(x + 3, 2);
            if (Math.Abs(denom) < 1e-12) return double.NaN;

            // compute tan arg, but guard extremely large tangent values
            double arg = x + 7;
            double t = Math.Tan(arg);

            // If tan is extremely large (approaching asymptote), treat as NaN
            if (double.IsInfinity(t) || Math.Abs(t) > 1e6) return double.NaN;

            return 5.0 * t / denom;
        }

        // --- Coordinate transforms ---
        private float WorldToScreenX(double x)
        {
            double availableW = ClientSize.Width - 2 * margin;
            if (availableW <= 0) return margin;
            double scaleX = availableW / (xMax - xMin);
            return margin + (float)((x - xMin) * scaleX);
        }

        private float WorldToScreenY(double y)
        {
            double availableH = ClientSize.Height - 2 * margin;
            if (availableH <= 0) return margin;
            double scaleY = availableH / (yMax - yMin);
            // invert Y because screen Y grows downward
            return margin + (float)((yMax - y) * scaleY);
        }

        private PointF WorldToScreen(double x, double y)
        {
            return new PointF(WorldToScreenX(x), WorldToScreenY(y));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                axisPen?.Dispose();
                gridPen?.Dispose();
                graphPen?.Dispose();
                labelFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
