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
            Grid canvas = this.FindName("ColorWheel") as Grid;

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
                line.PointerMoved += ColorWheelLine_PointerMoved;

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
                UpdateBrightness(Convert.ToDouble(PanelEntity.Attributes["brightness"]));
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

                ShowColorTemperatureCircle(Visibility.Visible);
            }
            else
            {
                ShowColorTemperatureCircle(Visibility.Collapsed);
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

                ShowColorWheelCircle(Visibility.Visible);
            }
            else
            {
                ShowColorWheelCircle(Visibility.Collapsed);
            }
        }

        /// <summary>
        /// Updates the Color Wheel based on input from color source.
        /// </summary>
        /// <param name="rgb"></param>
        private void SetColorCircleLocationAndColor(RGB rgb)
        {
            Grid grid = this.FindName("ColorWheel") as Grid;
            Ellipse ellipse = this.FindName("ColorWheelCircle") as Ellipse;

            HSV hsv = ColorConverter.RGBtoHSV(rgb);

            Line line = grid.Children[(int)hsv.H] as Line;

            double angle = (Math.PI / 180) * hsv.H;
            double radian = (grid.Width / 2) * hsv.S;
            double x = radian * Math.Cos(angle);
            double y = radian * Math.Sin(angle);

            ellipse.Fill = rgb.CreateSolidColorBrush();

            ellipse.Margin = new Thickness(
                x + grid.Width / 2,
                y + grid.Height / 2,
                0,
                0);
        }

        /// <summary>
        /// Updates the Color Wheel based on input from the UI.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="pointFromLine"></param>
        /// <param name="pointFromParent"></param>
        /// <param name="marginFromParent"></param>
        private void UpdateColorWheel(Line line, Point pointFromLine, Point pointFromParent, Thickness marginFromParent)
        {
            Ellipse ellipse = this.FindName("ColorWheelCircle") as Ellipse;

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

            //StackPanel root = this.FindName("RootPanel") as StackPanel;
            //root.Background = rgb.CreateSolidColorBrush();
        }

        /// <summary>
        /// Updates the Brightness slider.
        /// </summary>
        /// <param name="brightness"></param>
        private void UpdateBrightness(double brightness)
        {
            Ellipse ellipse = this.FindName("BrightnessCircle") as Ellipse;
            Rectangle rectangle = this.FindName("BrightnessRectangle") as Rectangle;
            Grid grid = this.FindName("BrightnessRoot") as Grid;

            brightness = Math.Max(brightness, 0.0);
            brightness = Math.Min(brightness, 255.0);

            double percentage = 1.0 - brightness / 255.0;
            double offset = (grid.Width - rectangle.Width) / 2 - (ellipse.Width / 2);

            ellipse.Fill = RGB.GetBlendedColor(percentage, Colors.DarkSlateGray, Colors.LightGray).CreateSolidColorBrush();
            ellipse.Margin = new Thickness(
                (1.0 - percentage) * rectangle.Width + offset, 0, 0, 0);
        }

        /// <summary>
        /// Updates the ColorTemperature slider.
        /// </summary>
        /// <param name="colorTemperature"></param>
        /// <param name="maxColorTemperature"></param>
        /// <param name="minColorTemperature"></param>
        private void UpdateColorTemperature(int colorTemperature, int maxColorTemperature, int minColorTemperature)
        {
            Ellipse ellipse = this.FindName("ColorTemperatureCircle") as Ellipse;
            Rectangle rectangle = this.FindName("ColorTemperature") as Rectangle;
            Grid grid = this.FindName("ColorTemperatureRoot") as Grid;

            colorTemperature = Math.Min(colorTemperature, maxColorTemperature);
            colorTemperature = Math.Max(colorTemperature, minColorTemperature);

            double percentage = (double)(colorTemperature - minColorTemperature) / (double)(maxColorTemperature - minColorTemperature);
            double offset = (grid.Width - rectangle.Width) / 2 - (ellipse.Width / 2);

            ellipse.Fill = RGB.GetBlendedColor(percentage, Colors.Gold, Colors.LightCyan).CreateSolidColorBrush();
            ellipse.Margin = new Thickness(
                (1.0 - percentage) * rectangle.Width + offset, 0, 0, 0);
        }

        /// <summary>
        /// Toggles the Power Button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PowerButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;

            bitmapIcon.Foreground = new SolidColorBrush(Colors.White);
            WebRequests.SendAction(PanelEntity.EntityId, "toggle");
        }

        private void PowerButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = new SolidColorBrush(Colors.DarkGray);
        }
        private void PowerButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = new SolidColorBrush(Colors.White);
        }

        /// <summary>
        /// Moves the Color Wheel circle to the selected location and sets the Fill color.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorWheelLine_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                Grid grid = this.FindName("ColorWheel") as Grid;

                PointerPoint pointerPointFromLine = e.GetCurrentPoint(sender as UIElement);
                PointerPoint pointerPointFromParent = e.GetCurrentPoint(grid as UIElement);
                
                UpdateColorWheel(sender as Line, pointerPointFromLine.Position, pointerPointFromParent.Position, grid.Margin);
            }
        }

        /// <summary>
        /// Moves the Color Wheel circle to the selected location and sets the Fill color.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorWheelLine_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Grid grid = this.FindName("ColorWheel") as Grid;

            Point pointFromLine = e.GetPosition(sender as UIElement);
            Point pointFromGrid = e.GetPosition(grid as UIElement);

            UpdateColorWheel(sender as Line, pointFromLine, pointFromGrid, grid.Margin);
        }

        /// <summary>
        /// Hides the Color Wheel circle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorWheelGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ShowColorWheelCircle(Visibility.Collapsed);
        }

        /// <summary>
        /// Shows the Color Wheel circleand hides the Color Temperature circle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorWheelGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ShowColorWheelCircle(Visibility.Visible);
        }

        /// <summary>
        /// Shows the Color Wheel circle and hides the Color Temperature circle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorWheelGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                ShowColorWheelCircle(Visibility.Visible);
            }
        }

        private void ColorTemperature_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ShowColorTemperatureCircle(Visibility.Visible);

            Rectangle rectangle = this.FindName("ColorTemperature") as Rectangle;
            double percentage = 1.0 - e.GetPosition(rectangle).X / rectangle.Width;
  
            SetColorTemperature(percentage);
        }

        private void ColorTemperature_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ShowColorTemperatureCircle(Visibility.Collapsed);
        }

        private void ColorTemperature_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ShowColorTemperatureCircle(Visibility.Visible);
        }

        private void ColorTemperature_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                Rectangle rectangle = this.FindName("ColorTemperature") as Rectangle;
                double percentage = 1.0 - e.GetCurrentPoint(rectangle).Position.X / rectangle.Width;

                SetColorTemperature(percentage);
            }
        }

        private void ColorTemperature_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                Rectangle rectangle = this.FindName("ColorTemperature") as Rectangle;
                double percentage = 1.0 - e.GetCurrentPoint(rectangle).Position.X / rectangle.Width;

                SetColorTemperature(percentage);

                ShowColorTemperatureCircle(Visibility.Visible);
            }
        }

        private void SetColorTemperature(double percentage)
        {
            int minTemperature = Convert.ToInt32(PanelEntity.Attributes["min_mireds"]);
            int maxTemperature = Convert.ToInt32(PanelEntity.Attributes["max_mireds"]);
            int temperature = Convert.ToInt32((maxTemperature - minTemperature) * percentage + minTemperature);

            UpdateColorTemperature(temperature, maxTemperature, minTemperature);
        }

        private void ShowColorTemperatureCircle(Visibility visibility)
        {
            if (visibility == Visibility.Collapsed)
            {
                Ellipse colorWheelEllipse = this.FindName("ColorTemperatureCircle") as Ellipse;
                colorWheelEllipse.Visibility = Visibility.Collapsed;
            }
            else
            {
                Ellipse colorWheelEllipse = this.FindName("ColorWheelCircle") as Ellipse;
                colorWheelEllipse.Visibility = Visibility.Collapsed;

                Ellipse colorTemperatureEllipse = this.FindName("ColorTemperatureCircle") as Ellipse;
                colorTemperatureEllipse.Visibility = Visibility.Visible;
            }
        }

        private void ShowColorWheelCircle(Visibility visibility)
        {
            if (visibility == Visibility.Collapsed)
            {
                Ellipse colorTemperatureEllipse = this.FindName("ColorWheelCircle") as Ellipse;
                colorTemperatureEllipse.Visibility = Visibility.Collapsed;
            }
            else
            {
                Ellipse colorWheelEllipse = this.FindName("ColorWheelCircle") as Ellipse;
                colorWheelEllipse.Visibility = Visibility.Visible;

                Ellipse colorTemperatureEllipse = this.FindName("ColorTemperatureCircle") as Ellipse;
                colorTemperatureEllipse.Visibility = Visibility.Collapsed;
            }
        }

        private void ColorTemperatureCircle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                ShowColorTemperatureCircle(Visibility.Collapsed);
            }
        }

        private void ColorTemperatureCircle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                ShowColorTemperatureCircle(Visibility.Collapsed);
            }
        }

        /// <summary>
        /// Brightness
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Brightness_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Ellipse ellipse = this.FindName("BrightnessCircle") as Ellipse;
            ellipse.Visibility = Visibility.Visible;

            Rectangle rectangle = this.FindName("BrightnessRectangle") as Rectangle;
            double percentage = e.GetPosition(rectangle).X / rectangle.Width;

            UpdateBrightness(255 * percentage);
        }

        private void Brightness_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Ellipse ellipse = this.FindName("BrightnessCircle") as Ellipse;
            ellipse.Visibility = Visibility.Collapsed;

        }

        private void Brightness_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Ellipse ellipse = this.FindName("BrightnessCircle") as Ellipse;
            ellipse.Visibility = Visibility.Visible;
        }

        private void Brightness_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                Ellipse ellipse = this.FindName("BrightnessCircle") as Ellipse;
                ellipse.Visibility = Visibility.Collapsed;

                Rectangle rectangle = this.FindName("BrightnessRectangle") as Rectangle;
                double percentage = e.GetCurrentPoint(rectangle).Position.X / rectangle.Width;

                UpdateBrightness(255 * percentage);
            }
        }

        private void Brightness_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                Ellipse ellipse = this.FindName("BrightnessCircle") as Ellipse;
                ellipse.Visibility = Visibility.Visible;

                Rectangle rectangle = this.FindName("BrightnessRectangle") as Rectangle;
                double percentage = e.GetCurrentPoint(rectangle).Position.X / rectangle.Width;

                UpdateBrightness(255 * percentage);
            }
        }

        private void BrightnessCircle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                Ellipse ellipse = sender as Ellipse;
                ellipse.Visibility = Visibility.Collapsed;
            }
        }

        private void BrightnessCircle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                Ellipse ellipse = sender as Ellipse;
                ellipse.Visibility = Visibility.Collapsed;
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
