using HashBoard;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using static HashBoard.Entity;

namespace Hashboard
{
    public partial class LightControl : UserControl
    {
        private const int MinimumColorTemperature = 154;

        private const int MaximumColorTemperature = 500;

        private RGB CurrentColor = new RGB(255);

        private Entity PanelEntity;

        private IEnumerable<Entity> ChildrenEntities;

        private enum ButtonState { NotPressed, Pressed }

        public LightControl(Entity entity, IEnumerable<Entity> childrenEntities)
        {
            this.InitializeComponent();

            this.RequestedTheme = ThemeControl.GetApplicationTheme();

            PanelEntity = entity;
            ChildrenEntities = childrenEntities;

            DrawColorWheel();

            InitializeUI();
        }

        /// <summary>
        /// Renders a beautiful color wheel with touch-enabled events from each gradient-blended line.
        /// </summary>
        private void DrawColorWheel()
        {
            Grid canvas = this.FindName("ColorWheel") as Grid;

            for (int i = 0; i < 360; i++)
            {
                RotateTransform rt = new RotateTransform() { Angle = i };
                LinearGradientBrush lgb = new LinearGradientBrush();
                Line line = new Line() { X1 = 0, Y1 = 0, X2 = canvas.Width / 2, Y2 = 0, StrokeThickness = 6, RenderTransform = rt};

                // Only allow interaction with the color wheel when color behavior is supported. To signify the color wheel
                // is disabled, also add a gray-tone to the colors.
                if (PanelEntity.GetSupportedFeatures(ChildrenEntities) == SupportedFeatures.Colors)
                {
                    line.Tapped += ColorWheelLine_Tapped;
                    line.PointerReleased += ColorWheelLine_PointerReleased;

                    lgb.GradientStops.Add(new GradientStop() { Color = Colors.White });
                    lgb.GradientStops.Add(new GradientStop() { Color = ColorConverter.HSVtoRGB(i, 1, 1), Offset = 1 });
                }
                else
                {
                    lgb.GradientStops.Add(new GradientStop() { Color = Color.FromArgb(255, 125, 125, 125) });
                    lgb.GradientStops.Add(new GradientStop() { Color = Color.FromArgb(255, 50, 50, 50), Offset = 1 });
                }

                line.Stroke = lgb;
                line.Margin = new Thickness(canvas.Width / 2, canvas.Width / 2, 0, 0);
                line.HorizontalAlignment = HorizontalAlignment.Left;
                line.VerticalAlignment = VerticalAlignment.Top;

                canvas.Children.Add(line);
            }
        }

