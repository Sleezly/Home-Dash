using System;

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
    }
}
