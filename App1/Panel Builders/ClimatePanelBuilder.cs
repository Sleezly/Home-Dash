using Hashboard;
using System;
using System.Collections.Generic;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace HashBoard
{
    public class ClimatePanelBuilder : PanelBuilderBase
    {
        public new int? FontSize { get; set; }

        protected override Panel CreateSinglePanel(Entity entity, int width, int height)
        {
            Grid grid = new Grid
            {
                Width = width,
                Height = height,
                Padding = new Thickness(PanelMargins)
            };

            TextBlock textName = new TextBlock
            {
                Text = entity.Name(),
                FontSize = base.FontSize,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = FontColorBrush,
                Padding = new Thickness(12)
            };

            TextBlock textTemperature = new TextBlock
            {
                Foreground = FontColorBrush,
                FontWeight = FontWeights.Bold,
                FontSize = FontSize ?? base.FontSize,
                Text = entity.Attributes.ContainsKey("temperature") ? entity.Attributes["temperature"] != null ?
                Convert.ToString(entity.Attributes["temperature"]) : entity.State : entity.State,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock textCurrentTemperature = new TextBlock
            {
                Foreground = FontColorBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Text = "Actual: " + (entity.Attributes.ContainsKey("current_temperature") ? entity.Attributes["current_temperature"] != null ?
                Convert.ToString(entity.Attributes["current_temperature"]) : string.Empty : string.Empty),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Padding = new Thickness(12)
            };

            if (entity.Attributes.ContainsKey("unit_of_measurement"))
            {
                if (entity.Attributes.ContainsKey("temperature"))
                {
                    textTemperature.Text += entity.Attributes["unit_of_measurement"];
                }

                if (entity.Attributes.ContainsKey("unit_of_measurement"))
                {
                    textCurrentTemperature.Text += entity.Attributes["unit_of_measurement"];
                }
            }

            grid.Background = ClimateControl.CreateLinearGradientBrush(entity);

            grid.Children.Add(textName);
            grid.Children.Add(textTemperature);
            grid.Children.Add(textCurrentTemperature);

            return grid;
        }

        protected override Panel CreateGroupPanel(Entity entity, IEnumerable<Entity> childrenEntities, int width, int height)
        {
            return null;
        }
    }
}
