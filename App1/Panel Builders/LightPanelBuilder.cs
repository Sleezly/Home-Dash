using Hashboard;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Collections.Generic;
using System.Linq;
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
            SolidColorBrush backgroundBrush = null;

            if (entity.State == "on")
            {
                backgroundBrush = entity.GetColor().CreateSolidColorBrush();
            }

            return CreatePanel(entity, width, height, backgroundBrush);
        }

        protected override Panel CreateGroupPanel(Entity entity, IEnumerable<Entity> childrenEntities, int width, int height)
        {
            IEnumerable<RGB> colorsRgb = childrenEntities.Where(x => !x.IsInOffState()).Select(x => x.GetColorRgb()).Where(x => x != null);
            IEnumerable<RGB> colorsTemperature = childrenEntities.Where(x => !x.IsInOffState()).Select(x => x.GetColorRgb()).Where(x => x != null);
            
            SolidColorBrush backgroundBrush = null;

            if (colorsRgb.Any())
            {
                backgroundBrush = RGB.Average(colorsRgb).CreateSolidColorBrush();
            }
            else if (colorsTemperature.Any())
            {
                backgroundBrush = RGB.Average(colorsTemperature).CreateSolidColorBrush();
            }
            else
            {
                backgroundBrush = entity.GetColorDefault().CreateSolidColorBrush();
            }

            return CreatePanel(entity, width, height, backgroundBrush);
        }

        private Panel CreatePanel(Entity entity, int width, int height, SolidColorBrush backgroundBrush)
        {
            DockPanel panel = new DockPanel
            {
                Width = width,
                Height = height,
                Padding = new Thickness(PanelMargins),
                Background = backgroundBrush
            };

            TextBlock textBlock = new TextBlock
            {
                Foreground = FontColorBrush,
                FontWeight = FontWeights.Bold,
                FontSize = FontSize,
                Text = entity.Name(),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(textBlock);

            return panel;
        }
    }
}
