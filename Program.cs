using System;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicGraph
{
    public class GraphForm : Form
    {
        private const double XStart = 1.2;
        private const double XEnd = 6.3;
        private const double Step = 0.2;

        public GraphForm()
        {
            this.Text = "Dynamic Graph";
            this.Resize += (s, e) => this.Invalidate();
            this.DoubleBuffered = true;
        }

        private double Function(double x)
        {
            return (5 * Math.Tan(x + 7)) / Math.Pow(x + 3, 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(Color.White);

            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;
            if (w < 50 || h < 50) return;

            Pen axisPen = new Pen(Color.Black, 2);
            Pen graphPen = new Pen(Color.Blue, 2);

            int x0 = w / 2;
            int y0 = h / 2;
            g.DrawLine(axisPen, 0, y0, w, y0);
            g.DrawLine(axisPen, x0, 0, x0, h);

            double minY = double.MaxValue;
            double maxY = double.MinValue;
            for (double x = XStart; x <= XEnd; x += Step)
            {
                double y = Function(x);
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            double scaleX = w / (XEnd - XStart);
            double scaleY = (h * 0.8) / (maxY - minY);

            PointF? prev = null;
            for (double x = XStart; x <= XEnd; x += Step)
            {
                double y = Function(x);

                float screenX = (float)((x - XStart) * scaleX);
                float screenY = (float)(h - (y - minY) * scaleY);

                PointF current = new PointF(screenX, screenY);

                if (prev != null)
                {
                    g.DrawLine(graphPen, prev.Value, current);
                }
                prev = current;
            }
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new GraphForm());
        }
    }
}
