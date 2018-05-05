using Hashboard;
using System.Collections.Generic;
using Windows.UI.Xaml;

namespace HashBoard
{
    public class PanelData
    {
        public PanelTouchHandler TapHandler { get; set; }

        public PanelTouchHandler TapAndHoldHandler { get; set; }

        public Entity Entity { get; set; }

        public IEnumerable<Entity> ChildrenEntities { get; set; }

        public static PanelData GetPanelData(object obj) { return (PanelData)((FrameworkElement)obj).Tag; }
    }
}
