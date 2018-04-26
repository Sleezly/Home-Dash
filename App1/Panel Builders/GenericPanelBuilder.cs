using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace HashBoard
{
    public class GenericPanelBuilder : PanelBuilderBase
    {
        public string ValueTextFromAttributeOverride { get; set; }

        protected override Panel CreateSinglePanel(Entity entity, int width, int height)
        {
            Grid grid = new Grid();
            grid.Width = width;
            grid.Height = height;

            TextBlock textName = new TextBlock();
            textName.Text = entity.Attributes["friendly_name"];
            textName.FontSize = FontSize;
            textName.TextWrapping = TextWrapping.Wrap;
            textName.HorizontalAlignment = HorizontalAlignment.Center;
            textName.VerticalAlignment = VerticalAlignment.Top;
            textName.Foreground = FontColorBrush;

            TextBlock textBlock = new TextBlock();
            textBlock.Foreground = FontColorBrush;
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.FontSize = FontSize;
            textBlock.Text = string.IsNullOrEmpty(ValueTextFromAttributeOverride) ? entity.State :
                Convert.ToString(entity.Attributes[ValueTextFromAttributeOverride]);
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;

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
