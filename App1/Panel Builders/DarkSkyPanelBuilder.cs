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
            panel.Padding = new Thickness(Padding);
            panel.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            TextBlock textDate = new TextBlock();
            textDate.Text = entity.Attributes["friendly_name"];
            textDate.HorizontalAlignment = HorizontalAlignment.Center;
            textDate.VerticalAlignment = VerticalAlignment.Top;
            //textDate.FontWeight = FontWeights.Bold;
            textDate.Foreground = FontColorBrush;
            //textDate.FontSize = FontSize;

            //Border border = new Border();
            //border.BorderThickness = new Thickness(2);
            //border.BorderBrush = new SolidColorBrush(Colors.Black);

            Image image = GetWeatherImage(entity.Attributes["icon"]);
            image.Width = panel.Width - Padding * 2;
            image.VerticalAlignment = VerticalAlignment.Center;
            image.HorizontalAlignment = HorizontalAlignment.Center;
            //BitmapCache mode??

            //border.Child = image;

            TextBlock textTemperature = new TextBlock();
            textTemperature.Text = string.Join(" | ", entity.State.Split('/'));
            textTemperature.HorizontalAlignment = HorizontalAlignment.Center;
            textTemperature.VerticalAlignment = VerticalAlignment.Bottom;
            //textTemperature.FontWeight = FontWeights.Bold;
            textTemperature.Foreground = FontColorBrush;
            //textTemperature.FontSize = FontSize;

            panel.Children.Add(textDate);
            panel.Children.Add(image);
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
                throw new ArgumentOutOfRangeException($"Unknown DarkSky Sensor State Value '{darkSkySensorState}'");
            }

            return Imaging.LoadImage(weatherImageAsset);
        }
    }
}
