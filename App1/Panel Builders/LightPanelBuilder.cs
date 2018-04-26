using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace HashBoard
{
    public class LightPanelBuilder : PanelBuilderBase
    {
        protected override Panel CreateSinglePanel(Entity entity, int width, int height)
        {
            SolidColorBrush backgroundBrush;

            if (entity.State == "on")
            {
                RGB rgb = GetColor(entity);
                backgroundBrush = new SolidColorBrush(Color.FromArgb(CellOpacity, rgb.R, rgb.G, rgb.B));
            }
            else
            {
                backgroundBrush = NoninteractiveBrush;
            }

            return CreatePanel(entity, width, height, backgroundBrush);
        }

        protected override Panel CreateGroupPanel(Entity entity, IEnumerable<Entity> childrenEntities, int width, int height)
        {
            List<RGB> rgbList = new List<RGB>();

            foreach (Entity childEntity in childrenEntities)
            {
                if (childEntity.State == "on")
                {
                    rgbList.Add(GetColor(childEntity));
                }
            }

            SolidColorBrush backgroundBrush;
            if (rgbList.Count > 0)
            {
                RGB rgb = RGB.Average(rgbList);
                backgroundBrush = new SolidColorBrush(Color.FromArgb(CellOpacity, rgb.R, rgb.G, rgb.B));
            }
            else
            {
                backgroundBrush = NoninteractiveBrush;
            }

            return CreatePanel(entity, width, height, backgroundBrush);
        }

        private Panel CreatePanel(Entity entity, int width, int height, SolidColorBrush backgroundBrush)
        {
            DockPanel panel = new DockPanel();
            panel.Width = width;
            panel.Height = height;

            panel.Background = backgroundBrush;

            TextBlock textBlock = new TextBlock();
            textBlock.Foreground = FontColorBrush;
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.FontSize = 16;
            textBlock.Text = entity.Attributes["friendly_name"];
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.FontSize = FontSize;

            panel.Children.Add(textBlock);

            return panel;
        }

        private RGB GetColor(Entity entity)
        {
            if (entity.Attributes.ContainsKey("rgb_color"))
            {
                byte r = Convert.ToByte(entity.Attributes["rgb_color"][0]);
                byte g = Convert.ToByte(entity.Attributes["rgb_color"][1]);
                byte b = Convert.ToByte(entity.Attributes["rgb_color"][2]);

                return new RGB(r, g, b);
            }
            else if (entity.Attributes.ContainsKey("color_temp"))
            {
                int colorTemperature = Convert.ToInt32(entity.Attributes["color_temp"]);
                return ColorConverter.MiredToRGB(colorTemperature);
            }
            else
            {
                // Default to 2700 Kelvin
                return ColorConverter.MiredToRGB(370);
            }
        }
    }
}
