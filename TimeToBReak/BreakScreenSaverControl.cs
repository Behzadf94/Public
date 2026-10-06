using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using T2B.Services;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace T2B.UI
{
    public sealed class BreakScreenSaverControl : Control
    {
        private readonly WinFormsTimer _renderTimer;

        private readonly Random _random = new Random();
        private PointF _pos;
        private PointF _vel;
        private PointF _targetPosition;
        private float _targetChangeTimer;

        private double _t;
        private DateTime _lastFrame;

        private Func<TimeSpan> _getRemaining;
        private Func<string> _getUserDisplayName;

        private const float PaddingOuter = 30f;
        private const float BlockWidth = 520f;
        private const float BlockHeight = 560f;
        private const float MaxSpeed = 220f;
        private const float MinSpeed = 60f;
        private const float MaxAcceleration = 320f;
        private const float BoundaryMargin = 120f;

        public BreakScreenSaverControl(Func<TimeSpan> getRemaining, Func<string> getUserDisplayName)
        {
            _getRemaining = getRemaining ?? throw new ArgumentNullException(nameof(getRemaining));
            _getUserDisplayName = getUserDisplayName ?? throw new ArgumentNullException(nameof(getUserDisplayName));

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.Black;

            _pos = new PointF(100, 100);
            _vel = new PointF(140f, 110f);
            _targetPosition = _pos;
            _targetChangeTimer = 0f;

            _lastFrame = DateTime.UtcNow;

            _renderTimer = new WinFormsTimer { Interval = 16 };
            _renderTimer.Tick += (s, e) => StepAndInvalidate();
            _renderTimer.Start();
        }

        public void UpdateProviders(Func<TimeSpan> getRemaining, Func<string> getUserDisplayName)
        {
            if (getRemaining != null) _getRemaining = getRemaining;
            if (getUserDisplayName != null) _getUserDisplayName = getUserDisplayName;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _renderTimer.Stop();
                _renderTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        private void StepAndInvalidate()
        {
            DateTime now = DateTime.UtcNow;
            double dt = (now - _lastFrame).TotalSeconds;
            if (dt <= 0) dt = 0.016;
            if (dt > 0.1) dt = 0.1;
            _lastFrame = now;

            _t += dt;

            RectangleF bounds = GetMovementBounds();

            UpdateTarget(bounds, (float)dt);
            ApplyPhysics(bounds, (float)dt);

            _pos = new PointF(
                Math.Min(bounds.Right, Math.Max(bounds.Left, _pos.X)),
                Math.Min(bounds.Bottom, Math.Max(bounds.Top, _pos.Y)));

            Invalidate();
        }

        private RectangleF GetMovementBounds()
        {
            float w = Math.Min(BlockWidth, ClientSize.Width - 2 * PaddingOuter);
            float h = Math.Min(BlockHeight, ClientSize.Height - 2 * PaddingOuter);

            w = Math.Max(200, w);
            h = Math.Max(200, h);

            float left = PaddingOuter;
            float top = PaddingOuter;
            float right = Math.Max(PaddingOuter, ClientSize.Width - PaddingOuter - w);
            float bottom = Math.Max(PaddingOuter, ClientSize.Height - PaddingOuter - h);

            return RectangleF.FromLTRB(left, top, right, bottom);
        }

        private void UpdateTarget(RectangleF bounds, float dt)
        {
            _targetChangeTimer -= dt;
            if (_targetChangeTimer <= 0f || Distance(_pos, _targetPosition) < 20f)
            {
                _targetPosition = new PointF(
                    bounds.Left + (float)_random.NextDouble() * (bounds.Width),
                    bounds.Top + (float)_random.NextDouble() * (bounds.Height));

                _targetChangeTimer = 1.0f + (float)_random.NextDouble() * 2.0f;
            }
        }

        private void ApplyPhysics(RectangleF bounds, float dt)
        {
            PointF desired = new PointF(_targetPosition.X - _pos.X, _targetPosition.Y - _pos.Y);
            float dist = Distance(desired);
            if (dist > 0.01f)
            {
                desired = Scale(desired, 1.0f / dist * MaxSpeed);
            }

            PointF steer = new PointF(desired.X - _vel.X, desired.Y - _vel.Y);
            steer = Truncate(steer, MaxAcceleration * dt);

            // boundary repulsion
            PointF repulsion = GetBoundaryRepulsion(bounds);
            repulsion = Truncate(repulsion, MaxAcceleration * 1.4f * dt);

            _vel = new PointF(_vel.X + steer.X + repulsion.X, _vel.Y + steer.Y + repulsion.Y);
            _vel = Truncate(_vel, MaxSpeed);

            if (Distance(_vel) < MinSpeed)
            {
                _vel = Scale(Normalize(_vel), MinSpeed);
            }

            _pos = new PointF(_pos.X + _vel.X * dt, _pos.Y + _vel.Y * dt);
        }

        private PointF GetBoundaryRepulsion(RectangleF bounds)
        {
            PointF force = new PointF(0, 0);

            if (_pos.X < bounds.Left + BoundaryMargin)
            {
                float strength = 1f - (_pos.X - bounds.Left) / BoundaryMargin;
                force.X += MaxAcceleration * strength;
            }
            else if (_pos.X > bounds.Right - BoundaryMargin)
            {
                float strength = 1f - (bounds.Right - _pos.X) / BoundaryMargin;
                force.X -= MaxAcceleration * strength;
            }

            if (_pos.Y < bounds.Top + BoundaryMargin)
            {
                float strength = 1f - (_pos.Y - bounds.Top) / BoundaryMargin;
                force.Y += MaxAcceleration * strength;
            }
            else if (_pos.Y > bounds.Bottom - BoundaryMargin)
            {
                float strength = 1f - (bounds.Bottom - _pos.Y) / BoundaryMargin;
                force.Y -= MaxAcceleration * strength;
            }

            return force;
        }

        private float Distance(PointF a, PointF b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private float Distance(PointF a)
        {
            return (float)Math.Sqrt(a.X * a.X + a.Y * a.Y);
        }

        private PointF Scale(PointF vector, float scale)
        {
            return new PointF(vector.X * scale, vector.Y * scale);
        }

        private PointF Normalize(PointF vector)
        {
            float len = Distance(vector);
            if (len < 1e-5f) return new PointF(1, 0);
            return new PointF(vector.X / len, vector.Y / len);
        }

        private PointF Truncate(PointF vector, float max)
        {
            float len = Distance(vector);
            if (len <= max) return vector;
            return Scale(vector, max / len);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            DrawAnimatedBackground(g);

            double hue = (_t * 45.0) % 360.0;
            Color neon = NeonRainbowPalette.FromHsv(hue, 1.0, 1.0);
            Color neon2 = NeonRainbowPalette.FromHsv((hue + 60) % 360, 1.0, 1.0);
            Color bgBase = NeonRainbowPalette.FromHsv((hue + 180) % 360, 0.55, 0.35);
            Color stroke = NeonRainbowPalette.Complement(bgBase);

            float blockW = Math.Min(BlockWidth, ClientSize.Width - 2 * PaddingOuter);
            float blockH = Math.Min(BlockHeight, ClientSize.Height - 2 * PaddingOuter);
            blockW = Math.Max(260, blockW);
            blockH = Math.Max(260, blockH);

            RectangleF blockRect = new RectangleF(_pos.X, _pos.Y, blockW, blockH);

            float clockSize = Math.Min(blockW, blockH * 0.62f);
            RectangleF clockRect = new RectangleF(
                blockRect.X + (blockRect.Width - clockSize) / 2f,
                blockRect.Y,
                clockSize,
                clockSize);

            DrawNeonAnalogClock(g, clockRect, neon, neon2, stroke);

            RectangleF textRect = new RectangleF(
                blockRect.X,
                clockRect.Bottom + 10,
                blockRect.Width,
                blockRect.Bottom - (clockRect.Bottom + 10));

            DrawNeonInfoText(g, textRect);
        }

        private void DrawAnimatedBackground(Graphics g)
        {
            double hue = (_t * 25.0) % 360.0;

            Color c1 = NeonRainbowPalette.FromHsv(hue, 0.65, 0.20);
            Color c2 = NeonRainbowPalette.FromHsv((hue + 120) % 360, 0.65, 0.20);
            Color c3 = NeonRainbowPalette.FromHsv((hue + 240) % 360, 0.65, 0.20);

            using (var brush = new LinearGradientBrush(
                new Point(0, 0),
                new Point(ClientSize.Width, ClientSize.Height),
                c1, c2))
            {
                var blend = new ColorBlend();
                blend.Colors = new[] { c1, c2, c3, c1 };
                blend.Positions = new[] { 0f, 0.33f, 0.66f, 1f };
                brush.InterpolationColors = blend;

                g.FillRectangle(brush, ClientRectangle);
            }

            using (var vignette = new GraphicsPath())
            {
                vignette.AddEllipse(-ClientSize.Width * 0.1f, -ClientSize.Height * 0.1f,
                    ClientSize.Width * 1.2f, ClientSize.Height * 1.2f);

                using (var pgb = new PathGradientBrush(vignette))
                {
                    pgb.CenterColor = Color.FromArgb(0, 0, 0, 0);
                    pgb.SurroundColors = new[] { Color.FromArgb(180, 0, 0, 0) };
                    g.FillRectangle(pgb, ClientRectangle);
                }
            }
        }

        private void DrawNeonAnalogClock(Graphics g, RectangleF rect, Color neon, Color neon2, Color stroke)
        {
            PointF center = new PointF(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            float radius = rect.Width / 2f;

            using (var penGlow = new Pen(Color.FromArgb(80, neon.R, neon.G, neon.B), 18f))
            {
                penGlow.LineJoin = LineJoin.Round;
                g.DrawEllipse(penGlow, rect.X + 8, rect.Y + 8, rect.Width - 16, rect.Height - 16);
            }

            using (var penRing = new Pen(Color.FromArgb(220, stroke.R, stroke.G, stroke.B), 3f))
            {
                g.DrawEllipse(penRing, rect.X + 10, rect.Y + 10, rect.Width - 20, rect.Height - 20);
            }

            using (var font = new Font("Tahoma", radius * 0.14f, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                for (int h = 1; h <= 12; h++)
                {
                    double ang = (Math.PI / 6.0) * (h - 3);
                    float rNum = radius * 0.78f;

                    float x = center.X + (float)(Math.Cos(ang) * rNum);
                    float y = center.Y + (float)(Math.Sin(ang) * rNum);

                    string s = h.ToString();
                    SizeF size = g.MeasureString(s, font);

                    DrawNeonString(g, s,
                        new PointF(x - size.Width / 2f, y - size.Height / 2f),
                        font, neon, neon2);
                }
            }

            DateTime now = DateTime.Now;

            double sec = now.Second + now.Millisecond / 1000.0;
            double min = now.Minute + sec / 60.0;
            double hour = (now.Hour % 12) + min / 60.0;

            double secAng = (Math.PI * 2.0) * (sec / 60.0) - Math.PI / 2.0;
            double minAng = (Math.PI * 2.0) * (min / 60.0) - Math.PI / 2.0;
            double hrAng = (Math.PI * 2.0) * (hour / 12.0) - Math.PI / 2.0;

            DrawHand(g, center, hrAng, radius * 0.50f, 10f, Color.FromArgb(220, neon2.R, neon2.G, neon2.B), 60);
            DrawHand(g, center, minAng, radius * 0.68f, 7f, Color.FromArgb(230, neon.R, neon.G, neon.B), 55);
            DrawHand(g, center, secAng, radius * 0.78f, 3f, Color.FromArgb(240, 255, 80, 80), 45);

            using (var dotBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
            {
                g.FillEllipse(dotBrush, center.X - 6, center.Y - 6, 12, 12);
            }
        }

        private void DrawHand(Graphics g, PointF center, double angle, float length, float thickness, Color color, int glowAlpha)
        {
            PointF end = new PointF(
                center.X + (float)(Math.Cos(angle) * length),
                center.Y + (float)(Math.Sin(angle) * length));

            using (var glow = new Pen(Color.FromArgb(glowAlpha, color.R, color.G, color.B), thickness + 10f))
            {
                glow.StartCap = LineCap.Round;
                glow.EndCap = LineCap.Round;
                glow.LineJoin = LineJoin.Round;
                g.DrawLine(glow, center, end);
            }

            using (var pen = new Pen(color, thickness))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                g.DrawLine(pen, center, end);
            }
        }

        private void DrawNeonInfoText(Graphics g, RectangleF rect)
        {
            TimeSpan remaining = _getRemaining();
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            string remainingText = string.Format("زمان باقی‌مانده استراحت: {0:mm\\:ss}", remaining);
            string timeText = string.Format("ساعت: {0:HH:mm:ss}", DateTime.Now);
            string dateText = "تاریخ (شمسی): " + PersianDateService.NowPersianDateString();
            string userText = "کاربر: " + _getUserDisplayName();

            float fontSize = Math.Max(16f, rect.Height * 0.12f);
            using (var font = new Font("Tahoma", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                string[] lines = { remainingText, timeText, dateText, userText };

                float totalH = 0f;
                SizeF[] sizes = new SizeF[lines.Length];
                for (int i = 0; i < lines.Length; i++)
                {
                    sizes[i] = g.MeasureString(lines[i], font);
                    totalH += sizes[i].Height;
                }
                totalH += (lines.Length - 1) * 8f;

                float y = rect.Y + (rect.Height - totalH) / 2f;

                for (int i = 0; i < lines.Length; i++)
                {
                    float x = rect.X + (rect.Width - sizes[i].Width) / 2f;

                    Color cA = NeonRainbowPalette.FromHsv((_t * 70.0 + i * 40) % 360.0, 1.0, 1.0);
                    Color cB = NeonRainbowPalette.FromHsv((_t * 70.0 + i * 40 + 70) % 360.0, 1.0, 1.0);

                    DrawNeonString(g, lines[i], new PointF(x, y), font, cA, cB);
                    y += sizes[i].Height + 8f;
                }
            }
        }

        private void DrawNeonString(Graphics g, string text, PointF pos, Font font, Color c1, Color c2)
        {
            using (var path = new GraphicsPath())
            {
                path.AddString(text, font.FontFamily, (int)font.Style, font.Size, pos, StringFormat.GenericDefault);

                using (var glowPen = new Pen(Color.FromArgb(60, c1.R, c1.G, c1.B), 10f))
                {
                    glowPen.LineJoin = LineJoin.Round;
                    g.DrawPath(glowPen, path);
                }

                using (var glowPen2 = new Pen(Color.FromArgb(50, c2.R, c2.G, c2.B), 6f))
                {
                    glowPen2.LineJoin = LineJoin.Round;
                    g.DrawPath(glowPen2, path);
                }

                RectangleF bounds = path.GetBounds();
                using (var brush = new LinearGradientBrush(
                    new PointF(bounds.Left, bounds.Top),
                    new PointF(bounds.Right, bounds.Bottom),
                    c1, c2))
                {
                    g.FillPath(brush, path);
                }

                using (var outline = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f))
                {
                    outline.LineJoin = LineJoin.Round;
                    g.DrawPath(outline, path);
                }
            }
        }
    }
}
