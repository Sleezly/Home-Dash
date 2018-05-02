using Hashboard;
using System;
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
        private const int CellSize = 170;

        protected const int Padding = 6;

        public const double DefaultOpacity = 0.8;

        public const double PressedOpacity = 0.35;

        protected SolidColorBrush LightGrayBrushAlmostTransparent = new SolidColorBrush(Color.FromArgb(40, Colors.LightGray.R, Colors.LightGray.G, Colors.LightGray.B));

        protected SolidColorBrush LightGrayBrush = new SolidColorBrush(Color.FromArgb(Convert.ToByte(255 * DefaultOpacity), Colors.LightGray.R, Colors.LightGray.G, Colors.LightGray.B));

        protected SolidColorBrush NoninteractiveBrush = new SolidColorBrush(Color.FromArgb(Convert.ToByte(255 * DefaultOpacity), Colors.Black.R, Colors.Black.G, Colors.Black.B));

        protected SolidColorBrush FontColorBrush = new SolidColorBrush(Colors.White);

        protected int FontSize = 18;

        //public delegate bool CustomEntityRule(State entity);

        //public CustomEntityRule Rule { get; set; }

        public enum EntitySize { Narrow, Condensed, Normal, Wide };

        public EntitySize Size { get; set; } = EntitySize.Normal;

        public string TapEventAction { get; set; } = null;

        public string HoldEventAction { get; set; } = null;

        public TappedEventHandler TapEventHandler { private get; set; } = null;

        public HoldingEventHandler HoldEventHandler { private get; set; } = null;

        public PointerEventHandler PressedEventHandler { private get; set; } = null;

        public PointerEventHandler ReleasedEventHandler { private get; set; } = null;

        public string EntityIdStartsWith { get; set; }

        protected abstract Panel CreateSinglePanel(Entity entity, int width, int height);

        protected abstract Panel CreateGroupPanel(Entity entity, IEnumerable<Entity> childrenEntities, int width, int height);

        public Panel CreatePanel(Entity entity)
        {
            Panel panel = CreateSinglePanel(entity, Width(CellSize), CellSize);

            if (panel != null)
            {
                SetPanelAttributes(entity, panel, null);
            }

            return panel;
        }

        public Panel CreateGroupPanel(Entity entity, IEnumerable<Entity> childrenEntities)
        {
            Panel panel = CreateGroupPanel(entity, childrenEntities, Width(CellSize), CellSize);

            if (panel != null)
            {
                SetPanelAttributes(entity, panel, childrenEntities);
            }

            return panel;
        }

        private void SetPanelAttributes(Entity entity, Panel panel, IEnumerable<Entity> childrenEntities)
        {
            panel.Margin = new Thickness(Padding);

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
                    panel.Background = NoninteractiveBrush;
                }
                else
                {
                    panel.Background = ThemeControl.AccentColorBrush;
                    panel.Background.Opacity = DefaultOpacity;
                }
            }

            panel.Tag = new PanelData()
            {
                Entity = entity,
                ChildrenEntities = childrenEntities,
                ActionToInvokeOnTap = TapEventAction,
                ActionToInvokeOnHold = HoldEventAction,
            };
        }

        private int Width(int size)
        {
            switch (Size)
            { 
                case EntitySize.Narrow:
                    return size / 2 - Padding;

                case EntitySize.Condensed:
                    return size / 3 * 2 - Padding / 3 * 2;

                case EntitySize.Wide:
                    return size * 2 + Padding * 2;
                
                default:
                    return size;
            }
        }
    }
}
