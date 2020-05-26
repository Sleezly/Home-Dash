using Hashboard;
using System;
using System.Collections.Generic;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace HashBoard
{
    public class CoverPanelBuilder : PanelBuilderBase
    {
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
                FontSize = FontSize,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = FontColorBrush
            };

            double? currentPosition = entity.Attributes.ContainsKey("current_position") ?
                entity.Attributes["current_position"] :
                null;

            TextBlock textBlock = new TextBlock
            {
                Foreground = FontColorBrush,
                FontWeight = FontWeights.Bold,
                FontSize = FontSize,
                Text = currentPosition.HasValue && currentPosition.Value != 0 && currentPosition.Value != 100 ?
                    $"{currentPosition}%" :
                    entity.State,
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
