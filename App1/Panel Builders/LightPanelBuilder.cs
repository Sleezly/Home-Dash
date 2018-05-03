using Hashboard;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Collections.Generic;
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
                backgroundBrush = entity.GetColor().CreateSolidColorBrush();
            }
            else
            {
                backgroundBrush = new SolidColorBrush(NoninteractiveBrushColor);
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
                    rgbList.Add(childEntity.GetColor());
                }
            }

            SolidColorBrush backgroundBrush;
            if (rgbList.Count > 0)
            {
                backgroundBrush = RGB.Average(rgbList).CreateSolidColorBrush();
            }
            else
            {
                backgroundBrush = new SolidColorBrush(NoninteractiveBrushColor);
            }

            return CreatePanel(entity, width, height, backgroundBrush);
        }

        private Panel CreatePanel(Entity entity, int width, int height, SolidColorBrush backgroundBrush)
        {
            DockPanel panel = new DockPanel();
            panel.Width = width;
            panel.Height = height;
            panel.Padding = new Thickness(PanelMargins);
            panel.Background = backgroundBrush;

            TextBlock textBlock = new TextBlock();
            textBlock.Foreground = FontColorBrush;
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.FontSize = FontSize;
            textBlock.Text = entity.Attributes["friendly_name"] ?? string.Empty;
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.FontSize = FontSize;

            panel.Children.Add(textBlock);

            return panel;
        }
    }
}
