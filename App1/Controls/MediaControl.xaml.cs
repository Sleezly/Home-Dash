using HashBoard;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using static HashBoard.Entity;

namespace Hashboard
{
    public partial class MediaControl : UserControl
    {
        private const int VolumeIncrementAdjustments = 10;

        private Entity PanelEntity;
        
        private readonly SolidColorBrush DisabledButtonBrush = new SolidColorBrush(Colors.Gray);

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
            // Device Text
            TextBlock textDeviceText = FindName("DeviceText") as TextBlock;
            if (PanelEntity.Attributes.ContainsKey("friendly_name"))
            {
                textDeviceText.Text = PanelEntity.Attributes["friendly_name"].ToUpper();
            }
            else
            {
                textDeviceText.Text = string.Empty;
            }

            // Artist Text
            TextBlock textArtistText = FindName("ArtistText") as TextBlock;
            if (PanelEntity.Attributes.ContainsKey("media_artist"))
            {
                textArtistText.Text = PanelEntity.Attributes["media_artist"].ToUpper();
            }
            else
            {
                textArtistText.Text = string.Empty;
            }

            // Spotify only has "media_title"
            TextBlock textTrackText = FindName("TrackText") as TextBlock;
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

            // Media Picture
            Image imageMedia = this.FindName("MediaImage") as Image;
            if (PanelEntity.Attributes.ContainsKey("entity_picture"))
            {
                if (!string.Equals(PanelEntity.Attributes["entity_picture"], imageMedia.Tag?.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    imageMedia.Tag = PanelEntity.Attributes["entity_picture"];
                    imageMedia.Source = Imaging.LoadImageSource(PanelEntity.Attributes["entity_picture"]);
                }
            }
            else
            {
                imageMedia.Source = null;
            }

            // Volume
            BitmapIcon buttonVolumeUp = this.FindName("ButtonVolumeUp") as BitmapIcon;
            BitmapIcon buttonVolumeDown = this.FindName("ButtonVolumeDown") as BitmapIcon;

            Line scobblerLine = this.FindName("ScobblerLine") as Line;
            Line scobblerProgress1 = this.FindName("ScobblerProgress1") as Line;
            Line scobblerProgress2 = this.FindName("ScobblerProgress2") as Line;

            if (PanelEntity.HasSupportedFeatures((uint)MediaPlatformSupportedFeatures.VolumeSet) &&
                PanelEntity.Attributes.ContainsKey("volume_level"))
            {
                double percentagePrevious = Convert.ToInt16(15 * scobblerProgress1.X2 / scobblerLine.X2) / 15.0;

                scobblerProgress1.X2 = scobblerLine.X2 * Convert.ToDouble(PanelEntity.Attributes["volume_level"]);
                scobblerProgress2.X1 = scobblerProgress1.X2 - scobblerProgress2.StrokeThickness;

                scobblerProgress1.Visibility = Visibility.Visible;
                scobblerProgress2.Stroke = this.Foreground;

                EnableButton(buttonVolumeUp, true);
                EnableButton(buttonVolumeDown, true);

                buttonVolumeUp.Foreground = this.Foreground;
                buttonVolumeDown.Foreground = this.Foreground;
            }
            else
            {
                // Not supported
                scobblerProgress2.X1 = 0;

                scobblerProgress1.Visibility = Visibility.Collapsed;
                scobblerProgress2.Stroke = DisabledButtonBrush;

                scobblerLine.Tapped -= ScobblerLine_Tapped;
                scobblerLine.PointerReleased -= Grid_PointerReleased;

                EnableButton(buttonVolumeUp, false);
                EnableButton(buttonVolumeDown, false);

                buttonVolumeUp.Foreground = DisabledButtonBrush;
                buttonVolumeDown.Foreground = DisabledButtonBrush;
            }

            // Play and Pause Toggle
            BitmapIcon bitmapIconPlay = FindName("ButtonPlay") as BitmapIcon;
            Ellipse ellipsePlay = FindName("EllipsePlay") as Ellipse;

            if (PanelEntity.HasSupportedFeatures((uint)MediaPlatformSupportedFeatures.Pause | (uint)MediaPlatformSupportedFeatures.Play))
            {
                Uri uriSource;

                if (string.Equals(PanelEntity.State, "playing", StringComparison.InvariantCultureIgnoreCase))
                {
                    uriSource = new Uri($"ms-appx:///Assets/media/media-pause.png");
                }
                else
                {
                    uriSource = new Uri($"ms-appx:///Assets/media/media-play.png");
                }

                // By checking URI source before setting it we ensure no image flickering during content refreshes (e.g. multiple volume sets)
                if (uriSource != bitmapIconPlay.UriSource)
                {
                    bitmapIconPlay.UriSource = uriSource;
                }

                ellipsePlay.Stroke = this.Foreground;
                bitmapIconPlay.Foreground = this.Foreground;

                EnableEllipse(ellipsePlay, true);
                EnableButton(bitmapIconPlay, true);
            }
            else
            {
                // Not supported
                ellipsePlay.Stroke = DisabledButtonBrush;
                bitmapIconPlay.Foreground = DisabledButtonBrush;

                EnableEllipse(ellipsePlay, false);
                EnableButton(bitmapIconPlay, false);
            }

            // Previous
            BitmapIcon previousButton = this.FindName("ButtonPrevious") as BitmapIcon;
            if (PanelEntity.HasSupportedFeatures((uint)MediaPlatformSupportedFeatures.PreviousTack))
            {
                previousButton.Foreground = this.Foreground;
                EnableButton(previousButton, true);
            }
            else
            {
                // Not supported
                previousButton.Foreground = DisabledButtonBrush;
                EnableButton(previousButton, false);
            }

            // Next
            BitmapIcon nextButton = this.FindName("ButtonNext") as BitmapIcon;
            if (PanelEntity.HasSupportedFeatures((uint)MediaPlatformSupportedFeatures.NextTrack))
            {
                nextButton.Foreground = this.Foreground;
                EnableButton(nextButton, true);
            }
            else
            {
                // Not supported
                nextButton.Foreground = DisabledButtonBrush;
                EnableButton(nextButton, false);
            }

            // Power Toggle
            BitmapIcon bitmapIconPower = FindName("ButtonPower") as BitmapIcon;
            if (PanelEntity.HasSupportedFeatures((uint)MediaPlatformSupportedFeatures.TurnOff | (uint)MediaPlatformSupportedFeatures.TurnOn))
            {
                bitmapIconPower.Foreground = this.Foreground;
                EnableButton(bitmapIconPower, true);
            }
            else
            {
                // Not supported
                bitmapIconPower.Foreground = DisabledButtonBrush;
                EnableButton(bitmapIconPower, false);
            }

            // Source Select
            ComboBox comboBox = this.FindName("SourceComboBox") as ComboBox;
            if (PanelEntity.HasSupportedFeatures((uint)MediaPlatformSupportedFeatures.SelectSource) && 
                PanelEntity.Attributes.ContainsKey("source_list"))
            {
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

                comboBox.IsEnabled = true;
                SourceSelectTextBlock.Foreground = this.Foreground;
            }
            else
            {
                comboBox.IsEnabled = false;
                SourceSelectTextBlock.Foreground = DisabledButtonBrush;
            }
        }

