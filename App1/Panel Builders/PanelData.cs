using System.Collections.Generic;
using Windows.UI.Xaml;

namespace HashBoard
{
    public class PanelData
    {
        public Entity Entity { get; set; } 

        public string ActionToInvokeOnTap { get; set; }

        public string ActionToInvokeOnHold { get; set; }

        public IEnumerable<Entity> ChildrenEntities { get; set; }

        public static PanelData GetPanelData(object obj) { return (PanelData)((FrameworkElement)obj).Tag; }
    }
}