        /// <summary>
        /// Render all UI.
        /// </summary>
        private void InitializeUI()
        {
            if (PanelEntity.Attributes.ContainsKey("friendly_name"))
            {
                TextBlock textBlock = FindName("DeviceText") as TextBlock;
                textBlock.Text = PanelEntity.Attributes["friendly_name"];
            }

            // For the group panel, average the color of children entities together
            if (ChildrenEntities != null)
            {
                // Set the color adjustment sliders and color wheel by blending the average
                // color for each. Include entities which are in the On state only.
                IEnumerable<Entity> onEntities = ChildrenEntities.Where(x => string.Equals(x.State, "on", StringComparison.InvariantCultureIgnoreCase));

                if (onEntities.Any())
                {
                    // Average the brightness 
                    IEnumerable<Entity> brightnessEntities = onEntities.Where(x => x.Attributes.ContainsKey("brightness"));
                    if (brightnessEntities.Any())
                    {
                        double averageBrightness = onEntities.Select(x => Convert.ToDouble(x.Attributes["brightness"])).Cast<double>().Average();
                        UpdateBrightnessControl(averageBrightness);
                    }
                    else
                    {
                        Ellipse ellipse = this.FindName("BrightnessCircle") as Ellipse;
                        ellipse.Visibility = Visibility.Collapsed;
                    }

                    // Average the color temperature
                    IEnumerable<Entity> colorTempEntities = onEntities.Where(x => x.Attributes.ContainsKey("color_temp"));
                    if (colorTempEntities.Any())
                    {
                        double averageColorTemperature = colorTempEntities.Select(x => Convert.ToDouble(x.Attributes["color_temp"])).Cast<double>().Average();

                        UpdateColorTemperatureControl(Convert.ToInt32(averageColorTemperature));

                        ShowColorTemperatureCircle(Visibility.Visible);
                    }
                    else
                    {
                        ShowColorTemperatureCircle(Visibility.Collapsed);
                    }

                    // Average the color
                    IEnumerable<Entity> rgbEntities = onEntities.Where(x => x.Attributes.ContainsKey("rgb_color"));
                    if (rgbEntities.Any())
                    {
                        RGB averageColor = RGB.Average(rgbEntities.Select(x => new RGB(
                            Convert.ToByte(x.Attributes["rgb_color"][0]),
                            Convert.ToByte(x.Attributes["rgb_color"][1]),
                            Convert.ToByte(x.Attributes["rgb_color"][2]))).Cast<RGB>());

                        SetColorCircleLocationAndColor(averageColor);

                        ShowColorWheelCircle(Visibility.Visible);
                    }
                    else
                    {
                        ShowColorWheelCircle(Visibility.Collapsed);
                    }
                }
                else
                {
                    UpdateBrightnessControl(0.0);

                    ShowColorTemperatureCircle(Visibility.Collapsed);
                    ShowColorWheelCircle(Visibility.Collapsed);
                }
            }
            else
            {
                if (string.Equals(PanelEntity.State, "on", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (PanelEntity.Attributes.ContainsKey("brightness"))
                    {
                        UpdateBrightnessControl(Convert.ToDouble(PanelEntity.Attributes["brightness"]));
                    }
                    else
                    {
                        Ellipse ellipse = this.FindName("BrightnessCircle") as Ellipse;
                        ellipse.Visibility = Visibility.Collapsed;
                    }

                    if (PanelEntity.Attributes.ContainsKey("color_temp"))
                    {
                        UpdateColorTemperatureControl(Convert.ToInt32(PanelEntity.Attributes["color_temp"]));

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
                else
                {
                    UpdateBrightnessControl(0.0);

                    ShowColorTemperatureCircle(Visibility.Collapsed);
                    ShowColorWheelCircle(Visibility.Collapsed);
                }
            }

            // ColorTemperature should be a constant yellowish-hue line for brightness-only lights
            if (PanelEntity.GetSupportedFeatures(ChildrenEntities) == SupportedFeatures.BrightnessOnly)
            {
                Rectangle colorTemperature = this.FindName("ColorTemperature") as Rectangle;
                colorTemperature.Fill = new SolidColorBrush(Colors.LightYellow);
            }

            // Update the power button last so as to render with any color updates
            ShowHightlightColor(ButtonState.NotPressed);
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

            double angle = (Math.PI / 180) * hsv.H;
            double radian = (grid.Width / 2) * hsv.S;
            double x = radian * Math.Cos(angle);
            double y = radian * Math.Sin(angle);

            // UI elements to have new color value
            CurrentColor = rgb;

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

            // Get the linear blended average color calculation
            RGB rgb = RGB.GetLinearBlendedColor(percentage, colorStart, colorEnd);

            // Use the same calculation to place the circle via-RGB as we do for cursor placement as this guarantees
            // both methods to place the ellipse generate the same result.
            SetColorCircleLocationAndColor(rgb);

            // Send the RGB update via REST API
            SendColorUpdate(CurrentColor);

            // Update the power button color as well
            PanelEntity.State = "on";
            ShowHightlightColor(ButtonState.NotPressed);
        }

        /// <summary>
        /// Sends a web request with desired new brightness value.
        /// </summary>
        /// <param name="brightness"></param>
        private void SendBrightnessUpdate(double brightness)
        {
            if (ChildrenEntities != null)
            {
                WebRequests.SendAction(
                    "turn_on",
                    ChildrenEntities.Select(x => x.EntityId),
                    new Dictionary<string, string>() {
                        { "brightness", Convert.ToInt32(brightness).ToString() }
                    });
            }
            else
            {
                WebRequests.SendAction(
                    "light",
                    "turn_on",
                    new Dictionary<string, string>() {
                        { "entity_id", PanelEntity.EntityId.ToString() },
                        { "brightness", Convert.ToInt32(brightness).ToString() },
                    });
            }
        }

        /// <summary>
        /// Sends a web request with desired new color temperature value.
        /// </summary>
        /// <param name="colorTemperature"></param>
        private void SendColorTemperatureUpdate(int colorTemperature)
        {
            if (ChildrenEntities != null)
            {
                WebRequests.SendAction(
                    "turn_on",
                    ChildrenEntities.Select(x => x.EntityId),
                    new Dictionary<string, string>() {
                        { "color_temp", Convert.ToInt32(colorTemperature).ToString() }
                    });
            }
            else
            {
                WebRequests.SendAction(
                    "light",
                    "turn_on",
                    new Dictionary<string, string>() {
                        { "entity_id", PanelEntity.EntityId.ToString() },
                        { "color_temp", Convert.ToInt32(colorTemperature).ToString() },
                    });
            }
        }

        /// <summary>
        /// Sends a web request with desired new RGB color value.
        /// </summary>
        /// <param name="RGB"></param>
        private void SendColorUpdate(RGB rgb)
        {
            if (ChildrenEntities != null)
            {
                WebRequests.SendAction(
                    "turn_on",
                    ChildrenEntities.Select(x => x.EntityId),
                    new Dictionary<string, string>() {
                        { "rgb_color", $"[{rgb.R},{rgb.G},{rgb.B}]" }
                    });
            }
            else
            {
                WebRequests.SendAction(
                    "light",
                    "turn_on",
                    new Dictionary<string, string>() {
                        { "entity_id", PanelEntity.EntityId.ToString() },
                        { "rgb_color", $"[{rgb.R},{rgb.G},{rgb.B}]" }
                    });
            }
        }

        /// <summary>
        /// Updates the Brightness slider.
        /// </summary>
        /// <param name="brightness"></param>
        private void UpdateBrightnessControl(double brightness)
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

            // Update the power button color as well
            PanelEntity.State = "on";
            ShowHightlightColor(ButtonState.NotPressed);
        }

        /// <summary>
        /// Updates the ColorTemperature slider.
        /// </summary>
        /// <param name="colorTemperature"></param>
        /// <param name="maxColorTemperature"></param>
        /// <param name="minColorTemperature"></param>
        private void UpdateColorTemperatureControl(int colorTemperature)
        {
            Ellipse ellipse = this.FindName("ColorTemperatureCircle") as Ellipse;
            Rectangle rectangle = this.FindName("ColorTemperature") as Rectangle;
            Grid grid = this.FindName("ColorTemperatureRoot") as Grid;

            colorTemperature = Math.Min(colorTemperature, MaximumColorTemperature);
            colorTemperature = Math.Max(colorTemperature, MinimumColorTemperature);

            double percentage = (double)(colorTemperature - MinimumColorTemperature) / (double)(MaximumColorTemperature - MinimumColorTemperature);
            double offset = (grid.Width - rectangle.Width) / 2 - (ellipse.Width / 2);

            CurrentColor = RGB.GetBlendedColor(percentage, Colors.Gold, Colors.LightCyan);

            ellipse.Fill = CurrentColor.CreateSolidColorBrush();
            ellipse.Margin = new Thickness(
                (1.0 - percentage) * rectangle.Width + offset, 0, 0, 0);

            // Update the power button color as well
            PanelEntity.State = "on";
            ShowHightlightColor(ButtonState.NotPressed);
        }

        /// <summary>
        /// Toggles the Power Button and change the color to match the new toggled state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PowerButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ChildrenEntities != null)
            {
                WebRequests.SendAction("toggle", ChildrenEntities.Select(x => x.EntityId));
            }
            else
            {
                WebRequests.SendAction(PanelEntity.EntityId, "toggle");
            }

            PanelEntity.State = PanelEntity.GetToggledState();

            ShowHightlightColor(ButtonState.NotPressed);
        }

        /// <summary>
        /// Dim the power button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PowerButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ShowHightlightColor(ButtonState.Pressed);
        }

