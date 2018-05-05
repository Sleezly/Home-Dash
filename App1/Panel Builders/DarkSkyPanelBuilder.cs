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
            string weatherImageAsset;

            if (darkSkySensorState.Contains("clear") || darkSkySensorState.Contains("sun") || darkSkySensorState.Contains("weather-night"))
            {
                if (darkSkySensorState.Contains("night"))
                {
                    weatherImageAsset = "weather_clear_night.png";
                }
                else
                {
                    weatherImageAsset = "weather_clear_day.png";
                }
            }
            else if (darkSkySensorState.Contains("partly"))
            {
                if (darkSkySensorState.Contains("night"))
                {
                    weatherImageAsset = "weather_partly_cloudy_night.png";
                }
                else
                {
                    weatherImageAsset = "weather_partly_cloudy_day.png";
                }
            }
            else if (darkSkySensorState.Contains("sleet") || darkSkySensorState.Contains("snowy-rain"))
            {
                weatherImageAsset = "weather_snow_rain.png";
            }
            else if (darkSkySensorState.Contains("cloud"))
            {
                weatherImageAsset = "weather_cloudy.png";
            }
            else if (darkSkySensorState.Contains("fog"))
            {
                weatherImageAsset = "weather_fog.png";
            }
            else if (darkSkySensorState.Contains("rain") || darkSkySensorState.Contains("pour"))
            {
                weatherImageAsset = "weather_rain.png";
            }
            else if (darkSkySensorState.Contains("snow"))
            {
                weatherImageAsset = "weather_snow.png";
            }
            else if (darkSkySensorState.Contains("wind"))
            {
                weatherImageAsset = "weather_windy.png";
            }
            else
            {
                weatherImageAsset = "weather_na.png";
            }

            return Imaging.LoadImage(weatherImageAsset);
        }
    }
}
