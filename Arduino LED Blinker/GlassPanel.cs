using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace UnoLedControl
{
    public class GlassPanel : Panel
    {
        public Color FillColor { get; set; } = Color.FromArgb(255, 10, 10, 14); // opaque
        public int CornerRadius { get; set; } = 14;

        public GlassPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw, true);

            DoubleBuffered = true;

            BackColor = Color.Transparent; // children can still be transparent-ish
            UpdateStyles();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Do nothing: prevents background erase flicker.
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRoundedRegion();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Paint the rounded background
            using (var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius))
            using (var brush = new SolidBrush(FillColor))
            using (var pen = new Pen(Color.FromArgb(60, 255, 255, 255)))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            // Now let children paint on top
            base.OnPaint(e);
        }

        private void UpdateRoundedRegion()
        {
            // Make the control actually rounded (no rectangular “ghost” edges)
            using (var path = RoundedRect(new Rectangle(0, 0, Width, Height), CornerRadius))
            {
                Region?.Dispose();
                Region = new Region(path);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int r = Math.Max(1, radius);
            int d = r * 2;

            // Clamp if the panel is tiny
            d = Math.Min(d, Math.Min(bounds.Width, bounds.Height));

            var path = new GraphicsPath();

            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
