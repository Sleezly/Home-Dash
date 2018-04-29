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

namespace Hashboard
{
    public partial class ClimateControl : UserControl
    {
        Entity ClimateEntity;

        public ClimateControl(Entity entity)
        {
            this.InitializeComponent();

            this.RequestedTheme = ThemeControl.GetApplicationTheme();

            ClimateEntity = entity;

            InitializeUI();

            SetEllipse();
        }

        private void InitializeUI()
        {
            TextBlock deviceName = this.FindName("DeviceText") as TextBlock;
            TextBlock currentTemperature = this.FindName("CurrentTemperature") as TextBlock;
            TextBlock targetTemperature = this.FindName("TargetTemperature") as TextBlock;

            deviceName.Text = ClimateEntity.Attributes["friendly_name"];
            currentTemperature.Text = ClimateEntity.Attributes["current_temperature"] + ClimateEntity.Attributes["unit_of_measurement"];
            targetTemperature.Text = $"Target: {ClimateEntity.Attributes["temperature"]}{ClimateEntity.Attributes["unit_of_measurement"]}";

            ComboBox comboOperation = this.FindName("ComboOperation") as ComboBox;
            ComboBox comboFanMode = this.FindName("ComboFanMode") as ComboBox;

            foreach (string item in ClimateEntity.Attributes["operation_list"])
            {
                comboOperation.Items.Add(item);
            }

            comboOperation.SelectionChanged -= ComboOperation_SelectionChanged;
            comboOperation.SelectedItem = ClimateEntity.Attributes["operation_mode"] as string;
            comboOperation.SelectionChanged += ComboOperation_SelectionChanged;

            foreach (string item in ClimateEntity.Attributes["fan_list"])
            {
                comboFanMode.Items.Add(item);
            }

            comboFanMode.SelectionChanged -= ComboFanMode_SelectionChanged;
            comboFanMode.SelectedItem = ClimateEntity.Attributes["fan_mode"] as string;
            comboFanMode.SelectionChanged += ComboFanMode_SelectionChanged;
        }

        private void SetEllipse()
        {
            Ellipse ellipse = this.FindName("Ellipse") as Ellipse;
            LinearGradientBrush lgb = new LinearGradientBrush();

            switch (ClimateEntity.Attributes["operation_mode"] as string)
            {
                case "off":
                    lgb.GradientStops.Add(new GradientStop() { Color = Colors.Gray });
                    lgb.GradientStops.Add(new GradientStop() { Color = Colors.LightGray });
                    break;

                case "eco":
                    lgb.GradientStops.Add(new GradientStop() { Color = Colors.Green });
                    lgb.GradientStops.Add(new GradientStop() { Color = Colors.LightGreen });
                    break;

                case "heat":
                    lgb.GradientStops.Add(new GradientStop() { Color = Colors.Orange });
                    lgb.GradientStops.Add(new GradientStop() { Color = Colors.OrangeRed });
                    break;

                case "cool":
                    lgb.GradientStops.Add(new GradientStop() { Color = Colors.LightSkyBlue });
                    lgb.GradientStops.Add(new GradientStop() { Color = Colors.LightCyan });
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Unhandled climate operation mode '{ClimateEntity.Attributes["operation_mode"]}'.");
            }

            ellipse.Fill = lgb;
        }

        private void BitmapIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = new SolidColorBrush(Colors.Black);

            int temperatureToSet = Convert.ToInt32(ClimateEntity.Attributes["temperature"]);

            if (string.Equals(bitmapIcon.Name, "ArrowUp"))
            {
                temperatureToSet++;
            }
            else
            {
                temperatureToSet--;
            }

            // Save the new temperate value
            ClimateEntity.Attributes["temperature"] = temperatureToSet;

            WebRequests.SendAction("climate", "set_temperature", new Dictionary<string, string>() {
                { "entity_id", ClimateEntity.EntityId },
                { "temperature", temperatureToSet.ToString() },
            });

            // Update the UI
            TextBlock targetTemperature = this.FindName("TargetTemperature") as TextBlock;
            targetTemperature.Text = $"Target: {ClimateEntity.Attributes["temperature"]}{ClimateEntity.Attributes["unit_of_measurement"]}";
        }

        private void BitmapIcon_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = new SolidColorBrush(Colors.DarkGray);
        }

        private void BitmapIcon_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                BitmapIcon bitmapIcon = sender as BitmapIcon;
                bitmapIcon.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void ComboOperation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            ClimateEntity.Attributes["operation_mode"] = comboBox.SelectedItem.ToString();

            WebRequests.SendAction("climate", "set_operation_mode", new Dictionary<string, string>() {
                { "entity_id", ClimateEntity.EntityId },
                { "operation_mode", comboBox.SelectedItem.ToString() },
            });

            SetEllipse();
        }

        private void ComboFanMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            ClimateEntity.Attributes["fan_mode"] = comboBox.SelectedItem.ToString();

            WebRequests.SendAction("climate", "set_fan_mode", new Dictionary<string, string>() {
                { "entity_id", ClimateEntity.EntityId },
                { "fan_mode", comboBox.SelectedItem.ToString() },
            });
        }
    }
}