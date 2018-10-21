using System;
using System.Collections.Generic;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace HashBoard
{
    public class MediaPlayerPanelBuilder : PanelBuilderBase
    {
        public string ValueTextFromAttributeOverride { get; set; }

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
                Text = entity.Attributes["friendly_name"] ?? string.Empty,
                FontSize = FontSize,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = FontColorBrush
            };

            TextBlock textBlock = new TextBlock
            {
                Foreground = FontColorBrush,
                FontWeight = FontWeights.Bold,
                FontSize = FontSize,
                Text = string.IsNullOrEmpty(ValueTextFromAttributeOverride) ? entity.State :
                Convert.ToString(entity.Attributes[ValueTextFromAttributeOverride]),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (entity.Attributes.ContainsKey("unit_of_measurement"))
            {
                textBlock.Text += entity.Attributes["unit_of_measurement"];
            }

            if (entity.Attributes.ContainsKey("entity_picture"))
            {
                grid.Background = Imaging.LoadImageBrush2(entity.Attributes["entity_picture"]);

                textBlock.VerticalAlignment = VerticalAlignment.Bottom;
            }
            
            grid.Children.Add(textName);
            grid.Children.Add(textBlock);

            return grid;
        }

        protected override Panel CreateGroupPanel(Entity entity, IEnumerable<Entity> childrenEntities, int width, int height)
        {
            return null;
        }
    }
}