        /// <summary>
        /// Cancel a dimming of the power button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PowerButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ShowHightlightColor(ButtonState.NotPressed);
        }

        /// <summary>
        /// Colors the power button with the provided highlight.
        /// </summary>
        /// <param name=""></param>
        private void ShowHightlightColor(ButtonState powerButtonState)
        {
            // Entity name should match current color for Dark theme only
            if (RequestedTheme == ElementTheme.Dark)
            {
                TextBlock textBlock = FindName("DeviceText") as TextBlock;
                textBlock.Foreground = CurrentColor.CreateSolidColorBrush();
            }

            // Power button should also match but only when enabled and not in a pressed state
            BitmapIcon bitmapIcon = this.FindName("ButtonPower") as BitmapIcon;

            if (powerButtonState == ButtonState.NotPressed)
            {
                if (string.Equals(PanelEntity.State, "on", StringComparison.InvariantCultureIgnoreCase))
                {
                    // On
                    if (RequestedTheme == ElementTheme.Dark)
                    {
                        bitmapIcon.Foreground = CurrentColor.CreateSolidColorBrush();
                    }
                    else
                    {
                        // Button will not be visible while in Light theme so simply use Foreground color here.
                        bitmapIcon.Foreground = this.Foreground;
                    }
                }
                else
                {
                    // Off
                    bitmapIcon.Foreground = new SolidColorBrush(Colors.DarkGray);
                }
            }
            else
            {
                // Pressed
                bitmapIcon.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        /// <summary>
        /// Shows a circle over the color temperature slider.
        /// </summary>
        /// <param name="visibility"></param>
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

        /// <summary>
        /// Shows a circle over the Color Wheel.
        /// </summary>
        /// <param name="visibility"></param>
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

        /// <summary>
        /// Moves the Color Wheel circle to the selected location and sets the Fill color.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorWheelLine_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Grid grid = this.FindName("ColorWheel") as Grid;

            PointerPoint pointerPointFromLine = e.GetCurrentPoint(sender as UIElement);
            PointerPoint pointerPointFromParent = e.GetCurrentPoint(grid as UIElement);

            // Sending a color-change on drag overloads Home Assistant so disable this
            UpdateColorWheel(sender as Line, pointerPointFromLine.Position, pointerPointFromParent.Position, grid.Margin);
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
            if (PanelEntity.GetSupportedFeatures(ChildrenEntities) == SupportedFeatures.Colors)
            {
                ShowColorWheelCircle(Visibility.Collapsed);
            }
        }

        /// <summary>
        /// Shows the Color Wheel circleand hides the Color Temperature circle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorWheelGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (PanelEntity.GetSupportedFeatures(ChildrenEntities) == SupportedFeatures.Colors)
            {
                ShowColorWheelCircle(Visibility.Visible);
            }
        }

