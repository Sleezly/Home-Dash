using Hashboard;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace HashBoard
{
    public abstract class PanelBuilderBase
    {
        private const int PanelSize = 170;

        protected const int PanelPadding = 5;

        protected const int PanelMargins = 2;

        public const double DefaultOpacity = 0.8;

        public const double PressedOpacity = 0.4;

        public const double StateIsOffOpacity = 0.3;

        protected Color NoninteractiveBrushColor = Colors.Black;

        protected SolidColorBrush FontColorBrush = new SolidColorBrush(Colors.White);

        protected int FontSize = 18;

        public enum EntitySize { Narrow, Condensed, Normal, Wide };

        public EntitySize Size { get; set; } = EntitySize.Normal;

        public PanelTouchHandler TapHandler { get; set; }

        public PanelTouchHandler TapAndHoldHandler { get; set; }

        public TappedEventHandler TapEventHandler { private get; set; } = null;

        public HoldingEventHandler HoldEventHandler { private get; set; } = null;

        public PointerEventHandler PressedEventHandler { private get; set; } = null;

        public PointerEventHandler ReleasedEventHandler { private get; set; } = null;

        public string EntityIdStartsWith { get; set; }

        protected abstract Panel CreateSinglePanel(Entity entity, int width, int height);

        protected abstract Panel CreateGroupPanel(Entity entity, IEnumerable<Entity> childrenEntities, int width, int height);
        
        public Panel CreatePanel(Entity entity)
        {
            Panel panel = CreateSinglePanel(entity, Width(PanelSize), PanelSize);

            if (panel != null)
            {
                SetPanelAttributes(entity, panel, null);
            }

            return panel;
        }

        public Panel CreateGroupPanel(Entity entity, IEnumerable<Entity> childrenEntities)
        {
            Panel panel = CreateGroupPanel(entity, childrenEntities, Width(PanelSize), PanelSize);

            if (panel != null)
            {
                SetPanelAttributes(entity, panel, childrenEntities);
            }

            return panel;
        }

        private void SetPanelAttributes(Entity entity, Panel panel, IEnumerable<Entity> childrenEntities)
        {
            panel.Margin = new Thickness(PanelPadding);

            panel.Tapped += this?.TapEventHandler;
            panel.PointerPressed += this?.PressedEventHandler;
            panel.PointerExited += this?.ReleasedEventHandler;
            panel.PointerReleased += this?.ReleasedEventHandler;
            panel.PointerCaptureLost += this?.ReleasedEventHandler;
            panel.Holding += this?.HoldEventHandler;
            panel.IsHoldingEnabled = (this.HoldEventHandler != null);

            if (panel.Background == null)
            {
                if (TapEventHandler == null)
                {
                    panel.Background = new SolidColorBrush(NoninteractiveBrushColor);
                }
                else
                {
                    panel.Background = ThemeControl.AccentColorBrush;
                }
            }

            if (entity.IsInOffState())
            {
                panel.Background.Opacity = StateIsOffOpacity;
            }
            else
            {
                panel.Background.Opacity = DefaultOpacity;
            }

            panel.Tag = new PanelData()
            {
                Entity = entity,
                ChildrenEntities = childrenEntities,
                TapHandler = this.TapHandler,
                TapAndHoldHandler = this.TapAndHoldHandler,
            };
        }

        private int Width(int size)
        {
            switch (Size)
            { 
                case EntitySize.Narrow:
                    return size / 2 - PanelPadding;

                case EntitySize.Condensed:
                    return size / 3 * 2 - PanelPadding / 3 * 2;

                case EntitySize.Wide:
                    return size * 2 + PanelPadding * 2;
                
                default:
                    return size;
            }
        }
    }
}