        private void EnableEllipse(Ellipse ellipse, bool enabled)
        {
            if (enabled)
            {
                // To ensure we don't add event handlers multiple times, unregister events first
                EnableEllipse(ellipse, false);

                ellipse.Tapped += EllipsePlay_Tapped;
                ellipse.PointerPressed += EllipsePlay_PointerPressed;
                ellipse.PointerExited += EllipsePlay_PointerExited;
            }
            else
            {
                ellipse.Tapped -= EllipsePlay_Tapped;
                ellipse.PointerPressed -= EllipsePlay_PointerPressed;
                ellipse.PointerExited -= EllipsePlay_PointerExited;
            }
        }

        private void EnableButton(BitmapIcon bitmapIconButton, bool enabled)
        {
            if (enabled)
            {
                // To ensure we don't add event handlers multiple times, unregister events first
                EnableButton(bitmapIconButton, false);

                bitmapIconButton.Tapped += BitmapIcon_Tapped;
                bitmapIconButton.PointerPressed += BitmapIcon_PointerPressed;
                bitmapIconButton.PointerExited += BitmapIcon_PointerExited;
            }
            else
            {
                bitmapIconButton.Tapped -= BitmapIcon_Tapped;
                bitmapIconButton.PointerPressed -= BitmapIcon_PointerPressed;
                bitmapIconButton.PointerExited -= BitmapIcon_PointerExited;
            }
        }

