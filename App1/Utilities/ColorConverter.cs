using System;
using Windows.UI;

namespace HashBoard
{
    public class ColorConverter
    {
        public static double ConvertColorTemperatureToKelvin(int colorTemperature)
        {
            return 1000000.0 / colorTemperature;
        }

        public static RGB MiredToRGB(int colorTemperature)
        {
            RGB rgb = new RGB();

            double kelvin = ConvertColorTemperatureToKelvin(colorTemperature) / 100;

            // Calculate Red
            if (kelvin <= 66)
            {
                rgb.R = 255;
            }
            else
            {
                double r = kelvin - 60;
                r = 329.698727446 * Math.Pow(r, -0.1332047592);

                if (r < 0)
                    rgb.R = 0;
                else if (r > 255)
                    rgb.R = 255;
                else
                    rgb.R = (byte)r;
            }

            // Calculate Green
            double g;
            if (kelvin <= 66)
            {
                g = kelvin;
                g = 99.4708025861 * Math.Log(g) - 161.1195681661;
            }
            else
            {
                g = kelvin - 60;
                g = 288.1221695283 * Math.Pow(g, -0.0755148492);
            }

            if (g < 0)
                rgb.G = 0;
            else if (g > 255)
                rgb.G = 255;
            else
                rgb.G = (byte)g;

            // Calculate Blue
            if (kelvin >= 66)
            {
                rgb.B = 255;
            }
            else
            {
                if (kelvin <= 19)
                {
                    rgb.B = 0;
                }
                else
                {
                    double b = kelvin - 10;
                    b = 138.5177312231 * Math.Log(b) - 305.0447927307;

                    if (b < 0)
                        rgb.B = 0;
                    else if (b > 255)
                        rgb.B = 255;
                    else
                        rgb.B = (byte)b;
                }
            }

            return rgb;
        }

        public static Color HSVtoRGB(float h, float s, float v)
        {
            byte r = 0;
            byte g = 0;
            byte b = 0;
            h /= 60;
            int i = (int)Math.Floor(h);
            float f = h - i;
            float p = v * (1 - s);
            float q = v * (1 - s * f);
            float t = v * (1 - s * (1 - f));
            switch (i)
            {
                case 0:
                    r = (byte)(255 * v);
                    g = (byte)(255 * t);
                    b = (byte)(255 * p);
                    break;
                case 1:
                    r = (byte)(255 * q);
                    g = (byte)(255 * v);
                    b = (byte)(255 * p);
                    break;
                case 2:
                    r = (byte)(255 * p);
                    g = (byte)(255 * v);
                    b = (byte)(255 * t);
                    break;
                case 3:
                    r = (byte)(255 * p);
                    g = (byte)(255 * q);
                    b = (byte)(255 * v);
                    break;
                case 4:
                    r = (byte)(255 * t);
                    g = (byte)(255 * p);
                    b = (byte)(255 * v);
                    break;
                default:
                    r = (byte)(255 * v);
                    g = (byte)(255 * p);
                    b = (byte)(255 * q);
                    break;
            }
            return Color.FromArgb(255, r, g, b);
        }

        public static HSV RGBtoHSV(RGB rgb)
        {
            double delta, min;
            double h = 0, s, v;

            min = Math.Min(Math.Min(rgb.R, rgb.G), rgb.B);
            v = Math.Max(Math.Max(rgb.R, rgb.G), rgb.B);
            delta = v - min;

            if (v == 0.0)
                s = 0;
            else
                s = delta / v;

            if (s == 0)
                h = 0.0;

            else
            {
                if (rgb.R == v)
                    h = (rgb.G - rgb.B) / delta;
                else if (rgb.G == v)
                    h = 2 + (rgb.B - rgb.R) / delta;
                else if (rgb.B == v)
                    h = 4 + (rgb.R - rgb.G) / delta;

                h *= 60;

                if (h < 0.0)
                    h = h + 360;
            }

            return new HSV(h, s, v / 255);
        }
    }
}