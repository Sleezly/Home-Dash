using HashBoard;
using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Hashboard
{
    public partial class LightControl : UserControl
    {
        private Entity PanelEntity;

        public LightControl(Entity entity)
        {
            this.InitializeComponent();

            PanelEntity = entity;

            DrawColorWheel();

            //Task.Factory.StartNew(async () =>
            //{
            //    ////await LoadColorWheelBitmapDecoder();
            //    //IRandomAccessStream stream = await randomAccessStreamReference.OpenReadAsync();

            //    //ColorWheelBitmapDecoder = await BitmapDecoder.CreateAsync(stream);

            //    //PixelDataProvider pixelDataProvider = await ColorWheelBitmapDecoder.GetPixelDataAsync();

            //    //ColorWheelPixelData = pixelDataProvider.DetachPixelData();
            //});

            UpdateUI();

            //ThreadPoolTimer timer = ThreadPoolTimer.CreatePeriodicTimer(async (t) =>
            //{
            //    Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}");

            //    PanelEntity = await WebRequests.GetData<Entity>(MainPage.hostname, $"api/states/{PanelEntity.EntityId}", MainPage.apiPassword);

            //    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            //    {
            //        UpdateUI();

            //        if (this.Visibility == Visibility.Collapsed)
            //        {
            //            t.Cancel();
            //        }
            //    });

            //}, TimeSpan.FromSeconds(1));
        }

        private void DrawColorWheel()
        {
            Grid canvas = this.FindName("Wheel") as Grid;

            for (int i = 0; i < 360; i++)
            {
                RotateTransform rt = new RotateTransform() { Angle = i };

                LinearGradientBrush lgb = new LinearGradientBrush();
                lgb.GradientStops.Add(new GradientStop() { Color = Colors.White });
                lgb.GradientStops.Add(new GradientStop() { Color = ColorConverter.HSVtoRGB(i, 1, 1), Offset = 1 });

                Line line = new Line() { X1 = 0, Y1 = 0, X2 = canvas.Width / 2, Y2 = 0, StrokeThickness = 6, RenderTransform = rt, Stroke = lgb };
                line.Margin = new Thickness(canvas.Width / 2, canvas.Width / 2, 0, 0);
                line.HorizontalAlignment = HorizontalAlignment.Left;
                line.VerticalAlignment = VerticalAlignment.Top;

                line.Tapped += ColorWheelLine_Tapped;
                line.PointerPressed += ColorWheelLine_PointerPressed;
                line.PointerMoved += ColorWheelLine_PointerMoved;
                line.PointerReleased += ColorWheelLine_PointerReleased;

                canvas.Children.Add(line);
            }
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
            else
            {
                Ellipse ellipse = this.FindName("BrightnessCircle") as Ellipse;
                ellipse.Visibility = Visibility.Collapsed;
            }

            if (PanelEntity.Attributes.ContainsKey("color_temp"))
            {
                UpdateColorTemperature(
                    Convert.ToInt32(PanelEntity.Attributes["color_temp"]),
                    Convert.ToInt32(PanelEntity.Attributes["max_mireds"]),
                    Convert.ToInt32(PanelEntity.Attributes["min_mireds"]));
            }
            else
            {
                Ellipse ellipse = this.FindName("ColorTemperatureCircle") as Ellipse;
                ellipse.Visibility = Visibility.Collapsed;
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

                SetColorCircleLocationAndColor(rgb);
            }
            else
            {
                Ellipse ellipse = this.FindName("WheelCircle") as Ellipse;
                ellipse.Visibility = Visibility.Collapsed;
            }
        }

        private void SetColorCircleLocationAndColor(RGB rgb)
        {
            Grid grid = this.FindName("Wheel") as Grid;

            HSV hsv = ColorConverter.RGBtoHSV(rgb);

            Line line = grid.Children[(int)hsv.H] as Line;

            Ellipse ellipse = this.FindName("WheelCircle") as Ellipse;

            double angle = (Math.PI / 180) * hsv.H;
            double radian = (grid.Width / 2) * hsv.S;
            double x = radian * Math.Cos(angle);
            double y = radian * Math.Sin(angle);

            ellipse.Margin = new Thickness(
                x + grid.Width / 2,
                y + grid.Height / 2,
                0,
                0);

            ellipse.Fill = rgb.CreateSolidColorBrush();
        }

        private void MoveColorCircleToLineAndSetColor(Line line, Point pointFromLine, Point pointFromParent, Thickness marginFromParent)
        {
            /*
            Grid grid = this.FindName("Wheel") as Grid;
            Ellipse ellipse = this.FindName("WheelCircle") as Ellipse;

            //ellipse.Visibility = Visibility.Visible;
            //ellipse.Margin = new Thickness(
            //    e.GetCurrentPoint(grid as UIElement).Position.X,// - grid.ActualWidth / 2,
            //    e.GetCurrentPoint(grid as UIElement).Position.Y - grid.ActualHeight / 2,
            //    0, 0);

            Line line = sender as Line;

            LinearGradientBrush linearGradientBrush = line.Stroke as LinearGradientBrush;

            Color colorStart = linearGradientBrush.GradientStops[0].Color;
            Color colorEnd = linearGradientBrush.GradientStops[1].Color;

            double x = e.GetCurrentPoint(line).Position.X;
            //double y = e.GetCurrentPoint(line).Position.Y;

            double percentage = 1.0 - x / line.ActualWidth;

            RGB rgb = RGB.GetBlendedColor(percentage, colorStart, colorEnd);

            ellipse.Fill = rgb.CreateSolidColorBrush();

            //MoveColorCircle(rgb);
            Debug.WriteLine($"RGB: {rgb.R}, {rgb.G}, {rgb.B}");
            //HSV hsv = RGBToHSV(new RGB(brush.Color.R, brush.Color.G, brush.Color.B));
            //Debug.WriteLine($"{hsv.H}, {hsv.S}, {hsv.V}");
            */

            //Grid grid = this.FindName("Wheel") as Grid;
            Ellipse ellipse = this.FindName("WheelCircle") as Ellipse;

            //Line line = sender as Line;

            LinearGradientBrush linearGradientBrush = line.Stroke as LinearGradientBrush;

            Color colorStart = linearGradientBrush.GradientStops[0].Color;
            Color colorEnd = linearGradientBrush.GradientStops[1].Color;
            double x = pointFromLine.X;
            double percentage = 1.0 - x / line.ActualWidth;

            RGB rgb = RGB.GetBlendedColor(percentage, colorStart, colorEnd);

            ellipse.Fill = rgb.CreateSolidColorBrush();

            ellipse.Margin = new Thickness(
                pointFromParent.X - marginFromParent.Left / 2,
                pointFromParent.Y - marginFromParent.Top / 2,
                0, 0);

            StackPanel root = this.FindName("RootPanel") as StackPanel;
            root.Background = rgb.CreateSolidColorBrush();
        }

        private void UpdateBrightness(byte brightness)
        {
            Ellipse ellipse = this.FindName("BrightnessCircle") as Ellipse;
            Rectangle rectangle = this.FindName("BrightnessRectangle") as Rectangle;

            double percentage = 1.0 - (double)brightness / 255.0;
            
            ellipse.Fill = RGB.GetBlendedColor(percentage, Colors.DarkSlateGray, Colors.LightGray).CreateSolidColorBrush();
            ellipse.Margin = new Thickness((1.0 - percentage) * rectangle.Width - ellipse.Width / 2, 0, 0, 0);
        }

        private void UpdateColorTemperature(int colorTemperature, int maxColorTemperature, int minColorTemperature)
        {
            Ellipse ellipse = this.FindName("ColorTemperatureCircle") as Ellipse;
            Rectangle rectangle = this.FindName("ColorTemperature") as Rectangle;

            colorTemperature = Math.Min(colorTemperature, maxColorTemperature);
            colorTemperature = Math.Max(colorTemperature, minColorTemperature);

            double percentage = (double)(colorTemperature - minColorTemperature) / (double)(maxColorTemperature - minColorTemperature);

            ellipse.Fill = RGB.GetBlendedColor(percentage, Colors.Gold, Colors.LightCyan).CreateSolidColorBrush();
            ellipse.Margin = new Thickness((1.0 - percentage) * rectangle.Width - ellipse.Width / 2, 0, 0, 0);
        }


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

        private void ColorWheelLine_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                Grid grid = this.FindName("Wheel") as Grid;

                PointerPoint pointerPointFromLine = e.GetCurrentPoint(sender as UIElement);
                PointerPoint pointerPointFromParent = e.GetCurrentPoint(grid as UIElement);
                
                MoveColorCircleToLineAndSetColor(sender as Line, pointerPointFromLine.Position, pointerPointFromParent.Position, grid.Margin);
            }
        }

        private void ColorWheelLine_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Grid grid = this.FindName("Wheel") as Grid;

            Point pointFromLine = e.GetPosition(sender as UIElement);
            Point pointFromGrid = e.GetPosition(grid as UIElement);

            MoveColorCircleToLineAndSetColor(sender as Line, pointFromLine, pointFromGrid, grid.Margin);

        }

        private void ColorWheelLine_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //Ellipse ellipse = this.FindName("WheelCircle") as Ellipse;
            //ellipse.Visibility = Visibility.Collapsed;
        }

        private void ColorWheelLine_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            //Grid grid = this.FindName("Wheel") as Grid;


            //Ellipse ellipse = this.FindName("WheelCircle") as Ellipse;
            //ellipse.Visibility = Visibility.Visible;

            //ellipse.Margin = new Thickness(
            //    e.GetCurrentPoint(grid as UIElement).Position.X - grid.Margin.Left / 2,
            //    e.GetCurrentPoint(grid as UIElement).Position.Y - grid.Margin.Top / 2,
            //    0, 0);
        }

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Ellipse ellipse = this.FindName("WheelCircle") as Ellipse;
            ellipse.Visibility = Visibility.Collapsed;
        }

        private void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Ellipse ellipse = this.FindName("WheelCircle") as Ellipse;
            ellipse.Visibility = Visibility.Visible;
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                Ellipse ellipse = this.FindName("WheelCircle") as Ellipse;
                ellipse.Visibility = Visibility.Visible;
            }
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

    }
}
