using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace HashBoard
{
    public class DarkSkyPanelbuilder : PanelBuilderBase
    {
        protected override Panel CreateGroupPanel(Entity entity, IEnumerable<Entity> childrenEntities, int width, int height)
        {
            return null;
        }

        protected override Panel CreateSinglePanel(Entity entity, int width, int height)
        {
            Grid panel = new Grid();
            panel.Width = width;
            panel.Height = height;
            panel.Padding = new Thickness(PanelMargins);
            panel.Background = new SolidColorBrush(Colors.Transparent);
            panel.CacheMode = new BitmapCache();

            TextBlock textDate = new TextBlock();
            textDate.Text = entity.Attributes["friendly_name"] ?? string.Empty;
            textDate.HorizontalAlignment = HorizontalAlignment.Center;
            textDate.VerticalAlignment = VerticalAlignment.Top;
            textDate.Foreground = FontColorBrush;

            panel.Children.Add(textDate);

            if (entity.Attributes.ContainsKey("icon"))
            {
                Image image = GetWeatherImage(entity.Attributes["icon"]);
                image.Width = panel.Width - PanelPadding * 2;
                image.VerticalAlignment = VerticalAlignment.Center;
                image.HorizontalAlignment = HorizontalAlignment.Center;

                panel.Children.Add(image);
            }

            TextBlock textTemperature = new TextBlock();
            textTemperature.Text = string.Join(" | ", entity.State.Split('/'));
            textTemperature.HorizontalAlignment = HorizontalAlignment.Center;
            textTemperature.VerticalAlignment = VerticalAlignment.Bottom;
            textTemperature.Foreground = FontColorBrush;

            panel.Children.Add(textTemperature);

            return panel;
        }

        private static Image GetWeatherImage(string darkSkySensorState)
        {
            if (darkSkySensorState.Contains(":"))
            {
                return Imaging.LoadImage($"weather\\{darkSkySensorState.Split(':')[1]}.png");
            }
            else
            {
                return Imaging.LoadImage($"weather\\error.png");
            }
        }
    }
}
