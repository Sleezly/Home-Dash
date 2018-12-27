using HashBoard;
using System;
using System.Collections.Generic;
using Windows.UI;
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
        }

        /// <summary>
        /// Respond to entity changes while the popup control is up.
        /// </summary>
        /// <param name="entity"></param>
        public void EntityUpdated(Entity entity, IEnumerable<Entity> childrenEntities)
        {
            ClimateEntity = entity;

            InitializeUI();
        }

        public static LinearGradientBrush CreateLinearGradientBrush(Entity entity)
        {
            LinearGradientBrush lgb = new LinearGradientBrush();

            if (entity.Attributes.ContainsKey("operation_mode"))
            {
                switch (entity.Attributes["operation_mode"] as string)
                {
                    case "off":
                        lgb.GradientStops.Add(new GradientStop() { Color = Colors.LightGray, Offset = 0 });
                        lgb.GradientStops.Add(new GradientStop() { Color = Colors.Gray, Offset = 1 });
                        break;

                    case "eco":
                        lgb.GradientStops.Add(new GradientStop() { Color = Colors.LightGreen, Offset = 0 });
                        lgb.GradientStops.Add(new GradientStop() { Color = Colors.Green, Offset = 1 });
                        break;

                    case "heat":
                        lgb.GradientStops.Add(new GradientStop() { Color = Colors.Orange, Offset = 0 });
                        lgb.GradientStops.Add(new GradientStop() { Color = Colors.OrangeRed, Offset = 1 });
                        break;

                    case "cool":
                        lgb.GradientStops.Add(new GradientStop() { Color = Colors.LightSkyBlue, Offset = 0 });
                        lgb.GradientStops.Add(new GradientStop() { Color = Colors.LightCyan, Offset = 1 });
                        break;

                    default:
                        throw new ArgumentOutOfRangeException($"Unhandled climate operation mode '{entity.Attributes["operation_mode"]}'.");
                }
            }
            else
            {
                lgb.GradientStops.Add(new GradientStop() { Color = Colors.LightGray, Offset = 0 });
                lgb.GradientStops.Add(new GradientStop() { Color = Colors.DarkGray, Offset = 1 });
            }

            return lgb;
        }

        private void InitializeUI()
        {
            TextBlock deviceName = this.FindName("DeviceText") as TextBlock;
            TextBlock currentTemperature = this.FindName("CurrentTemperature") as TextBlock;
            TextBlock targetTemperature = this.FindName("TargetTemperature") as TextBlock;

            deviceName.Text = ClimateEntity.Attributes["friendly_name"].ToUpper();
            currentTemperature.Text = $"ACTUAL: {ClimateEntity.Attributes["current_temperature"]}";
            targetTemperature.Text = $"{ClimateEntity.Attributes["temperature"]}";

            // Operation drop-down
            ComboBox comboOperation = this.FindName("ComboOperation") as ComboBox;
            comboOperation.SelectionChanged -= ComboOperation_SelectionChanged;
            comboOperation.Items.Clear();

            foreach (string item in ClimateEntity.Attributes["operation_list"])
            {
                comboOperation.Items.Add(item);
            }

            comboOperation.SelectedItem = ClimateEntity.Attributes["operation_mode"] as string;
            comboOperation.SelectionChanged += ComboOperation_SelectionChanged;

            // Fan Mode drop-down
            ComboBox comboFanMode = this.FindName("ComboFanMode") as ComboBox;            
            comboFanMode.SelectionChanged -= ComboFanMode_SelectionChanged;
            comboFanMode.Items.Clear();

            foreach (string item in ClimateEntity.Attributes["fan_list"])
            {
                comboFanMode.Items.Add(item);
            }

            comboFanMode.SelectedItem = ClimateEntity.Attributes["fan_mode"] as string;
            comboFanMode.SelectionChanged += ComboFanMode_SelectionChanged;

            // Colorful circle
            SetEllipse();
        }

        private void SetEllipse()
        {
            Ellipse ellipse = this.FindName("Ellipse") as Ellipse;
            ellipse.Fill = CreateLinearGradientBrush(ClimateEntity);
        }

        private void BitmapIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = new SolidColorBrush(Colors.LightYellow);

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
            targetTemperature.Text = $"{ClimateEntity.Attributes["temperature"]}";
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
                bitmapIcon.Foreground = new SolidColorBrush(Colors.LightYellow);
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