using System;
using System.Drawing;

namespace T2B.UI
{
    public static class NeonRainbowPalette
    {
        public static Color FromHsv(double h, double s, double v)
        {
            // Normalize hue to [0, 360)
            h = h % 360.0;
            if (h < 0) h += 360.0;

            s = Clamp01(s);
            v = Clamp01(v);

            double c = v * s;
            double hh = h / 60.0;
            double x = c * (1.0 - Math.Abs((hh % 2.0) - 1.0));
            double m = v - c;

            double r1 = 0, g1 = 0, b1 = 0;

            if (0 <= hh && hh < 1) { r1 = c; g1 = x; b1 = 0; }
            else if (1 <= hh && hh < 2) { r1 = x; g1 = c; b1 = 0; }
            else if (2 <= hh && hh < 3) { r1 = 0; g1 = c; b1 = x; }
            else if (3 <= hh && hh < 4) { r1 = 0; g1 = x; b1 = c; }
            else if (4 <= hh && hh < 5) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }

            int r = (int)Math.Round((r1 + m) * 255.0);
            int g = (int)Math.Round((g1 + m) * 255.0);
            int b = (int)Math.Round((b1 + m) * 255.0);

            r = ClampByte(r);
            g = ClampByte(g);
            b = ClampByte(b);

            return Color.FromArgb(r, g, b);
        }

        public static Color Complement(Color c)
        {
            return Color.FromArgb(255 - c.R, 255 - c.G, 255 - c.B);
        }

        private static int ClampByte(int x)
        {
            if (x < 0) return 0;
            if (x > 255) return 255;
            return x;
        }

        private static double Clamp01(double x)
        {
            if (x < 0) return 0;
            if (x > 1) return 1;
            return x;
        }
    }
}
