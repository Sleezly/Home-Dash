using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.System.Display;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static HashBoard.PanelBuilderBase;

namespace HashBoard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public const string hostname = "hassio.local:8123";

        public const string apiPassword = "api_password=yavin333";

        private const int LightSensorReadingRateInMs = 5000;

        private DateTime MousePressStartTime;

        private List<PanelBuilderBase> CustomEntities;

        private CancellationTokenSource cancellationTokenSource;

        //private CancellationTokenSource cancellationTokenSourceContentDialog;

        // Pxoximity sensors
        //private ProximitySensor sensor;
        //private ProximitySensorDisplayOnOffController displayController;
        //private Windows.Devices.Enumeration.DeviceWatcher watcher;
        private DisplayRequest DisplayRequestSetting = new DisplayRequest();
        private BrightnessOverride BrightnessOverrideSetting;
        private LightSensor LightSensorSetting;
        private double PreviousDisplayBrightness;

        public MainPage()
        {
            this.InitializeComponent();

            LoadEntityHandler();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Keep the display in the On state while the app is running
            DisplayRequestSetting.RequestActive();

            //watcher = Windows.Devices.Enumeration.DeviceInformation.CreateWatcher(ProximitySensor.GetDeviceSelector());
            //watcher.Added += OnProximitySensorAdded;
            //watcher.Start();

            // Get capability to dim and brighten the display when needed
            BrightnessOverrideSetting = BrightnessOverride.GetForCurrentView();
            if (BrightnessOverrideSetting.IsSupported)
            {
                //brightnessOverride.SetBrightnessLevel(1.0, DisplayBrightnessOverrideOptions.None);
                BrightnessOverrideSetting.StartOverride();
            }

            // Register for ambient light value changes
            LightSensorSetting = LightSensor.GetDefault();
            if (LightSensorSetting != null)
            {
                LightSensorSetting.ReportInterval = LightSensorReadingRateInMs;
                LightSensorSetting.ReadingChanged += LightSensor_ReadingChanged;
            }

            await LoadFrame();
        }

        /// <summary>
        /// Ambient Light Sensor adjusts the brightness of the display. Less ambient light equates to a dimmer display.
        /// </summary>
        /// <param name="lightSensor"></param>
        /// <param name="e"></param>
        private async void LightSensor_ReadingChanged(LightSensor lightSensor, LightSensorReadingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                LightSensorReading lightSensorReading = lightSensor.GetCurrentReading();

                if (BrightnessOverrideSetting != null && BrightnessOverrideSetting.IsSupported)
                {
                    const double maximumAllowedBrightness = 0.5;
                    const double highestLuxValueBeforeFullBrightness = 25.0;

                    double brightness = Math.Min(lightSensorReading.IlluminanceInLux, highestLuxValueBeforeFullBrightness) / highestLuxValueBeforeFullBrightness * maximumAllowedBrightness;

                    if (PreviousDisplayBrightness != brightness)
                    {
                        Debug.WriteLine($"LightSensorReading: {lightSensorReading.IlluminanceInLux.ToString()}. Setting brightness to {brightness.ToString()}.");

                        BrightnessOverrideSetting.SetBrightnessLevel(brightness, DisplayBrightnessOverrideOptions.None);

                        PreviousDisplayBrightness = brightness;
                    }
                }
            });
        }

        /// <summary>
        /// Invoked when the device watcher finds a proximity sensor.
        /// </summary>
        /// <param name="sender">The device watcher</param>
        /// <param name="device">Device information for the proximity sensor that was found</param>
        //private async void OnProximitySensorAdded(Windows.Devices.Enumeration.DeviceWatcher sender, Windows.Devices.Enumeration.DeviceInformation device)
        //{
        //    if (null == sensor)
        //    {
        //        ProximitySensor foundSensor = ProximitySensor.FromId(device.Id);

        //        if (null != foundSensor)
        //        {
        //            sensor = foundSensor;

        //            displayController = sensor.CreateDisplayOnOffController();
        //        }
        //        else
        //        {
        //            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        //            {
        //                Debug.WriteLine("Could not get a proximity sensor from the device id");
        //            });
        //        }
        //    }
        //}

        /// <summary>
        /// Invoked immediately before the Page is unloaded and is no longer the current source of a parent Frame.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            //if (displayController != null)
            //{
            //    displayController.Dispose();
            //    displayController = null;
            //}
            DisplayRequestSetting.RequestRelease();

            base.OnNavigatingFrom(e);
        }

        private void LoadEntityHandler()
        {
            CustomEntities = new List<PanelBuilderBase>()
            {
                new DateTimePanelBuilder() {
                    EntityIdStartsWith = "sensor.date__time",
                    Size = EntitySize.Wide,
                    FontSize = 24 },

                new StateOnlyPanelBuilder() {
                    EntityIdStartsWith = "sensor.dark_sky_daily_summary",
                    Size = EntitySize.Wide },

                new StateOnlyPanelBuilder() {
                    EntityIdStartsWith = "sensor.dark_sky_temperature",
                    FontSize = 32 },

                new DarkSkyPanelbuilder() {
                    EntityIdStartsWith = "sensor.forecast_today" },

                new DarkSkyPanelbuilder() {
                    EntityIdStartsWith = "sensor.forecast_",
                    Size = EntitySize.Condensed },

                new GenericPanelBuilder() {
                    EntityIdStartsWith = "climate.",
                    ValueTextFromAttributeOverride = "current_temperature" },

                new MediaPlayerPanelBuilder() {
                    EntityIdStartsWith = "media_player.",
                    TapEventAction = "media_play_pause",
                    Size = EntitySize.Normal,
                    EntityPopupControl = nameof(Hashboard.MediaControl),
                    TapEventHandler = PanelElement_Tapped,
                    HoldEventHandler = PanelElement_Holding,
                    PressedEventHandler = PanelElement_PointerPressed,
                    ReleasedEventHandler = PanelElement_PointerExited},

                new LightPanelBuilder() {
                    EntityIdStartsWith = "light.",
                    TapEventAction = "toggle",
                    EntityPopupControl = nameof(Hashboard.LightControl),
                    TapEventHandler = PanelElement_Tapped,
                    HoldEventHandler = PanelElement_Holding,
                    PressedEventHandler = PanelElement_PointerPressed,
                    ReleasedEventHandler = PanelElement_PointerExited},
                
                new NameOnlyPanelBuilder() {
                    EntityIdStartsWith = "script.",
                    TapEventHandler = PanelElement_Tapped,
                    HoldEventHandler = PanelElement_Holding,
                    PressedEventHandler = PanelElement_PointerPressed,
                    ReleasedEventHandler = PanelElement_PointerExited},

                new GenericPanelBuilder() {
                    EntityIdStartsWith = "switch.",
                    TapEventAction = "toggle",
                    TapEventHandler = PanelElement_Tapped,
                    HoldEventHandler = PanelElement_Holding,
                    PressedEventHandler = PanelElement_PointerPressed,
                    ReleasedEventHandler = PanelElement_PointerExited},

                new GenericPanelBuilder() {
                    EntityIdStartsWith = "automation.",
                    TapEventAction = "activate",
                    TapEventHandler = PanelElement_Tapped,
                    HoldEventHandler = PanelElement_Holding,
                    PressedEventHandler = PanelElement_PointerPressed,
                    ReleasedEventHandler = PanelElement_PointerExited},

                new GenericPanelBuilder() { EntityIdStartsWith = string.Empty },
            };
        }

        /// <summary>
        /// Show the PopUp custom UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PanelElement_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e == null || e.HoldingState == Windows.UI.Input.HoldingState.Completed)
            {
                PanelData panelData = PanelData.GetPanelData(sender);

                if (panelData.PopupUserControl != null)
                {
                    UserControl popupContent;

                    switch (panelData.PopupUserControl)
                    {
                        case nameof(Hashboard.LightControl):
                            popupContent = new Hashboard.LightControl(panelData.Entity, panelData.ChildrenEntities);
                            break;

                        case nameof(Hashboard.MediaControl):
                            popupContent = new Hashboard.MediaControl(panelData.Entity);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (popupContent != null)
                    {
                        Popup popup = new Popup()
                        {
                            IsLightDismissEnabled = true,
                            LightDismissOverlayMode = LightDismissOverlayMode.On,
                            Child = popupContent,
                        };

                        popup.HorizontalOffset = Window.Current.Bounds.Width / 2 - 150;
                        popup.VerticalOffset = Window.Current.Bounds.Height / 2 - 250;

                        popup.Closed += (s, re) =>
                        {
                            popup.Child.Visibility = Visibility.Collapsed;
                        };

                        popup.IsOpen = true;
                    }
                }
            }
        }
        
        private void PanelElement_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DateTime now = DateTime.Now;

            if (MousePressStartTime + TimeSpan.FromSeconds(3) > now &&
                MousePressStartTime + TimeSpan.FromMilliseconds(300) < now)
            {
                PanelElement_Holding(sender, null);
            }
            else
            {
                PanelData panelData = PanelData.GetPanelData(sender);

                // Tap
                if (string.IsNullOrEmpty(panelData.ServiceToInvokeOnTap))
                {
                    SendPanelDataSimple((Panel)sender, panelData);
                }
                else
                {
                    SendPanelDataWithJson((Panel)sender, panelData);
                }
            }
        }

        private void SendPanelDataSimple(Panel panel, PanelData panelData)
        {
            WebRequests.SendActionNoData(panelData.Entity.EntityId);

            panelData.Entity.State = GetToggledState(panelData.Entity);
            panelData.Entity.LastUpdated = DateTime.Now;

            UpdateChildPanelIfneeded(panel, new List<Entity>() { panelData.Entity });
        }

        /// <summary>
        /// Iteract with the provided Data Panel and update the UI and children Panels where applicable.
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="panelData"></param>
        private void SendPanelDataWithJson(Panel panel, PanelData panelData)
        {
            if (panelData.ChildrenEntities != null)
            {
                WebRequests.SendAction(panelData.ServiceToInvokeOnTap, panelData.ChildrenEntities.Select(x => x.EntityId));

                // Toggle the state of the root and children entities
                panelData.Entity.State = GetToggledState(panelData.Entity);
                panelData.Entity.LastUpdated = DateTime.Now;

                foreach (Entity child in panelData.ChildrenEntities)
                {
                    child.State = GetToggledState(child);
                    child.LastUpdated = DateTime.Now;
                }

                UpdateChildPanelIfneeded((FrameworkElement)this.Content, panelData.ChildrenEntities.Union(new List<Entity> { panelData.Entity } ));
            }
            else
            {
                WebRequests.SendAction(panelData.Entity.EntityId, panelData.ServiceToInvokeOnTap);

                panelData.Entity.State = GetToggledState(panelData.Entity);
                panelData.Entity.LastUpdated = DateTime.Now;

                UpdateChildPanelIfneeded(panel, new List<Entity>() { panelData.Entity });
            }
        }

        private void PanelElement_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            MousePressStartTime = DateTime.Now;

            Panel panel = sender as Panel;

            panel.Background = new SolidColorBrush(Color.FromArgb(80, Colors.LightGray.R, Colors.LightGray.G, Colors.LightGray.B));
        }

        private void PanelElement_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Panel panel = sender as Panel;
            
            panel.Background = PanelData.GetPanelData(sender).BackgroundBrush;
        }

        private async Task LoadFrame()
        {
            DateTime dateTimeStart = DateTime.Now;

            Task<List<Entity>> task = WebRequests.GetData<List<Entity>>(hostname, "api/states", apiPassword);

            List<Entity> allEntities = await task;

            //IOrderedEnumerable<Entity> orderedEntities = allEntities.Where(entity => entity.Attributes != null && entity.Attributes.ContainsKey("entity_id")).OrderBy(entity => (int)entity.Attributes["order"]);

            TimeSpan duration = DateTime.Now - dateTimeStart;
            Debug.WriteLine($"{nameof(LoadFrame)} took {duration.TotalMilliseconds}ms to retrieve list of all States from HomeAssistant.");

            //IEnumerable<Entity> entityHeaders = allGroups.Where(group => group.Attributes.ContainsKey("view"));
            //IEnumerable<Entity> allCards = allGroups.Where(group => !group.Attributes.ContainsKey("view"));

            //ScrollViewer scrollViewer = new ScrollViewer();
            //scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            //StackPanel mainStackPanel = new StackPanel();
            //mainStackPanel.Orientation = Orientation.Vertical;
            //mainStackPanel.HorizontalAlignment = HorizontalAlignment.Center;

            //mainStackPanel.Children.Add(CreateTopPanel(allViews));
            //mainStackPanel.Children.Add(CreateActionPanel(allCards, allEntities));

            //scrollViewer.Content = mainStackPanel;

            //ImageBrush imageBrush = Imaging.LoadImageBrush("background-blue.jpg");

            //scrollViewer.Background = imageBrush;

            //this.Content = scrollViewer;

            this.Content = CreateViews(allEntities);

            duration = DateTime.Now - dateTimeStart;
            Debug.WriteLine($"{nameof(LoadFrame)} took {duration.TotalMilliseconds}ms to run.");

            // Kick off the entity updater thread to keep the data in sync
            cancellationTokenSource = new CancellationTokenSource();
            await Task.Run(() => EntityUpdateThread(cancellationTokenSource.Token), cancellationTokenSource.Token);
        }

        private async void EntityUpdateThread(CancellationToken cancellationToken)
        {
            Debug.WriteLine($"{nameof(EntityUpdateThread)} now running.");

            // Infinite loop until told to stop
            while (!cancellationToken.IsCancellationRequested)
            {
                DateTime now = DateTime.Now;

                if (!cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine($"{nameof(EntityUpdateThread)} is awake. Now processing.");

                    Task<List<Entity>> task = WebRequests.GetData<List<Entity>>(hostname, "api/states", apiPassword);
                    List<Entity> allEntities = await task;

                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        UpdateChildPanelIfneeded((FrameworkElement)this.Content, allEntities);
                    });
                }

                Debug.WriteLine($"{nameof(EntityUpdateThread)} now sleeping.");

                Task.Delay(10000, cancellationToken).ContinueWith(tsk => { }).Wait();
            }

            Debug.WriteLine($"{nameof(EntityUpdateThread)} now terminating.");
        }

        /// <summary>
        /// Data Update on UI Thread
        /// </summary>
        /// <param name="element"></param>
        /// <param name="allEntities"></param>
        private void UpdateChildPanelIfneeded(FrameworkElement element, IEnumerable<Entity> allEntities)
        {
            PanelData panelData = PanelData.GetPanelData(element);
            
            // Only scan UI Elements which have PanelData as this signifies a panel element with data to process
            if (panelData != null) 
            {
                // Get this entity
                Entity entity = allEntities.FirstOrDefault(x => x.EntityId == panelData.Entity.EntityId);

                if (entity != null)
                {
                    // Check if we need to update the dashboard with fresh data
                    if (entity.LastUpdated > panelData.LastDashboardtaUpdate)
                    {
                        Debug.WriteLine($"{nameof(UpdateChildPanelIfneeded)} updating {panelData.Entity.EntityId}.");

                        Panel panel;

                        if (panelData.ChildrenEntities != null)
                        {
                            panel = CreateGroupEntityPanel(entity, panelData.ChildrenEntities);
                        }
                        else
                        {
                            panel = CreateChildEntityPanel(entity);
                        }

                        Panel parentPanel = (Panel)VisualTreeHelper.GetParent(element);

                        int indexOfElement = parentPanel.Children.IndexOf(element);
                        parentPanel.Children.RemoveAt(indexOfElement);
                        parentPanel.Children.Insert(indexOfElement, panel);
                    }
                }
            }
            else
            {
                // Update all children
                int childCount = VisualTreeHelper.GetChildrenCount(element);
                for (int i = 0; i < childCount; i++)
                {
                    DependencyObject obj = VisualTreeHelper.GetChild(element, i);
                    UpdateChildPanelIfneeded((FrameworkElement)obj, allEntities);
                }
            }
        }

        /// <summary>
        /// Create the main hub view.
        /// </summary>
        /// <param name="allEntities"></param>
        /// <returns></returns>
        private ScrollViewer CreateViews(IEnumerable<Entity> allEntities)
        {
            ScrollViewer scrollViewer = new ScrollViewer();
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;
            stackPanel.HorizontalAlignment = HorizontalAlignment.Center;

            IEnumerable<Entity> entityHeaders = allEntities.Where(group => group.Attributes.ContainsKey("view"));

            foreach (Entity entityHeader in entityHeaders)
            {
                stackPanel.Children.Add(CreateEntitiesInView(entityHeader, allEntities));
            }

            scrollViewer.Content = stackPanel;

            ImageBrush imageBrush = Imaging.LoadImageBrush("background-blue.jpg");

            scrollViewer.Background = imageBrush;

            return scrollViewer;
        }

        /// <summary>
        /// Creates the hub section view.
        /// </summary>
        /// <param name="entityHeader"></param>
        /// <param name="allEntities"></param>
        /// <returns></returns>
        private WrapPanel CreateEntitiesInView(Entity entityHeader, IEnumerable<Entity> allEntities)
        {
            WrapPanel wrapPanel = new WrapPanel();

            foreach (string groupEntityId in entityHeader.Attributes["entity_id"])
            {
                Entity entity = allEntities.First(x => x.EntityId == groupEntityId);

                if (entity.Attributes.ContainsKey("entity_id"))
                {
                    // Group panel
                    Panel panelGroup = CreateGroupEntityPanel(entity, allEntities);

                    if (panelGroup != null)
                    {
                        wrapPanel.Children.Add(panelGroup);
                    }

                    // Children entities
                    foreach (string childEntityId in entity.Attributes["entity_id"])
                    {
                        Entity childEntity = allEntities.First(x => x.EntityId == childEntityId);

                        Panel panelChild = CreateChildEntityPanel(childEntity);

                        if (panelChild != null)
                        {
                            wrapPanel.Children.Add(panelChild);
                        }
                    }
                    
                }
                else
                {
                    // Single entity
                    Panel panel = CreateChildEntityPanel(entity);

                    if (panel != null)
                    {
                        wrapPanel.Children.Add(panel);
                    }
                }
            }

            //Newtonsoft.Json.Linq.JArray entityIds = entityHeader.Attributes["entity_id"];

            //PanelBuilderBase customEntity = CustomEntities.FirstOrDefault(x => entityIds.Any(y => y.ToString().StartsWith(x.EntityIdStartsWith)));

            return wrapPanel;
        }

        private Panel CreateGroupEntityPanel(Entity entity, IEnumerable<Entity> allStates)
        {
            if (entity.Attributes.ContainsKey("hidden") && entity.Attributes["hidden"])
            {
                return null;
            }

            Newtonsoft.Json.Linq.JArray entityIds = entity.Attributes["entity_id"];

            PanelBuilderBase customEntity = CustomEntities.FirstOrDefault(x => entityIds.Any(y => y.ToString().StartsWith(x.EntityIdStartsWith)));

            IEnumerable<Entity> childrenEntities = allStates.Where(s => entityIds.Any(e => e.ToString() == s.EntityId));

            return customEntity.CreateGroupPanel(entity, childrenEntities);
        }

        private Panel CreateChildEntityPanel(Entity entity)
        {
            if (entity.Attributes.ContainsKey("hidden") && entity.Attributes["hidden"])
            {
                return null;
            }

            PanelBuilderBase customEntity = CustomEntities.First(x => entity.EntityId.StartsWith(x.EntityIdStartsWith));

            return customEntity.CreatePanel(entity);
        }

        private string GetToggledState(Entity entity)
        {
            switch (entity.State)
            {
                case "true":
                    return "false";
                case "false":
                    return "true";
                case "on":
                    return "off";
                case "off":
                    return "on";
                case "playing":
                    return "paused";
                case "paused":
                    return "playing";
                case "1":
                    return "0";
                case "0":
                    return "1";

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //private void MainPage_Tapped(object sender, PointerRoutedEventArgs e)
        //{
        //    if (contentDialog != null)
        //    {
        //        contentDialog.Hide();
        //        cancellationTokenSourceContentDialog.Cancel();
        //    }
        //}
    }
}
