using System;
using System.Collections.Generic;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace HashBoard
{
    public class DateTimePanelBuilder : PanelBuilderBase
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

            DateTime dateTime = Convert.ToDateTime(entity.State);

            TextBlock textBlock = new TextBlock
            {
                Foreground = FontColorBrush,
                FontWeight = FontWeights.Bold,
                FontSize = FontSize ?? base.FontSize,
                Text = dateTime.ToString("h:mm tt") + "\n\n" + dateTime.ToLongDateString(),
                TextWrapping = TextWrapping.Wrap,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            grid.Children.Add(textBlock);

            return grid;
        }

        protected override Panel CreateGroupPanel(Entity entity, IEnumerable<Entity> childrenEntities, int width, int height)
        {
            return null;
        }
    }
}
