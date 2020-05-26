using HashBoard;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using static HashBoard.Entity;

namespace Hashboard
{
    public sealed partial class CoverControl : UserControl
    {
        private Entity Entity;

        public CoverControl(Entity entity)
        {
            this.InitializeComponent();

            this.RequestedTheme = ThemeControl.GetApplicationTheme();

            Entity = entity;

            InitializeUI();
        }

        /// <summary>
        /// Respond to entity changes while the popup control is up.
        /// </summary>
        /// <param name="entity"></param>
        public void EntityUpdated(Entity entity, IEnumerable<Entity> childrenEntities)
        {
            Entity = entity;

            InitializeUI();
        }

        private void BitmapIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            WebRequests.SendAction(Entity.EntityId, bitmapIcon.Tag.ToString());

            SetButtonEnabledolor();
        }

        private void BitmapIcon_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = new SolidColorBrush(Colors.Gray);
        }

        private void BitmapIcon_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            SetButtonEnabledolor();
        }

        /// <summary>
        /// Sets the foreground button color.
        /// </summary>
        private void SetButtonEnabledolor()
        {
            BitmapIcon buttonUp = FindName("ButtonUp") as BitmapIcon;
            buttonUp.Foreground = Entity.Attributes["current_position"] == 100 ?
                buttonUp.Foreground = new SolidColorBrush(Colors.Gray) :
                buttonUp.Foreground = this.Foreground;

            BitmapIcon buttonDown = FindName("ButtonDown") as BitmapIcon;
            buttonDown.Foreground = Entity.Attributes["current_position"] == 0 ?
                buttonDown.Foreground = new SolidColorBrush(Colors.Gray) :
                buttonDown.Foreground = this.Foreground;

            BitmapIcon buttonStop = FindName("ButtonStop") as BitmapIcon;
            buttonStop.Foreground = this.Foreground;
        }

        /// <summary>
        /// Render all UI.
        /// </summary>
        private void InitializeUI()
        {
            if (Entity.Attributes.ContainsKey("friendly_name"))
            {
                TextBlock textBlock = FindName("DeviceText") as TextBlock;
                textBlock.Text = Entity.Name().ToUpper();
            }

            if (Entity.Attributes.ContainsKey("current_position"))
            {
                TextBlock textBlock = FindName("DevicePosition") as TextBlock;
                textBlock.Text = $"{Entity.Attributes["current_position"]}%";
            }

            TextBlock textState = FindName("DeviceState") as TextBlock;
            textState.Text = Entity.State;

            SetButtonEnabledolor();
        }
    }
}
