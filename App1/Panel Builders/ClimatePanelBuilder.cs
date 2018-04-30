using Hashboard;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace HashBoard
{
    public class ClimatePanelBuilder : PanelBuilderBase
    {
        public new int? FontSize { get; set; }

        protected override Panel CreateSinglePanel(Entity entity, int width, int height)
        {
            Grid grid = new Grid();
            grid.Width = width;
            grid.Height = height;

            TextBlock textName = new TextBlock();
            textName.Text = entity.Attributes["friendly_name"];
            textName.FontSize = base.FontSize;
            textName.TextWrapping = TextWrapping.Wrap;
            textName.HorizontalAlignment = HorizontalAlignment.Center;
            textName.VerticalAlignment = VerticalAlignment.Top;
            textName.Foreground = FontColorBrush;
            textName.Padding = new Thickness(12);

            TextBlock textTemperature = new TextBlock();
            textTemperature.Foreground = FontColorBrush;
            textTemperature.FontWeight = FontWeights.Bold;
            textTemperature.FontSize = FontSize.HasValue ? FontSize.Value : base.FontSize;
            textTemperature.Text = Convert.ToString(entity.Attributes["temperature"]);
            textTemperature.TextWrapping = TextWrapping.Wrap;
            textTemperature.HorizontalAlignment = HorizontalAlignment.Center;
            textTemperature.VerticalAlignment = VerticalAlignment.Center;

            TextBlock textCurrentTemperature = new TextBlock();
            textCurrentTemperature.Foreground = FontColorBrush;
            textCurrentTemperature.FontWeight = FontWeights.Bold;
            textCurrentTemperature.FontSize = 14;
            textCurrentTemperature.Text = "Actual: " + Convert.ToString(entity.Attributes["current_temperature"]);
            textCurrentTemperature.TextWrapping = TextWrapping.Wrap;
            textCurrentTemperature.HorizontalAlignment = HorizontalAlignment.Center;
            textCurrentTemperature.VerticalAlignment = VerticalAlignment.Bottom;
            textCurrentTemperature.Padding = new Thickness(12);

            if (entity.Attributes.ContainsKey("unit_of_measurement"))
            {
                textTemperature.Text += entity.Attributes["unit_of_measurement"];
                textCurrentTemperature.Text += entity.Attributes["unit_of_measurement"];
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
