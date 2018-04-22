using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace HashBoard
{
    public class PanelData
    {
        public Brush BackgroundBrush { get; set; }

        public Entity Entity { get; set; } 

        public string ServiceToInvokeOnTap { get; set; }

        public IEnumerable<Entity> ChildrenEntities { get; set; }

        public DateTime LastDashboardtaUpdate { get; set; }

        public string PopupUserControl { get; set; }

        public static PanelData GetPanelData(object obj) { return (PanelData)((FrameworkElement)obj).Tag; }
    }
}
