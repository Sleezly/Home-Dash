using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace HashBoard
{
    public class RGB
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public RGB() { }
        public RGB(byte r, byte g, byte b) { R = r; G = g; B = b; }

        public static RGB Average(List<RGB> rgbList)
        {
            int r = 0;
            int g = 0;
            int b = 0;

            foreach (RGB rgb in rgbList)
            {
                r += rgb.R;
                g += rgb.G;
                b += rgb.B;
            }

            return new RGB(
                Convert.ToByte(r / rgbList.Count),
                Convert.ToByte(g / rgbList.Count),
                Convert.ToByte(b / rgbList.Count));
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