        /// <summary>
        /// Shows the Color Wheel circle and hides the Color Temperature circle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorWheelGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (PanelEntity.GetSupportedFeatures(ChildrenEntities) == SupportedFeatures.Colors)
            {
                if (e.Pointer.IsInContact)
                {
                    ShowColorWheelCircle(Visibility.Visible);
                }
            }
        }

        private void ColorTemperature_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (PanelEntity.GetSupportedFeatures(ChildrenEntities) != SupportedFeatures.BrightnessOnly)
            {
                ShowColorTemperatureCircle(Visibility.Visible);

                Rectangle rectangle = this.FindName("ColorTemperature") as Rectangle;
                double percentage = 1.0 - e.GetPosition(rectangle).X / rectangle.Width;

                SetColorTemperature(percentage);
            }
        }

        private void ColorTemperature_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (PanelEntity.GetSupportedFeatures(ChildrenEntities) != SupportedFeatures.BrightnessOnly)
            {
                ShowColorTemperatureCircle(Visibility.Collapsed);
            }
        }

        private void ColorTemperature_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (PanelEntity.GetSupportedFeatures(ChildrenEntities) != SupportedFeatures.BrightnessOnly)
            {
                ShowColorTemperatureCircle(Visibility.Visible);
            }
        }

        private void SetColorTemperature(double percentage)
        {
            if (PanelEntity.GetSupportedFeatures(ChildrenEntities) != SupportedFeatures.BrightnessOnly)
            {
                int temperature = Convert.ToInt32((MaximumColorTemperature - MinimumColorTemperature) * percentage + MinimumColorTemperature);

                UpdateColorTemperatureControl(temperature);

                SendColorTemperatureUpdate(temperature);
            }
        }

        private void ColorTemperatureCircle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (PanelEntity.GetSupportedFeatures(ChildrenEntities) != SupportedFeatures.BrightnessOnly)
            {
                if (e.Pointer.IsInContact)
                {
                    ShowColorTemperatureCircle(Visibility.Collapsed);
                }
            }
        }

        private void ColorTemperatureCircle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (PanelEntity.GetSupportedFeatures(ChildrenEntities) != SupportedFeatures.BrightnessOnly)
            {
                if (e.Pointer.IsInContact)
                {
                    ShowColorTemperatureCircle(Visibility.Collapsed);
                }
            }
        }

        /// <summary>
        /// Brightness
        /// </summary>
        private void Brightness_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Ellipse ellipse = this.FindName("BrightnessCircle") as Ellipse;
            ellipse.Visibility = Visibility.Visible;

            Rectangle rectangle = this.FindName("BrightnessRectangle") as Rectangle;
            double percentage = e.GetPosition(rectangle).X / rectangle.Width;

            UpdateBrightnessControl(255 * percentage);

            SendBrightnessUpdate(255 * percentage);
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
    }
}
