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
            Grid panel = new Grid
            {
                Width = width,
                Height = height,
                Padding = new Thickness(PanelMargins),
                Background = new SolidColorBrush(Colors.Transparent),
                CacheMode = new BitmapCache()
            };

            TextBlock textDate = new TextBlock
            {
                Text = entity.Attributes["friendly_name"] ?? string.Empty,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = FontColorBrush
            };

            panel.Children.Add(textDate);

            if (entity.Attributes.ContainsKey("icon"))
            {
                Image image = GetWeatherImage(entity.Attributes["icon"]);
                image.Width = panel.Width - PanelPadding * 2;
                image.VerticalAlignment = VerticalAlignment.Center;
                image.HorizontalAlignment = HorizontalAlignment.Center;

                panel.Children.Add(image);
            }

            TextBlock textTemperature = new TextBlock
            {
                Text = string.Join(" | ", entity.State.Split('/')),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Foreground = FontColorBrush
            };

            panel.Children.Add(textTemperature);

            return panel;
        }

        private static Image GetWeatherImage(string state)
        {
            if (state.Contains(":"))
            {
                return Imaging.LoadImage($"weather\\{state.Split(':')[1]}.png");
            }
            else
            {
                return Imaging.LoadImage($"weather\\error.png");
            }
        }
    }
}
