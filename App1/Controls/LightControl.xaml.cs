using HashBoard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using System.Linq;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.Storage;
using System.IO;
using Windows.ApplicationModel;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Hashboard
{
    public partial class LightControl : UserControl
    {
        private Entity PanelEntity;

        public LightControl(Entity entity)
        {
            this.InitializeComponent();

            PanelEntity = entity;

            LayoutRoot_Loaded();

            //Task.Factory.StartNew(async () =>
            //{
            //    ////await LoadColorWheelBitmapDecoder();
            //    //IRandomAccessStream stream = await randomAccessStreamReference.OpenReadAsync();

            //    //ColorWheelBitmapDecoder = await BitmapDecoder.CreateAsync(stream);

            //    //PixelDataProvider pixelDataProvider = await ColorWheelBitmapDecoder.GetPixelDataAsync();

            //    //ColorWheelPixelData = pixelDataProvider.DetachPixelData();
            //});

            UpdateUI();

            ThreadPoolTimer timer = ThreadPoolTimer.CreatePeriodicTimer(async (t) =>
            {
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}");

                PanelEntity = await WebRequests.GetData<Entity>(MainPage.hostname, $"api/states/{PanelEntity.EntityId}", MainPage.apiPassword);

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI();

                    if (this.Visibility == Visibility.Collapsed)
                    {
                        t.Cancel();
                    }
                });

            }, TimeSpan.FromSeconds(1));
        }

        private void LayoutRoot_Loaded()
        {
            Grid canvas = this.FindName("Wheel") as Grid;

            for (int i = 0; i < 360; i++)
            {
                RotateTransform rt = new RotateTransform() { Angle = i };
                LinearGradientBrush lgb = new LinearGradientBrush();
                lgb.GradientStops.Add(new GradientStop() { Color = Colors.White });
                lgb.GradientStops.Add(new GradientStop() { Color = ConvertHSV2RGB(i, 1, 1), Offset = 1 });
                Line line = new Line() { X1 = 0, Y1 = 0, X2 = canvas.Width / 2, Y2 = 0, StrokeThickness = 4, RenderTransform = rt, Stroke = lgb };
                line.HorizontalAlignment = HorizontalAlignment.Right;
                line.VerticalAlignment = VerticalAlignment.Center;

                line.PointerMoved += MediaImage_PointerMoved;

                canvas.Children.Add(line);
            }
        }

        private Color ConvertHSV2RGB(float h, float s, float v)
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

        private void UpdateUI()
        {
            if (!string.Equals(PanelEntity.State, "on", StringComparison.InvariantCultureIgnoreCase))
            {
                BitmapIcon bitmapIcon = this.FindName("ButtonPower") as BitmapIcon;
                bitmapIcon.Foreground = new SolidColorBrush(Colors.Gray);
            }

            if (PanelEntity.Attributes.ContainsKey("friendly_name"))
            {
                TextBlock textBlock = FindName("DeviceText") as TextBlock;
                textBlock.Text = PanelEntity.Attributes["friendly_name"];
            }

            if (PanelEntity.Attributes.ContainsKey("brightness"))
            {
                UpdateBrightness(Convert.ToByte(PanelEntity.Attributes["brightness"]));
            }

            if (PanelEntity.Attributes.ContainsKey("color_temp"))
            {
                UpdateColorTemperature(
                    Convert.ToInt32(PanelEntity.Attributes["color_temp"]),
                    Convert.ToInt32(PanelEntity.Attributes["max_mireds"]),
                    Convert.ToInt32(PanelEntity.Attributes["min_mireds"]));
            }

            if (PanelEntity.Attributes.ContainsKey("rgb_color"))
            {
                Newtonsoft.Json.Linq.JArray rgbColors = PanelEntity.Attributes["rgb_color"];

                RGB rgb = new RGB()
                {
                    R = Convert.ToByte(rgbColors[0]),
                    G = Convert.ToByte(rgbColors[1]),
                    B = Convert.ToByte(rgbColors[2]),
                };

                UpdateColorCircle(rgb);
            }
         }

        private SolidColorBrush GetColorFromPointOnBlendedColorLine(double percentage, Color left, Color right)
        {
            double r = Math.Sqrt(Math.Pow(left.R, 2) * percentage + Math.Pow(right.R, 2) * (1.0 - percentage));
            double g = Math.Sqrt(Math.Pow(left.G, 2) * percentage + Math.Pow(right.G, 2) * (1.0 - percentage));
            double b = Math.Sqrt(Math.Pow(left.B, 2) * percentage + Math.Pow(right.B, 2) * (1.0 - percentage));

            SolidColorBrush newFill = new SolidColorBrush(
                Color.FromArgb(
                    255,
                    Convert.ToByte(r),
                    Convert.ToByte(g),
                    Convert.ToByte(b)));

            return newFill;
        }

        private void UpdateBrightness(byte brightness)
        {
            Ellipse ellipse = this.FindName("BrightnessCircle") as Ellipse;
            Rectangle rectangle = this.FindName("BrightnessRectangle") as Rectangle;

            double percentage = 1.0 - (double)brightness / 255.0;

            ellipse.Fill = GetColorFromPointOnBlendedColorLine(percentage, Colors.DarkSlateGray, Colors.LightGray);
            ellipse.Margin = new Thickness((1.0 - percentage) * rectangle.Width - ellipse.Width / 2, 0, 0, 0);
        }

        private void UpdateColorTemperature(int colorTemperature, int maxColorTemperature, int minColorTemperature)
        {
            Ellipse ellipse = this.FindName("ColorTemperatureCircle") as Ellipse;
            Rectangle rectangle = this.FindName("ColorTemperature") as Rectangle;

            colorTemperature = Math.Min(colorTemperature, maxColorTemperature);
            colorTemperature = Math.Max(colorTemperature, minColorTemperature);

            double percentage = (double)(colorTemperature - minColorTemperature) / (double)(maxColorTemperature - minColorTemperature);

            ellipse.Fill = GetColorFromPointOnBlendedColorLine(percentage, Colors.Gold, Colors.LightCyan);
            ellipse.Margin = new Thickness((1.0 - percentage) * rectangle.Width - ellipse.Width / 2, 0, 0, 0);
        }

        private void MediaImage_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Grid grid = this.FindName("Wheel") as Grid;
            Ellipse ellipse = this.FindName("WheelCircle") as Ellipse;

            //ellipse.Margin = new Thickness(
            //    e.GetCurrentPoint(grid as UIElement).Position.X - ellipse.Width / 2,
            //    e.GetCurrentPoint(grid as UIElement).Position.Y - ellipse.Height / 2,
            //    0, 0);

            Line line = sender as Line;

            LinearGradientBrush linearGradientBrush = line.Stroke as LinearGradientBrush;

            Color colorStart = linearGradientBrush.GradientStops[0].Color;
            Color colorEnd = linearGradientBrush.GradientStops[1].Color;

            double x = e.GetCurrentPoint(line).Position.X;
            double y = e.GetCurrentPoint(line).Position.Y;

            double percentage = 1.0 - x / line.ActualWidth;

            SolidColorBrush brush = GetColorFromPointOnBlendedColorLine(percentage, colorStart, colorEnd);

            ellipse.Fill = brush;



            HSV hsv = RGBToHSV(new RGB(brush.Color.R, brush.Color.G, brush.Color.B));

            Debug.WriteLine($"{hsv.H}, {hsv.S}, {hsv.V}");
        }

        public class HSV { public double H; public double S; public double V; public HSV(double h, double s, double v) { H = h; S = s; V = v; } }
        public HSV RGBToHSV(RGB rgb)
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


        private void UpdateColorCircle(RGB rgb)
        {
            Grid grid = this.FindName("Wheel") as Grid;

            HSV hsv = RGBToHSV(rgb);

            Line line = grid.Children[(int)hsv.H] as Line;

            Ellipse ellipse = this.FindName("WheelCircle") as Ellipse;

            double angle = (Math.PI / 180) * hsv.H;
            double radian = grid.Width * hsv.S;
            double x = radian * Math.Cos(angle);
            double y = radian * Math.Sin(angle);

            ellipse.Margin = new Thickness(x, y, 0, 0);
        }

        //private Color GetPixel(byte[] pixels, int x, int y, uint width, uint height)
        //{
        //    int i = x;
        //    int j = y;
        //    int k = (i * (int)width + j) * 3;
        //    var r = pixels[k + 0];
        //    var g = pixels[k + 1];
        //    var b = pixels[k + 2];
        //    return Color.FromArgb(255, r, g, b);
        //}

        //private System.Drawing.Point GetColorWheelCirclePoint(RGB rgb)
        //{
        //    if (ColorWheelPixelData != null)
        //    {
        //        for (int x = 0; x < ColorWheelBitmapDecoder.PixelWidth; x++)
        //        {
        //            for (int y = 0; y < ColorWheelBitmapDecoder.PixelHeight; y++)
        //            {
        //                Color color = GetPixel(ColorWheelPixelData, x, y, ColorWheelBitmapDecoder.PixelWidth, ColorWheelBitmapDecoder.PixelHeight);
        //                if (rgb.Equals(color, 5))
        //                {
        //                    //Ellipse ellipse = this.FindName("WheelCircle") as Ellipse;
        //                    //ellipse.Fill = new SolidColorBrush(Color.FromArgb(255, color.R, color.G, color.B));
        //                    Debug.WriteLine($"{x},{y}");

        //                    return new System.Drawing.Point(x, y);
        //                }
        //            }
        //        }
        //    }

        //    return new System.Drawing.Point();
        //}

        private void BitmapIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;

            bitmapIcon.Foreground = new SolidColorBrush(Colors.White);
            WebRequests.SendAction(PanelEntity.EntityId, "toggle");
        }

        private void ButtonPrevious_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = new SolidColorBrush(Colors.DarkGray);
        }
        private void ButtonPrevious_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = new SolidColorBrush(Colors.White);
        }

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            SetScobblerPercentage(sender, e);
        }

        private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                SetScobblerPercentage(sender, e);
            }
        }

        private void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            SetScobblerPercentage(sender, e);
        }

        private void SetScobblerPercentage(object sender, PointerRoutedEventArgs e)
        {
        }

    }
}
