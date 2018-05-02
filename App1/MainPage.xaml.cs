using Hashboard;
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
using Windows.UI.Input;
using Windows.UI.Popups;
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
        private const int LightSensorReadingRateInMs = 5000;

        private const string CustomgroupPanelName = "customstaticgroup";

        private const string SettingsControlPanelName = "settingscontrol";

        private const string ThemeControlMenuPanelName = "themecontrol";

        private DateTime MousePressStartTime;

        private List<PanelBuilderBase> CustomEntities;

        // Polling thread cancellation token
        private CancellationTokenSource PollingThreadCancellationToken;

        // MQTT response worker thread and wakeup cancellation tokens
        private CancellationTokenSource EntityUpdateRequestedQuitCancellationToken;
        private CancellationTokenSource EntityUpdateRequestedWakeupCancellationToken;

        // Pxoximity sensors
        private DisplayRequest DisplayRequestSetting = new DisplayRequest();
        private BrightnessOverride BrightnessOverrideSetting;
        private LightSensor LightSensorSetting;
        private double PreviousDisplayBrightness;

        MqttSubscriber mqttSubscriber;

        /// <summary>
        /// List of custom controls which can be opned as a Popup via Tap or Tap+Hold actions on a Panel.
        /// </summary>
        private List<string> popupUserControlList = new List<string>()
        {
            nameof(MediaControl),
            nameof(LightControl),
            nameof(ClimateControl),
            nameof(SettingsControl),
            nameof(ThemeControl),
        };

        public MainPage()
        {
            this.InitializeComponent();

            this.RequestedTheme = ThemeControl.GetApplicationTheme();

            //ShowMainPageBackground();
            ScrollViewer scrollViewer = this.FindName("MainScrollView") as ScrollViewer;
            scrollViewer.Background = ThemeControl.BackgroundBrush;

            LoadEntityHandler();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Keep the display in the On state while the app is running
            DisplayRequestSetting.RequestActive();

            // Get capability to dim and brighten the display when needed
            BrightnessOverrideSetting = BrightnessOverride.GetForCurrentView();
            if (BrightnessOverrideSetting.IsSupported)
            {
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

            StartPollingThread();

            StartMqttSubscriber();
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
                        //Debug.WriteLine($"LightSensorReading: {lightSensorReading.IlluminanceInLux.ToString()}. Setting brightness to {brightness.ToString()}.");

                        BrightnessOverrideSetting.SetBrightnessLevel(brightness, DisplayBrightnessOverrideOptions.None);

                        PreviousDisplayBrightness = brightness;
                    }
                }
            });
        }

        /// <summary>
        /// Invoked immediately before the Page is unloaded and is no longer the current source of a parent Frame.
        /// </summary>
        /// <param name="e"></param>
        //protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        //{
        //    // Return the dispaly brightness control back to original setting
        //    DisplayRequestSetting.RequestRelease();

        //    // Terminate the polling thread if it was started
        //    PollingThreadCancellationToken?.Cancel();

        //    // Termiante the MQTT worker thread if it was started
        //    EntityUpdateRequestedQuitCancellationToken?.Cancel();
        //    EntityUpdateRequestedWakeupCancellationToken?.Cancel();

        //    base.OnNavigatingFrom(e);
        //}

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

                new ClimatePanelBuilder() {
                    EntityIdStartsWith = "climate.",
                    FontSize = 32,
                    //HoldEventAction = nameof(ClimateControl),
                    TapEventAction = nameof(ClimateControl),
                    //ValueTextFromAttributeOverride = "current_temperature",
                    TapEventHandler = PanelElement_Tapped,
                    //HoldEventHandler = PanelElement_Holding,
                    PressedEventHandler = PanelElement_PointerPressed,
                    ReleasedEventHandler = PanelElement_PointerExited},

                new MediaPlayerPanelBuilder() {
                    EntityIdStartsWith = "media_player.",
                    TapEventAction = "media_play_pause",
                    Size = EntitySize.Normal,
                    HoldEventAction = nameof(MediaControl),
                    TapEventHandler = PanelElement_Tapped,
                    HoldEventHandler = PanelElement_Holding,
                    PressedEventHandler = PanelElement_PointerPressed,
                    ReleasedEventHandler = PanelElement_PointerExited},

                new LightPanelBuilder() {
                    EntityIdStartsWith = "light.",
                    TapEventAction = "toggle",
                    HoldEventAction = nameof(LightControl),
                    TapEventHandler = PanelElement_Tapped,
                    HoldEventHandler = PanelElement_Holding,
                    PressedEventHandler = PanelElement_PointerPressed,
                    ReleasedEventHandler = PanelElement_PointerExited},

                new NameOnlyPanelBuilder() {
                    EntityIdStartsWith = "script.",
                    TapEventHandler = PanelElement_Tapped,
                    //HoldEventHandler = PanelElement_Holding,
                    PressedEventHandler = PanelElement_PointerPressed,
                    ReleasedEventHandler = PanelElement_PointerExited},

                new GenericPanelBuilder() {
                    EntityIdStartsWith = "switch.",
                    TapEventAction = "toggle",
                    TapEventHandler = PanelElement_Tapped,
                    //HoldEventHandler = PanelElement_Holding,
                    PressedEventHandler = PanelElement_PointerPressed,
                    ReleasedEventHandler = PanelElement_PointerExited},

                new GenericPanelBuilder() {
                    EntityIdStartsWith = "automation.",
                    TapEventAction = "activate",
                    HoldEventAction = "toggle",
                    TapEventHandler = PanelElement_Tapped,
                    HoldEventHandler = PanelElement_Holding,
                    PressedEventHandler = PanelElement_PointerPressed,
                    ReleasedEventHandler = PanelElement_PointerExited},

                new NameOnlyPanelBuilder() {
                    EntityIdStartsWith = $"{SettingsControlPanelName}.",
                    TapEventAction = nameof(SettingsControl),
                    TapEventHandler = PanelElement_Tapped,
                    PressedEventHandler = PanelElement_PointerPressed,
                    ReleasedEventHandler = PanelElement_PointerExited},

                new NameOnlyPanelBuilder() {
                    EntityIdStartsWith = $"{ThemeControlMenuPanelName}.",
                    TapEventAction = nameof(ThemeControl),
                    TapEventHandler = PanelElement_Tapped,
                    PressedEventHandler = PanelElement_PointerPressed,
                    ReleasedEventHandler = PanelElement_PointerExited},

                new GenericPanelBuilder() { EntityIdStartsWith = string.Empty },
            };
        }

        /// <summary>
        /// Show the PopUp custom UI.
        /// </summary>
        private void ShowPopupControl(string popupControl, Entity entity, IEnumerable<Entity> childrenEntities = null)
        {
            UserControl popupContent;

            switch (popupControl)
            {
                case nameof(LightControl):
                    popupContent = new LightControl(entity, childrenEntities);
                    break;

                case nameof(MediaControl):
                    popupContent = new MediaControl(entity);
                    break;

                case nameof(SettingsControl):
                    popupContent = new SettingsControl();
                    break;

                case nameof(ClimateControl):
                    popupContent = new ClimateControl(entity);
                    break;

                case nameof(ThemeControl):
                    popupContent = new ThemeControl(ShowMainPageBackground);
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

                popup.Closed += async (s, re) =>
                {
                    if (popup.Child is SettingsControl)
                    {
                        await LoadFrame();

                        StartPollingThread();

                        StartMqttSubscriber();
                    }

                    if (popup.Child is ThemeControl)
                    {
                        this.RequestedTheme = ThemeControl.GetApplicationTheme();
                    }
                };

                popupContent.Loaded += (s, re) =>
                {
                    UserControl userControl = s as UserControl;

                    popup.HorizontalOffset = Window.Current.Bounds.Width / 2 - userControl.ActualWidth / 2;
                    popup.VerticalOffset = Window.Current.Bounds.Height / 2 - userControl.ActualHeight / 2;
                };

                popup.IsOpen = true;
            }
        }

        /// <summary>
        /// Updates the main background brush using theme resource.
        /// </summary>
        private async void ShowMainPageBackground()
        {
            ScrollViewer scrollViewer = this.FindName("MainScrollView") as ScrollViewer;
            scrollViewer.Background = ThemeControl.BackgroundBrush;

            await LoadFrame();
        }

        /// <summary>
        /// Application settings update
        /// </summary>
        private async void StartPollingThread()
        {
            if (SettingsControl.HomeAssistantPollingInterval == default(TimeSpan))
            {
                if (PollingThreadCancellationToken != null)
                {
                    Debug.WriteLine($"{nameof(StartPollingThread)} stopping polling thread as polling interval is set to zero.");

                    PollingThreadCancellationToken.Cancel();
                }
                else
                {
                    Debug.WriteLine($"{nameof(StartPollingThread)} no polling thread as polling interval is not set.");
                }
            }
            else
            {
                // Kick off the entity updater thread to keep the data in sync
                PollingThreadCancellationToken = new CancellationTokenSource();
                await Task.Run(() => PeriodicEntityUpdatePollingThread(PollingThreadCancellationToken.Token), PollingThreadCancellationToken.Token);
            }
        }

        /// <summary>
        /// Starts the MQTT subscriber when needed.
        /// </summary>
        private void StartMqttSubscriber()
        {
            if (string.IsNullOrEmpty(SettingsControl.MqttBrokerHostname))
            {
                Debug.WriteLine($"{nameof(StartMqttSubscriber)} no MQTT subscriber as polling interval is no MQTT broker hostname is set.");
                return;
            }

            if (mqttSubscriber == null)
            {
                mqttSubscriber = new MqttSubscriber(OnEntityUpdated, OnMqttBrokerConnectionResult);
            }

            if (!mqttSubscriber.IsSubscribed)
            {
                mqttSubscriber.Connect();

                Task.Factory.StartNew(MqttEntityUpdateRequestedResponseThread);

                Debug.WriteLine($"{nameof(StartMqttSubscriber)} connecting to MQTT.");
            }
            else
            {
                Debug.WriteLine($"{nameof(StartMqttSubscriber)} MQTT subscriber is already subscribed.");
            }
        }

        /// <summary>
        /// Handle MQTT Broker connection attmpts with failure message prompts for the user.
        /// </summary>
        /// <param name="connectionResponse"></param>
        private async void OnMqttBrokerConnectionResult(byte connectionResponse)
        {
            if (connectionResponse != 0)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    MessageDialog dialog = new MessageDialog($"Failed to connect to MQTT broker '{SettingsControl.MqttBrokerHostname}'." +
                        $"Response '{connectionResponse}': '{mqttSubscriber.Status}'.", "MQTT Broker");

                    await dialog.ShowAsync();
                });
            }
            else
            {
                Debug.WriteLine($"{nameof(StartMqttSubscriber)} successfully subscribed to MQTT brodker '{SettingsControl.MqttBrokerHostname}'.");
            }
        }

        private void PanelElement_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e == null || e.HoldingState == HoldingState.Completed)
            {
                PanelData panelData = PanelData.GetPanelData(sender);

                if (string.IsNullOrEmpty(panelData.ActionToInvokeOnHold))
                {
                    SendPanelDataSimple(sender as Panel, panelData);
                }
                else
                { 
                    if (popupUserControlList.Any(x => string.Equals(x, panelData.ActionToInvokeOnHold, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        ShowPopupControl(panelData.ActionToInvokeOnHold, panelData.Entity, panelData.ChildrenEntities);

                        // Restore the panel to original visual state
                        MarkPanelAsDefaultState(sender as Panel);
                    }
                    else
                    {
                        SendPanelDataWithJson((Panel)sender, panelData, panelData.ActionToInvokeOnHold);
                    }
                }
            }
        }

        /// <summary>
        /// Provide a visual indication that the panel has been pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PanelElement_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Hack to allow mouse presses to behave like tap+hold as it would on a touch screen device
            MousePressStartTime = DateTime.Now;

            MarkPanelAsPressed(sender as Panel);
        }

        /// <summary>
        /// When pointer is no longer in contact with a panel, restore it's color to original background brush.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PanelElement_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                MarkPanelAsDefaultState(sender as Panel);
            }
        }

        private void PanelElement_Tapped(object sender, TappedRoutedEventArgs e)
        {
            PanelData panelData = PanelData.GetPanelData(sender);

            if (MousePressStartTime + TimeSpan.FromSeconds(3) > DateTime.Now &&
                MousePressStartTime + TimeSpan.FromMilliseconds(300) < DateTime.Now)
            {
                if (!string.IsNullOrEmpty(panelData.ActionToInvokeOnHold))
                {
                    // Simulate tap + hold when using the mouse
                    PanelElement_Holding(sender, null);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(panelData.ActionToInvokeOnTap))
                {
                    SendPanelDataSimple((Panel)sender, panelData);
                }
                else
                {
                    // Reroute tap actions to launch a custom control panel when the requested name matches a named Popup control
                    if (popupUserControlList.Any(x => string.Equals(x, panelData.ActionToInvokeOnTap, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        ShowPopupControl(panelData.ActionToInvokeOnTap, panelData.Entity, panelData.ChildrenEntities);
                    }
                    else
                    {
                        SendPanelDataWithJson((Panel)sender, panelData, panelData.ActionToInvokeOnTap);
                    }
                }
            }
        }

        /// <summary>
        /// Send data request to Home Assistant with no JSON body in the request.
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="panelData"></param>
        private void SendPanelDataSimple(Panel panel, PanelData panelData)
        {
            MarkPanelAsUpdateRequired(panel);

            if (panelData.ChildrenEntities != null)
            {
                throw new NotImplementedException();
            }
            else
            {
                WebRequests.SendActionNoData(panelData.Entity.EntityId);
            }
        }

        /// <summary>
        /// Send data request to Home Assistant against to the provided service.
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="panelData"></param>
        private void SendPanelDataWithJson(Panel panel, PanelData panelData, string serviceToInvoke)
        {
            MarkPanelAsUpdateRequired(panel);

            if (panelData.ChildrenEntities != null)
            {
                WebRequests.SendAction(serviceToInvoke, panelData.ChildrenEntities.Select(x => x.EntityId));

                Panel parentPanel = (Panel)VisualTreeHelper.GetParent(panel);
                int indexOfGroupPanel = parentPanel.Children.IndexOf(panel);

                for (int i = indexOfGroupPanel + 1; i < indexOfGroupPanel + 1 + panelData.ChildrenEntities.Count(); i++)
                {
                    MarkPanelAsUpdateRequired(parentPanel.Children[i] as Panel);
                }
            }
            else
            {
                WebRequests.SendAction(panelData.Entity.EntityId, serviceToInvoke);
            }
        }

        /// <summary>
        /// Sets a visual indicator on the provided panel to suggest an entity state update is now pending.
        /// </summary>
        /// <param name="panel"></param>
        private void MarkPanelAsUpdateRequired(Panel panel)
        {
            panel.Background = new SolidColorBrush(Color.FromArgb(60, Colors.Lime.R, Colors.Lime.G, Colors.Lime.B));
        }

        /// <summary>
        /// Sets a visual indicator on the provided panel to show it is being pressed with a mouse or touch input.
        /// </summary>
        /// <param name="panel"></param>
        private void MarkPanelAsPressed(Panel panel)
        {
            panel.Background.Opacity = PanelBuilderBase.PressedOpacity;
        }

        /// <summary>
        /// Returns the panel to default state after a pressed event has occurred.
        /// </summary>
        /// <param name="panel"></param>
        private void MarkPanelAsDefaultState(Panel panel)
        {
            panel.Background.Opacity = PanelBuilderBase.DefaultOpacity;
        }

        /// <summary>
        /// Update the given entities on the UI thread.
        /// </summary>
        /// <param name="entitiesToUpdate"></param>
        /// <param name="allEntities"></param>
        private async void UpdateEntityPanels(IEnumerable<Entity> entitiesToUpdate, IEnumerable<Entity> allEntities)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateChildPanelIfneeded((FrameworkElement)this.Content, allEntities, entitiesToUpdate);
            });
        }

        /// <summary>
        /// MQTT callback to signal an update is requested. Queue up an event to wake up te MQTT worker thread.
        /// </summary>
        /// <param name="entityId"></param>
        private void OnEntityUpdated(string entityId)
        {
            // Set the event token signal -- very important to avoid blocking this thread since this is doing work
            // on the MQTT subscriber thread and will block additional entity updates from being requested.
            EntityUpdateRequestedWakeupCancellationToken.Cancel();
        }

        /// <summary>
        /// MQTT response is requested background worker thread. Wakes up via cancellation token to queue up a query
        /// to Home Assistant and then triggers a UI update. Once done, resets event token and goes back to sleep.
        /// </summary>
        private async void MqttEntityUpdateRequestedResponseThread()
        {
            Debug.WriteLine($"{nameof(MqttEntityUpdateRequestedResponseThread)} is now starting.");

            if (EntityUpdateRequestedQuitCancellationToken != null)
            {
                throw new ArgumentException($"{nameof(MqttEntityUpdateRequestedResponseThread)} cancellation token was not NULL.");
            }

            EntityUpdateRequestedQuitCancellationToken = new CancellationTokenSource();
            EntityUpdateRequestedWakeupCancellationToken = new CancellationTokenSource();

            DateTime lastUpdatedTime = DateTime.Now;

            while (!EntityUpdateRequestedQuitCancellationToken.IsCancellationRequested)
            {
                // Sleep until the event token is signaled
                EntityUpdateRequestedWakeupCancellationToken.Token.WaitHandle.WaitOne();

                // Allow for additional MQTT messages to be received before proceeding since it's likely there will be multiple entities
                // requesting an update at the same time.
                await Task.Delay(100);

                // Current token has been consumed so create a new token now
                EntityUpdateRequestedWakeupCancellationToken = new CancellationTokenSource();

                // Confirm cancelation is not requested first
                if (!EntityUpdateRequestedQuitCancellationToken.IsCancellationRequested)
                {
                    // Get all entities not just the entities we think we want
                    IEnumerable<Entity> allEntities = await WebRequests.GetData<IEnumerable<Entity>>($"api/states");

                    // Get all entities which have been updated since the last time we've checked
                    IEnumerable<Entity> entitiesToUpdate = allEntities.Where(x => x.LastUpdated > lastUpdatedTime).ToList();

                    if (entitiesToUpdate.Any())
                    {
                        // For all entities which need to be updated, get all group entities and check their children to see if the group entity needs to be updated as well
                        lastUpdatedTime = entitiesToUpdate.Select(x => x.LastUpdated).OrderByDescending(x => x).First();

                        foreach (Entity group in allEntities.Where(x => x.Attributes.ContainsKey("entity_id")))
                        {
                            IEnumerable<string> childrenEntityIds = (group.Attributes["entity_id"] as Newtonsoft.Json.Linq.JArray).ToString().Split("\"").Where(x => x.Contains("."));

                            if (childrenEntityIds.Any(x => entitiesToUpdate.Any(y => y.EntityId.Equals(x, StringComparison.InvariantCultureIgnoreCase))))
                            {
                                if (!entitiesToUpdate.Any(x => x.EntityId == group.EntityId))
                                {
                                    // Add this group entity which is missing from the update requested list
                                    Debug.WriteLine($"{nameof(MqttEntityUpdateRequestedResponseThread)} manually adding group {group.EntityId} to update list.");
                                    entitiesToUpdate = entitiesToUpdate.Union(new List<Entity>() { group });
                                }
                            }
                        }

                        Debug.WriteLine($"{nameof(MqttEntityUpdateRequestedResponseThread)} has updated Entities: {string.Join(", ", entitiesToUpdate.Select(x => x.EntityId).ToList())}");

                        // Perform the update on the UI thread; don't block
                        UpdateEntityPanels(entitiesToUpdate, allEntities);
                    }
                }
            }

            Debug.WriteLine($"{nameof(MqttEntityUpdateRequestedResponseThread)} is now terminating.");
        }

        /// <summary>
        /// Queries Home Assistant for state information and then populates the main view with panels to 
        /// show the state information.
        /// </summary>
        /// <returns></returns>
        private async Task LoadFrame()
        {
            ScrollViewer scrollViewer = this.FindName("MainScrollView") as ScrollViewer;
            StackPanel stackPanel = null;

            if (!string.IsNullOrEmpty(SettingsControl.HomeAssistantHostname))
            {
                Task<List<Entity>> task = WebRequests.GetData<List<Entity>>("api/states");

                stackPanel = CreateViews(await task);
            }
            else
            {
                stackPanel = CreateViews(new List<Entity>());
            }

            lock (this.Content)
            {
                scrollViewer.Content = stackPanel;
            }
        }

        /// <summary>
        /// Polling thread. Updates all entity panels with updated state information after querying
        /// Home Assistant for all entities.
        /// </summary>
        /// <param name="cancellationToken"></param>
        private async void PeriodicEntityUpdatePollingThread(CancellationToken cancellationToken)
        {
            Debug.WriteLine($"{nameof(PeriodicEntityUpdatePollingThread)} now running.");

            // Infinite loop until told to stop
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!cancellationToken.IsCancellationRequested && !string.IsNullOrEmpty(SettingsControl.HomeAssistantHostname))
                {
                    Debug.WriteLine($"{nameof(PeriodicEntityUpdatePollingThread)} is awake. Now processing.");

                    Task<List<Entity>> task = WebRequests.GetData<List<Entity>>("api/states");

                    List<Entity> allEntities = await task;

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        // Shared resource
                        lock (this.Content)
                        {
                            //UpdateChildPanelIfneeded((FrameworkElement)this.Content, allEntities);
                        }
                    });
                }

                Debug.WriteLine($"{nameof(PeriodicEntityUpdatePollingThread)} now sleeping.");

                Task.Delay(SettingsControl.HomeAssistantPollingInterval, cancellationToken).ContinueWith(tsk => { }).Wait();
            }

            Debug.WriteLine($"{nameof(PeriodicEntityUpdatePollingThread)} now terminating.");
        }

        /// <summary>
        /// Data Update on UI Thread
        /// </summary>
        /// <param name="element"></param>
        /// <param name="allEntities"></param>
        private void UpdateChildPanelIfneeded(FrameworkElement element, IEnumerable<Entity> allEntities, IEnumerable<Entity> entitiesToUpdate = null)
        {
            PanelData panelData = PanelData.GetPanelData(element);

            // Only scan UI Elements which have PanelData as this signifies a panel element with data to process
            if (panelData != null)
            {
                if (null == entitiesToUpdate)
                {
                    entitiesToUpdate = allEntities;
                }

                Entity entity = entitiesToUpdate.FirstOrDefault(x => x.EntityId == panelData.Entity.EntityId);

                // Some panels, such as the custom Settings panel, are not backed by entity data so skip those here
                if (entity != null)
                {
                    // Don't try to be fancy here. Simply update all entities as this is cheaper, easier and less 
                    // error-prone than attempt to identify which entity needs to be updated and which doesn't
                    // or which group(s) an entity is associated with, etc.

                    Panel panel;

                    if (entity.Attributes.ContainsKey("entity_id"))
                    {
                        // Update group panels
                        Newtonsoft.Json.Linq.JArray childrenEntityIds = entity.Attributes["entity_id"];
                        IEnumerable<Entity> childrenEntities = allEntities.Where(s => childrenEntityIds.Any(e => e.ToString() == s.EntityId));

                        panel = CreateGroupEntityPanel(entity, childrenEntities);
                    }
                    else
                    {
                        // Update single panels
                        panel = CreateChildEntityPanel(entity);
                    }

                    // Replace the old panel with the new panel
                    Panel parentPanel = (Panel)VisualTreeHelper.GetParent(element);
                    int indexOfElement = parentPanel.Children.IndexOf(element);
                    //parentPanel.Children.RemoveAt(indexOfElement);
                    //parentPanel.Children.Insert(indexOfElement, panel);

                    parentPanel.Children[indexOfElement] = panel;

                    // if (panelData.LastDashboardtaUpdate)
                    Debug.WriteLine($"Replaced Panel: {entity.EntityId}.");
                }
            }
            else
            {
                // Attempt to update panels with this current parent element
                int childCount = VisualTreeHelper.GetChildrenCount(element);
                for (int i = 0; i < childCount; i++)
                {
                    DependencyObject obj = VisualTreeHelper.GetChild(element, i);
                    UpdateChildPanelIfneeded((FrameworkElement)obj, entitiesToUpdate, allEntities);
                }
            }
        }

        /// <summary>
        /// Create a Panel for the purpose of loading a Settings UI selection popup screen.
        /// </summary>
        /// <returns></returns>
        private Entity CreateCustomStaticPanel(string panelName, string panelPicture)
        {
            // Create the custom panel
            return new Entity()
            {
                EntityId = $"{panelName}.{panelName}",
                LastChanged = DateTime.Now,
                LastUpdated = DateTime.Now,
                State = "on",
                Attributes = new Dictionary<string, dynamic>() {
                    { "friendly_name", string.Empty },
                    { "local_assets_picture", panelPicture }
            } };
        }

        /// <summary>
        /// Creates a custom panel.
        /// </summary>
        /// <param name="childrenEntities"></param>
        /// <returns></returns>
        private WrapPanel CreateCustomGroupPanel(IEnumerable<Entity> childrenEntities)
        { 
            // Create the custom group to hold the panel
            Entity view = new Entity()
            {
                EntityId = $"group.{CustomgroupPanelName}",
                LastChanged = DateTime.Now,
                LastUpdated = DateTime.Now,
                State = "off",
                Attributes = new Dictionary<string, dynamic>() {
                    { "friendly_name", CustomgroupPanelName },
                    //{ "entity_id", "[" + string.Join(", ", childrenEntities.Select(x => "\"" + x.EntityId + "\"")) + "]"},
                    { "entity_id", childrenEntities.Select(x => x.EntityId).ToList() },
                    { "order", 999 },
                    { "view", true },
            } };

            return CreateEntitiesInView(view, childrenEntities);
        }

        private StackPanel CreateViews(IEnumerable<Entity> allEntities)
        {
            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;
            stackPanel.HorizontalAlignment = HorizontalAlignment.Center;

            //ImageBrush imageBrush = Imaging.LoadImageBrush("background-blue.jpg");
            //stackPanel.Background = imageBrush;
            //stackPanel.Opacity = 0.5;

            // Get all home assistant "group" entities which have the "view=true" attribute set in customizations.yaml
            IEnumerable<Entity> entityHeaders = allEntities.Where(group => group.Attributes.ContainsKey("view"));

            // Add all single and group entities which are tied to each view
            foreach (Entity entityHeader in entityHeaders)
            {
                stackPanel.Children.Add(CreateEntitiesInView(entityHeader, allEntities));
            }

            // Add a Settings panel
            Entity settingsEntity = CreateCustomStaticPanel(SettingsControlPanelName, "panel-settings.png");
            Entity themeEntity = CreateCustomStaticPanel(ThemeControlMenuPanelName, "panel-paintbrush.png");
            WrapPanel customWrapPanel = CreateCustomGroupPanel(new List<Entity>() { settingsEntity, themeEntity });

            stackPanel.Children.Add(customWrapPanel);
                        
            return stackPanel;
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

            return wrapPanel;
        }

        private Panel CreateGroupEntityPanel(Entity entity, IEnumerable<Entity> allStates)
        {
            if (entity.Attributes.ContainsKey("hidden") && entity.Attributes["hidden"])
            {
                return null;
            }

            Newtonsoft.Json.Linq.JArray childrenEntityIds = entity.Attributes["entity_id"];
            
            PanelBuilderBase customEntity = CustomEntities.FirstOrDefault(x => childrenEntityIds.Any(y => y.ToString().StartsWith(x.EntityIdStartsWith)));

            IEnumerable<Entity> childrenEntities = allStates.Where(s => childrenEntityIds.Any(e => e.ToString() == s.EntityId));

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
    }
}