        private void BitmapIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = this.Foreground;

            // Adjust the ellipse for the Play button as well
            if (bitmapIcon.Name == "ButtonPlay")
            {
                Ellipse ellipsePlay = FindName("EllipsePlay") as Ellipse;
                ellipsePlay.Stroke = bitmapIcon.Foreground;
            }

            switch (bitmapIcon.Tag.ToString())
            {
                case "volume_down":
                    SendVolumeLevel(PanelEntity.Attributes["volume_level"] - 0.1);
                    break;

                case "volume_up":
                    SendVolumeLevel(PanelEntity.Attributes["volume_level"] + 0.1);
                    break;

                // power (toggle)
                // play (media_play_pause)
                // pause (media_play_pause)
                // shuffle (media_shuffle_set)
                default:
                    WebRequests.SendAction(PanelEntity.EntityId, bitmapIcon.Tag.ToString());
                    break;
            }
        }

        private void BitmapIcon_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = new SolidColorBrush(Colors.Gray);

            // Adjust the ellipse for the Play button as well
            if (bitmapIcon.Name == "ButtonPlay")
            {
                Ellipse ellipsePlay = FindName("EllipsePlay") as Ellipse;
                ellipsePlay.Stroke = bitmapIcon.Foreground;
            }
        }

        private void BitmapIcon_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = this.Foreground;

            // Adjust the ellipse for the Play button as well
            if (bitmapIcon.Name == "ButtonPlay")
            {
                Ellipse ellipsePlay = FindName("EllipsePlay") as Ellipse;
                ellipsePlay.Stroke = bitmapIcon.Foreground;
            }
        }

        private void EllipsePlay_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BitmapIcon bitmapIconPlay = FindName("ButtonPlay") as BitmapIcon;
            BitmapIcon_Tapped(bitmapIconPlay, e);
        }

        private void EllipsePlay_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIconPlay = FindName("ButtonPlay") as BitmapIcon;
            Ellipse ellipse = sender as Ellipse;

            bitmapIconPlay.Foreground = this.Foreground;
            ellipse.Stroke = bitmapIconPlay.Foreground;
        }

        private void EllipsePlay_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIconPlay = FindName("ButtonPlay") as BitmapIcon;
            Ellipse ellipse = sender as Ellipse;

            bitmapIconPlay.Foreground = this.Foreground;
            ellipse.Stroke = bitmapIconPlay.Foreground;
        }

        private void ScobblerLine_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SetScobblerPercentage(sender, e.GetPosition(sender as UIElement));
        }

        private void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            SetScobblerPercentage(sender, e.GetCurrentPoint(sender as UIElement).Position);
        }

        private void SetScobblerPercentage(object sender, Point userTapPooint)
        {
            Line scobblerLine = this.FindName("ScobblerLine") as Line;
            Line scobblerProgress1 = this.FindName("ScobblerProgress1") as Line;
            Line scobblerProgress2 = this.FindName("ScobblerProgress2") as Line;
            
            // Allow for 0.10 volume percentage increments
            double percentageNew = Convert.ToInt16(VolumeIncrementAdjustments * userTapPooint.X / scobblerLine.X2) / (double)VolumeIncrementAdjustments;

            SendVolumeLevel(percentageNew);
        }

        private void SendVolumeLevel(double volumeLevel)
        {
            volumeLevel = Math.Min(1.0, volumeLevel);
            volumeLevel = Math.Max(0.0, volumeLevel);

            Dictionary<string, string> serviceData = new Dictionary<string, string>()
            {
                { "entity_id", PanelEntity.EntityId },
                { "volume_level", volumeLevel.ToString() }
            };

            WebRequests.SendAction(PanelEntity.EntityId.Split('.')[0], "volume_set", serviceData);
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
