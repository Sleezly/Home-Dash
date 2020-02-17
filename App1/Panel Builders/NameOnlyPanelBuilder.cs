using Hashboard;
using System.Collections.Generic;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace HashBoard
{
    public class NameOnlyPanelBuilder : PanelBuilderBase
    {
        public new int? FontSize { get; set; } 

        protected override Panel CreateSinglePanel(Entity entity, int width, int height)
        {
            Grid grid = new Grid
            {
                Width = width,
                Height = height,
                Padding = new Thickness(PanelMargins)
            };

            TextBlock textBlock = new TextBlock
            {
                Foreground = FontColorBrush,
                FontWeight = FontWeights.Bold,
                FontSize = FontSize ?? base.FontSize,
                Text = entity.Name(),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (entity.Attributes.ContainsKey("local_assets_picture"))
            {
                grid.Background = Imaging.LoadAppXImageBrush(entity.Attributes["local_assets_picture"]);
            }

            grid.Children.Add(textBlock);

            return grid;
        }

        protected override Panel CreateGroupPanel(Entity entity, IEnumerable<Entity> childrenEntities, int width, int height)
        {
            return null;
        }
    }
}
