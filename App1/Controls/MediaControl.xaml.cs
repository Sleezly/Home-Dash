using HashBoard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
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

        private CancellationTokenSource cancellationTokenSource;

        public MediaControl(Entity entity)
        {
            this.InitializeComponent();

            PanelEntity = entity;

            if (PanelEntity.Attributes.ContainsKey("volume_level"))
            {
                Line scobblerLine = this.FindName("ScobblerLine") as Line;
                Line scobblerProgress1 = this.FindName("ScobblerProgress1") as Line;
                Line scobblerProgress2 = this.FindName("ScobblerProgress2") as Line;

                double percentagePrevious = Convert.ToInt16(10 * scobblerProgress1.X2 / scobblerLine.X2) / 10.0;

                scobblerProgress1.X2 = scobblerLine.X2 * Convert.ToDouble(PanelEntity.Attributes["volume_level"]);
                scobblerProgress2.X1 = scobblerProgress1.X2 - scobblerProgress2.StrokeThickness;
            }

            UpdateUI();

            cancellationTokenSource = new CancellationTokenSource();

            ThreadPoolTimer timer = ThreadPoolTimer.CreatePeriodicTimer(async (t) =>
            {
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}");

                PanelEntity = await WebRequests.GetData<Entity>(MainPage.hostname, $"api/states/{PanelEntity.EntityId}", MainPage.apiPassword);

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI();

                    if (this.Visibility == Visibility.Collapsed)
                    {
                        t.Cancel();
                    }
                });

            }, TimeSpan.FromSeconds(1));
        }

        private void UpdateUI()
        {
            if (PanelEntity.Attributes.ContainsKey("friendly_name"))
            {
                TextBlock textBlock = FindName("DeviceText") as TextBlock;
                textBlock.Text = PanelEntity.Attributes["friendly_name"];
            }

            if (PanelEntity.Attributes.ContainsKey("media_artist"))
            {
                TextBlock textBlock = FindName("ArtistText") as TextBlock;
                textBlock.Text = PanelEntity.Attributes["media_artist"];
            }

            // Spotify only has "media_title"
            if (PanelEntity.Attributes.ContainsKey("media_title"))
            {
                TextBlock textBlock = FindName("TrackText") as TextBlock;
                textBlock.Text = PanelEntity.Attributes["media_title"];
            }

            // Bose has "media_track" and "media_title" but we just want the track name so do this after "media_title"
            if (PanelEntity.Attributes.ContainsKey("media_track"))
            {
                TextBlock textBlock = FindName("TrackText") as TextBlock;
                textBlock.Text = PanelEntity.Attributes["media_track"];
            }

            if (PanelEntity.Attributes.ContainsKey("entity_picture"))
            {
                Image imageMedia = this.FindName("MediaImage") as Image;

                if (!string.Equals(PanelEntity.Attributes["entity_picture"], imageMedia.Tag?.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    imageMedia.Tag = PanelEntity.Attributes["entity_picture"];
                    imageMedia.Source = Imaging.LoadImageSource($"{MainPage.hostname}{PanelEntity.Attributes["entity_picture"]}");
                }
            }

            if (string.Equals(PanelEntity.State, "playing", StringComparison.InvariantCultureIgnoreCase))
            {
                BitmapIcon bitmapIcon = FindName("ButtonPlay") as BitmapIcon;

                if (!string.Equals(bitmapIcon.Tag?.ToString(), "media_pause", StringComparison.InvariantCultureIgnoreCase))
                {
                    bitmapIcon.UriSource = new Uri($"ms-appx:///Assets/media-pause.png");
                    bitmapIcon.Tag = "media_pause";
                }
            }
            else
            {
                BitmapIcon bitmapIcon = FindName("ButtonPlay") as BitmapIcon;

                if (!string.Equals(bitmapIcon.Tag?.ToString(), "media_play", StringComparison.InvariantCultureIgnoreCase))
                {
                    bitmapIcon.UriSource = new Uri($"ms-appx:///Assets/media-play.png");
                    bitmapIcon.Tag = "media_play";
                }
            }
        }

        private void BitmapIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;

            bitmapIcon.Foreground = new SolidColorBrush(Colors.White);
            switch (bitmapIcon.Tag.ToString())
            {
                case "volume_down":
                    ChangeScrobblerVolume(ScrobblerVolume.Down);
                    break;

                case "volume_up":
                    ChangeScrobblerVolume(ScrobblerVolume.Up);
                    break;

                //case "media_pause":
                //    WebRequests.SendAction(PanelEntity.EntityId, bitmapIcon.Tag.ToString());

                //    bitmapIcon.UriSource = new Uri($"ms-appx:///Assets/media-play.png");
                //    bitmapIcon.Tag = "media_play";
                //    break;

                //case "media_play":
                //    WebRequests.SendAction(PanelEntity.EntityId, bitmapIcon.Tag.ToString());

                //    bitmapIcon.UriSource = new Uri($"ms-appx:///Assets/media-pause.png");
                //    bitmapIcon.Tag = "media_pause";
                //    break;

                default:
                    WebRequests.SendAction(PanelEntity.EntityId, bitmapIcon.Tag.ToString());
                    break;
            }
        }

        private void ButtonPrevious_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = new SolidColorBrush(Colors.DarkGray);
        }
        private void ButtonPrevious_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            BitmapIcon bitmapIcon = sender as BitmapIcon;
            bitmapIcon.Foreground = new SolidColorBrush(Colors.White);
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

            //GetLatestEntityStateWithDelay(TimeSpan.FromSeconds(3));
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

                scobblerProgress1.X2 = scobblerLine.X2 * percentageNew;
                scobblerProgress2.X1 = scobblerProgress1.X2 - scobblerProgress2.StrokeThickness;

                Dictionary<string, string> serviceData = new Dictionary<string, string>()
                {
                    { "entity_id", PanelEntity.EntityId },
                    { "volume_level", percentageNew.ToString() }
                };

                WebRequests.SendAction(PanelEntity.EntityId.Split('.')[0], "volume_set", serviceData);
            }
        }
        
    }
}
