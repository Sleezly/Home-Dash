using HashBoard;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Hashboard
{
    public partial class MediaControl : UserControl
    {
        private Entity PanelEntity;

        public MediaControl(Entity entity)
        {
            this.InitializeComponent();

            this.RequestedTheme = ThemeControl.GetApplicationTheme();

            PanelEntity = entity;

            UpdateUI();
        }

        /// <summary>
        /// Respond to entity changes while the popup control is up.
        /// </summary>
        /// <param name="entity"></param>
        public void EntityUpdated(Entity entity, IEnumerable<Entity> childrenEntities)
        {
            PanelEntity = entity;

            UpdateUI();
        }

        private void UpdateUI()
        {
            TextBlock textDeviceText = FindName("DeviceText") as TextBlock;
            TextBlock textArtistText = FindName("ArtistText") as TextBlock;
            TextBlock textTrackText = FindName("TrackText") as TextBlock;
            BitmapIcon bitmapIcon = FindName("ButtonPlay") as BitmapIcon;

            if (PanelEntity.Attributes.ContainsKey("friendly_name"))
            {
                textDeviceText.Text = PanelEntity.Attributes["friendly_name"].ToUpper();
            }
            else
            {
                textDeviceText.Text = string.Empty;
            }

            if (PanelEntity.Attributes.ContainsKey("media_artist"))
            {
                textArtistText.Text = PanelEntity.Attributes["media_artist"].ToUpper();
            }
            else
            {
                textArtistText.Text = string.Empty;
            }

            // Spotify only has "media_title"
            if (PanelEntity.Attributes.ContainsKey("media_title"))
            {
                textTrackText.Text = PanelEntity.Attributes["media_title"].ToUpper();
            }
            else
            {
                textTrackText.Text = string.Empty;
            }

            // Bose has "media_track" and "media_title" but we just want the track name so do this after "media_title"
            if (PanelEntity.Attributes.ContainsKey("media_track"))
            {
                textTrackText.Text = PanelEntity.Attributes["media_track"].ToUpper();
            }

            if (PanelEntity.Attributes.ContainsKey("entity_picture"))
            {
                Image imageMedia = this.FindName("MediaImage") as Image;

                if (!string.Equals(PanelEntity.Attributes["entity_picture"], imageMedia.Tag?.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    imageMedia.Tag = PanelEntity.Attributes["entity_picture"];
                    imageMedia.Source = Imaging.LoadImageSource(PanelEntity.Attributes["entity_picture"]);
                }
            }

            // Set the volume sliders
            if (PanelEntity.Attributes.ContainsKey("volume_level"))
            {
                Line scobblerLine = this.FindName("ScobblerLine") as Line;
                Line scobblerProgress1 = this.FindName("ScobblerProgress1") as Line;
                Line scobblerProgress2 = this.FindName("ScobblerProgress2") as Line;

                double percentagePrevious = Convert.ToInt16(10 * scobblerProgress1.X2 / scobblerLine.X2) / 10.0;

                scobblerProgress1.X2 = scobblerLine.X2 * Convert.ToDouble(PanelEntity.Attributes["volume_level"]);
                scobblerProgress2.X1 = scobblerProgress1.X2 - scobblerProgress2.StrokeThickness;
            }

            // Play / Pause / Power button to render icon and dictate toggle-action given current state
            if (string.Equals(PanelEntity.State, "playing", StringComparison.InvariantCultureIgnoreCase))
            {
                bitmapIcon.Tag = "media_play_pause";
                bitmapIcon.UriSource = new Uri($"ms-appx:///Assets/media-pause.png");
            }
            else if (string.Equals(PanelEntity.State, "paused", StringComparison.InvariantCultureIgnoreCase))
            {
                bitmapIcon.UriSource = new Uri($"ms-appx:///Assets/media-play.png");
                bitmapIcon.Tag = "media_play_pause";
            }
            else
            {
                bitmapIcon.UriSource = new Uri($"ms-appx:///Assets/power.png");
                bitmapIcon.Tag = "toggle";
            }

            // Source Select
            if (PanelEntity.Attributes.ContainsKey("source_list"))
            {
                ComboBox comboBox = this.FindName("SourceComboBox") as ComboBox;

                // Ensure clean-slate to handle reentry from due to entity update while the control is open
                comboBox.SelectionChanged -= SourceComboBox_SelectionChanged;
                comboBox.Items.Clear();

                foreach (string item in PanelEntity.Attributes["source_list"])
                {
                    comboBox.Items.Add(item);
                }

                if (PanelEntity.Attributes.ContainsKey("source"))
                {
                    comboBox.SelectedItem = Convert.ToString(PanelEntity.Attributes["source"]);
                    comboBox.SelectionChanged += SourceComboBox_SelectionChanged;
                }
            }
            else
            {
                // No need to show source-select if there's nothing to show
                this.Height = 530;

                ComboBox comboBox = this.FindName("SourceComboBox") as ComboBox;
                comboBox.Visibility = Visibility.Collapsed;
            }
        }

        private void BitmapIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = this.Foreground;

            switch (bitmapIcon.Tag.ToString())
            {
                case "volume_down":
                    ChangeScrobblerVolume(ScrobblerVolume.Down);
                    break;

                case "volume_up":
                    ChangeScrobblerVolume(ScrobblerVolume.Up);
                    break;

                // power (toggle)
                // play (media_play_pause)
                // pause (media_play_pause)
                default:
                    WebRequests.SendAction(PanelEntity.EntityId, bitmapIcon.Tag.ToString());
                    break;
            }
        }

        private void ButtonPrevious_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = new SolidColorBrush(Colors.Gray);
        }

        private void ButtonPrevious_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = this.Foreground;
        }

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            SetScobblerPercentage(sender, e);
        }

        private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                SetScobblerPercentage(sender, e);
            }
        }

        private void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            SetScobblerPercentage(sender, e);
        }

        private void SetScobblerPercentage(object sender, PointerRoutedEventArgs e)
        {
            Line scobblerLine = this.FindName("ScobblerLine") as Line;
            Line scobblerProgress1 = this.FindName("ScobblerProgress1") as Line;
            Line scobblerProgress2 = this.FindName("ScobblerProgress2") as Line;

            PointerPoint pt = e.GetCurrentPoint(sender as UIElement);

            double percentageNew = Convert.ToInt16(10 * pt.Position.X / scobblerLine.X2) / 10.0;

            SetScrobblerVolume(percentageNew);
        }

        private enum ScrobblerVolume { Up, Down };
        private void ChangeScrobblerVolume(ScrobblerVolume direction)
        {
            Line scobblerLine = this.FindName("ScobblerLine") as Line;
            Line scobblerProgress1 = this.FindName("ScobblerProgress1") as Line;

            double percentagePrevious = Convert.ToInt16(10 * scobblerProgress1.X2 / scobblerLine.X2) / 10.0;
            double percentageNew = percentagePrevious + (direction == ScrobblerVolume.Up ? 0.1 : -0.1);

            if (percentageNew > 1.0)
            {
                percentageNew = 1.0;
            }

            if (percentageNew < 0.0)
            {
                percentageNew = 0.0;
            }

            SetScrobblerVolume(percentageNew);
        }

        private void SetScrobblerVolume(double percentageNew)
        {
            Line scobblerLine = this.FindName("ScobblerLine") as Line;
            Line scobblerProgress1 = this.FindName("ScobblerProgress1") as Line;

            double percentagePrevious = Convert.ToInt16(10 * scobblerProgress1.X2 / scobblerLine.X2) / 10.0;

            if (percentageNew != percentagePrevious)
            {
                Line scobblerProgress2 = this.FindName("ScobblerProgress2") as Line;

                scobblerProgress1.X2 = (scobblerLine.X2 - scobblerProgress1.StrokeThickness) * percentageNew + scobblerProgress1.StrokeThickness;
                scobblerProgress2.X1 = scobblerProgress1.X2 - scobblerProgress2.StrokeThickness;

                Dictionary<string, string> serviceData = new Dictionary<string, string>()
                {
                    { "entity_id", PanelEntity.EntityId },
                    { "volume_level", percentageNew.ToString() }
                };

                WebRequests.SendAction(PanelEntity.EntityId.Split('.')[0], "volume_set", serviceData);
            }
        }

        private void SourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            Dictionary<string, string> serviceData = new Dictionary<string, string>()
            {
                { "entity_id", PanelEntity.EntityId },
                { "source", comboBox.SelectedItem.ToString() }
            };

            WebRequests.SendAction(PanelEntity.EntityId.Split('.')[0], "select_source", serviceData);
        }
    }
}
