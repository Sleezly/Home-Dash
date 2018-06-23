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

        public static double[] RGBtoXY(RGB rgb)
        {
            double red = (rgb.R > 0.04045f) ? Math.Pow((rgb.R + 0.055f) / (1.0f + 0.055f), 2.4f) : (rgb.R / 12.92f);
            double green = (rgb.G > 0.04045f) ? Math.Pow((rgb.G + 0.055f) / (1.0f + 0.055f), 2.4f) : (rgb.G / 12.92f);
            double blue = (rgb.B > 0.04045f) ? Math.Pow((rgb.B + 0.055f) / (1.0f + 0.055f), 2.4f) : (rgb.B / 12.92f);

            double X = red * 0.664511f + green * 0.154324f + blue * 0.162028f;
            double Y = red * 0.283881f + green * 0.668433f + blue * 0.047685f;
            double Z = red * 0.000088f + green * 0.072310f + blue * 0.986039f;

            double[] xy = new double[2];

            xy[0] = X / (X + Y + Z);
            xy[1] = Y / (X + Y + Z);

            return xy;
        }

        public static int XYToTemperature(double[] xy)
        {
            double x = xy[0];
            double y = xy[1];
            // Method 1
            //http://stackoverflow.com/questions/13975917/calculate-colour-temperature-in-k
            //=(-449*((R1-0,332)/(S1-0,1858))^3)+(3525*((R1-0,332)/(S1-0,1858))^2)-(6823,3*((R1-0,332)/(S1-0,1858)))+(5520,33)
            //        double temp1 = -449 * Math.pow((x - 0.332) / (y - 0.1858), 3)
            //                + 3525 * Math.pow((x - 0.332) / (y - 0.1858), 2)
            //                - 6823.3 * ((x - 0.332) / (y - 0.1858))
            //                + 5520.33;
            //        float micro1 = (float) (1 / temp1 * 1000000);

            // Method 2
            //http://www.vinland.com/Correlated_Color_Temperature.html
            //         437*((x - 0,332)/(0,1858 - y))^3+
            //                3601*((x - 0,332)/(0,1858 - y))^2+
            //                6831*((x - 0,332)/(0,1858 - y)) +
            //                5517
            double temp2 = (437 * Math.Pow((x - 0.332) / (0.1858 - y), 3) +
                    3601 * Math.Pow((x - 0.332) / (0.1858 - y), 2) +
                    6831 * ((x - 0.332) / (0.1858 - y))) +
                    5517;
            //To set the light to a white value you need to interact with the “ct” (color temperature) resource,
            // which takes values in a scale called “reciprocal megakelvin” or “mirek”.
            // Using this scale, the warmest color 2000K is 500 mirek ("ct":500) and the coldest color 6500K is 153 mirek ("ct":153)
            double micro2 = (double)(1 / temp2 * 1000000);
            return (int)micro2;
        }
    }
}