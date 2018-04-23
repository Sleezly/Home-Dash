using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Media;
using System.Linq;

namespace HashBoard
{
    public class RGB
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public RGB() { }
        public RGB(byte r, byte g, byte b) { R = r; G = g; B = b; }

        public static RGB Average(IEnumerable<RGB> rgbs)
        {
            int r = 0;
            int g = 0;
            int b = 0;

            foreach (RGB rgb in rgbs)
            {
                r += rgb.R;
                g += rgb.G;
                b += rgb.B;
            }

            return new RGB(
                Convert.ToByte(r / rgbs.Count()),
                Convert.ToByte(g / rgbs.Count()),
                Convert.ToByte(b / rgbs.Count()));
        }

        public static RGB GetBlendedColor(double percentage, Color left, Color right)
        {
            double r = Math.Sqrt(Math.Pow(left.R, 2) * percentage + Math.Pow(right.R, 2) * (1.0 - percentage));
            double g = Math.Sqrt(Math.Pow(left.G, 2) * percentage + Math.Pow(right.G, 2) * (1.0 - percentage));
            double b = Math.Sqrt(Math.Pow(left.B, 2) * percentage + Math.Pow(right.B, 2) * (1.0 - percentage));

            return new RGB(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
        }

        public SolidColorBrush CreateSolidColorBrush()
        {
            return new SolidColorBrush(Color.FromArgb(255, R, G, B));
        }

        public bool Equals(Color color, byte tolerance = 0)
        {
            if (Math.Max(color.R, R) - Math.Min(color.R, R) <= tolerance &&
                Math.Max(color.G, G) - Math.Min(color.G, G) <= tolerance &&
                Math.Max(color.B, B) - Math.Min(color.B, B) <= tolerance)
            {
                return true;
            }

            return false;
        }
    }
}
