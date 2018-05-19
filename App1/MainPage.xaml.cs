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
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Hashboard.PanelTouchHandler;
using static HashBoard.Entity;
using static HashBoard.PanelBuilderBase;

namespace HashBoard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Built in panels which are always shown
        /// </summary>
        private const string CustomgroupPanelName = "customstaticgroup";

        private const string SettingsControlPanelName = "settingscontrol";

        private const string ThemeControlMenuPanelName = "themecontrol";

        /// <summary>
        /// Convert mouse press+hold in to a tap+hold event which happens natively on touch
        /// screen devices only.
        /// </summary>
        private DateTime MousePressStartTime;

        /// <summary>
        /// Polling thread cancellation token
        /// </summary>
        private CancellationTokenSource PollingThreadQuitCancellationToken;
        private CancellationTokenSource PollingThreadResetTimerCancellationToken;

        /// <summary>
        /// MQTT subscriber and worker cancellation tokens for event driven work requests
        /// </summary>
        private CancellationTokenSource EntityUpdateRequestedQuitCancellationToken;
        private CancellationTokenSource EntityUpdateRequestedWakeupCancellationToken;

        private MqttSubscriber mqttSubscriber = new MqttSubscriber();

        /// <summary>
        /// Pxoximity sensors
        /// </summary>
        private readonly TimeSpan LightSensorReadingRateInMs = TimeSpan.FromSeconds(5);

        private DisplayRequest DisplayRequestSetting = new DisplayRequest();
        private BrightnessOverride BrightnessOverrideSetting;
        private LightSensor LightSensorSetting;
        private double PreviousDisplayBrightness;

        /// <summary>
        /// Rule list which instructs how to build each type of entity in to a panel.
        /// </summary>
        private List<PanelBuilderBase> CustomEntities;

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

        /// <summary>
        /// Callback for Popup controls to be notified when the entity being edited has been modified.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="childrenEntities"></param>
        private delegate void EntityUpdatedCallback(Entity entity, IEnumerable<Entity> childrenEntities);
        private EntityUpdatedCallback NotifyPopupEntityUpdate = null;
        private Entity PopupEntity = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            this.RequestedTheme = ThemeControl.GetApplicationTheme();

            Application.Current.Suspending += App_Suspending;
            Application.Current.Resuming += App_Resuming;

            LoadCustomEntityHandler();

            StartPollingThread();

            StartMqttSubscriber();
        }

        /// <summary>
        /// Resuming
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void App_Resuming(object sender, object e)
        {
            Debug.WriteLine($"{nameof(App_Resuming)} starting any additional threads for Resume.");

            // Force an update of all entities immediately to ensure all panels have up-to-date state data
            await UpdateEntitiesSinceLastUpdate(default(DateTime));

            StartPollingThread();

            StartMqttSubscriber();
        }

        /// <summary>
        /// Suspending
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            Debug.WriteLine($"{nameof(App_Suspending)} stopping all threads to prepare for Suspend.");

            PollingThreadQuitCancellationToken?.Cancel();
            PollingThreadResetTimerCancellationToken?.Cancel();

            EntityUpdateRequestedQuitCancellationToken?.Cancel();
            EntityUpdateRequestedWakeupCancellationToken?.Cancel();
        }

        /// <summary>
        /// OnNavitatedTo
        /// </summary>
        /// <param name="e"></param>
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
                LightSensorSetting.ReportInterval = Convert.ToUInt32(LightSensorReadingRateInMs.TotalMilliseconds);
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
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                LightSensorReading lightSensorReading = lightSensor.GetCurrentReading();

                if (BrightnessOverrideSetting != null && BrightnessOverrideSetting.IsSupported)
                {
                    const double maximumAllowedBrightness = 0.15;
                    const double highestLuxValueBeforeFullBrightness = 25.0;

                    double brightness = Math.Min(lightSensorReading.IlluminanceInLux, highestLuxValueBeforeFullBrightness) / highestLuxValueBeforeFullBrightness * maximumAllowedBrightness;

                    if (PreviousDisplayBrightness != brightness)
                    {
                        BrightnessOverrideSetting.SetBrightnessLevel(brightness, DisplayBrightnessOverrideOptions.None);

                        PreviousDisplayBrightness = brightness;
                    }
                }
            });
        }
        
        private void LoadCustomEntityHandler()
        {
            CustomEntities = new List<PanelBuilderBase>()
            {
                // Date/Time sensor
                new DateTimePanelBuilder() {
                    EntityIdStartsWith = "sensor.date__time",
                    Size = EntitySize.Wide,
                    FontSize = 24 },

                // Dark Sky summary
                new StateOnlyPanelBuilder() {
                    EntityIdStartsWith = "sensor.dark_sky_daily_summary",
                    Size = EntitySize.Wide },

                // Dark Sky temperature
                new StateOnlyPanelBuilder() {
                    EntityIdStartsWith = "sensor.dark_sky_temperature",
                    FontSize = 32 },

                new DarkSkyPanelbuilder() {
                    EntityIdStartsWith = "sensor.forecast_today" },

                new DarkSkyPanelbuilder() {
                    EntityIdStartsWith = "sensor.forecast_",
                    Size = EntitySize.Condensed },

                // Climate Platform
                new ClimatePanelBuilder() {
                    EntityIdStartsWith = "climate.",
                    FontSize = 32,
                    TapHandler = new PanelTouchHandler(nameof(ClimateControl), ResponseExpected.None),
                    TapAndHoldHandler = new PanelTouchHandler(nameof(ClimateControl), ResponseExpected.None) },

                // Media Player Platform
                new MediaPlayerPanelBuilder() {
                    EntityIdStartsWith = "media_player.",
                    Size = EntitySize.Normal,
                    TapHandler = new PanelTouchHandler(new Dictionary<uint, string> {
                        { (uint)MediaPlatformSupportedFeatures.PlayMedia, "media_play_pause" }, // ServiceAction for PlayMedia supported_feature
                        { (uint)MediaPlatformSupportedFeatures.TurnOn, "toggle" },              // ServiceAction for TurnOn supported_feature
                        }, ResponseExpected.EntityUpdated),
                    TapAndHoldHandler = new PanelTouchHandler(nameof(MediaControl), ResponseExpected.None) },

                // Light Platform
                new LightPanelBuilder() {
                    EntityIdStartsWith = "light.",
                    TapHandler = new PanelTouchHandler("toggle", ResponseExpected.EntityUpdated),
                    TapAndHoldHandler = new PanelTouchHandler(nameof(LightControl), ResponseExpected.None) },

                // Script Platform
                new NameOnlyPanelBuilder() {
                    EntityIdStartsWith = "script.",
                    TapHandler = new PanelTouchHandler(string.Empty, ResponseExpected.None) },

                // Switch Platform
                new GenericPanelBuilder() {
                    EntityIdStartsWith = "switch.",
                    TapHandler = new PanelTouchHandler("toggle", ResponseExpected.EntityUpdated) },

                // Automation Platform
                new GenericPanelBuilder() {
                    EntityIdStartsWith = "automation.",
                    TapHandler = new PanelTouchHandler("trigger", ResponseExpected.None),
                    TapAndHoldHandler = new PanelTouchHandler("toggle", ResponseExpected.EntityUpdated) },
                
                // Default Settings Control Panel
                new NameOnlyPanelBuilder() {
                    EntityIdStartsWith = $"{SettingsControlPanelName}.",
                    TapHandler = new PanelTouchHandler(nameof(SettingsControl), ResponseExpected.None) },

                // Default Theme Control Panel
                new NameOnlyPanelBuilder() {
                    EntityIdStartsWith = $"{ThemeControlMenuPanelName}.",
                    TapHandler = new PanelTouchHandler(nameof(ThemeControl), ResponseExpected.None) },

                // Everything else
                new GenericPanelBuilder() { EntityIdStartsWith = string.Empty },
            };

            // Where tap or tap+hold are requested, assign the touch events for proper touch routing
            foreach (PanelBuilderBase customPanelBuilder in CustomEntities)
            {
                if (customPanelBuilder.TapHandler != null)
                {
                    customPanelBuilder.TapEventHandler = PanelElement_Tapped;
                    customPanelBuilder.PressedEventHandler = PanelElement_PointerPressed;
                    customPanelBuilder.ReleasedEventHandler = PanelElement_PointerExited;
                }

                if (customPanelBuilder.TapAndHoldHandler != null)
                {
                    customPanelBuilder.HoldEventHandler = PanelElement_Holding;
                    customPanelBuilder.PressedEventHandler = PanelElement_PointerPressed;
                    customPanelBuilder.ReleasedEventHandler = PanelElement_PointerExited;
                }
            }
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
                    NotifyPopupEntityUpdate = (popupContent as LightControl).EntityUpdated;
                    break;

                case nameof(MediaControl):
                    popupContent = new MediaControl(entity);
                    NotifyPopupEntityUpdate = (popupContent as MediaControl).EntityUpdated;
                    break;

                case nameof(ClimateControl):
                    popupContent = new ClimateControl(entity);
                    NotifyPopupEntityUpdate = (popupContent as ClimateControl).EntityUpdated;
                    break;

                case nameof(SettingsControl):
                    popupContent = new SettingsControl();
                    break;

                case nameof(ThemeControl):
                    popupContent = new ThemeControl(LoadFrame);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (popupContent != null)
            {
                // When a entity-updated callback is requested, keep track of the entity
                // which the pop-up control is monitoring.
                if (null != NotifyPopupEntityUpdate)
                {
                    PopupEntity = entity;
                }

                Popup popup = new Popup()
                {
                    IsLightDismissEnabled = true,
                    LightDismissOverlayMode = LightDismissOverlayMode.On,
                    Child = popupContent,
                };

                popup.Closed += async (s, re) =>
                {
                    PopupEntity = null;
                    NotifyPopupEntityUpdate = null; 

                    if (popup.Child is SettingsControl)
                    {
                        // Save application settings when closing the Settings popup
                        if ((popup.Child as SettingsControl).SaveSettings())
                        {
                            // Settings value changed, so update frame and reconnect to MQTT broker
                            await LoadFrame();

                            StartPollingThread();

                            StartMqttSubscriber();
                        }
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
        /// Shows an error meesage to the user.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        private async void ShowErrorDialog(string title, string message)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                MessageDialog dialog = new MessageDialog(message, title);

                await dialog.ShowAsync();
            });
        }

        /// <summary>
        /// Application settings update
        /// </summary>
        private void StartPollingThread()
        {
            if (SettingsControl.HomeAssistantPollingInterval == default(TimeSpan))
            {
                if (PollingThreadQuitCancellationToken != null)
                {
                    Debug.WriteLine($"{nameof(StartPollingThread)} stopping polling thread as polling interval is set to zero.");

                    PollingThreadQuitCancellationToken.Cancel();
                }
                else
                {
                    Debug.WriteLine($"{nameof(StartPollingThread)} no polling thread as polling interval is not set.");
                }
            }
            else
            {
                if (PollingThreadQuitCancellationToken != null)
                {
                    Debug.WriteLine($"{nameof(StartPollingThread)} polling thread is already active.");
                }
                else
                {
                    // Kick off the polling thread
                    PollingThreadQuitCancellationToken = new CancellationTokenSource();
                    Task.Factory.StartNew(PeriodicMqttHealthCheckPollingThread, PollingThreadQuitCancellationToken.Token);
                }
            }
        }

        /// <summary>
        /// Starts the MQTT subscriber when needed.
        /// </summary>
        private void StartMqttSubscriber()
        {
            if (mqttSubscriber.IsSubscribed)
            {
                Debug.WriteLine($"{nameof(StartMqttSubscriber)} Disconnecting from previous MQTT subscription.");

                EntityUpdateRequestedQuitCancellationToken?.Cancel();
                EntityUpdateRequestedWakeupCancellationToken?.Cancel();

                mqttSubscriber.Disconnect();
            }

            if (string.IsNullOrEmpty(SettingsControl.MqttBrokerHostname))
            {
                Debug.WriteLine($"{nameof(StartMqttSubscriber)} no MQTT subscriber as no MQTT broker hostname is set.");
                return;
            }

            mqttSubscriber.Connect(OnEntityUpdated, OnMqttBrokerConnectionResult);

            Debug.WriteLine($"{nameof(StartMqttSubscriber)} attempting to connect to MQTT broker.");
        }

        /// <summary>
        /// Handle MQTT Broker connection attmpts with failure message prompts for the user.
        /// </summary>
        /// <param name="connectionResponse"></param>
        private async void OnMqttBrokerConnectionResult(byte connectionResponse)
        {
            switch (connectionResponse)
            {
                case MqttSubscriber.MqttConnectionSuccess:
                    Debug.WriteLine($"{nameof(StartMqttSubscriber)} successfully subscribed to MQTT brodker '{SettingsControl.MqttBrokerHostname}'.");

                    // Connected successfully. Start the MQTT response receiver thread which will process the MQTT events.
                    EntityUpdateRequestedQuitCancellationToken = new CancellationTokenSource();
                    await Task.Factory.StartNew(MqttEntityUpdateRequestedResponseThread, EntityUpdateRequestedQuitCancellationToken.Token);
                    break;

                case MqttSubscriber.MqttConnectionException:
                    Debug.WriteLine($"{nameof(StartMqttSubscriber)} encountered exception while attempting to connect to '{SettingsControl.MqttBrokerHostname}'. Retrying.");

                    // Failed to connect. This typically means we (the client) were not yet ready to start the subscriber or did not cleanup a previous connection
                    // after a suspend/resume sequence of events. So, wait a little bit and try again. Ugh.
                    await Task.Delay(100);
                    StartMqttSubscriber();
                    break;

                default:
                    // Standard MQTT error, such as bad user name/password or invalid broker IP address. Inform user.
                    ShowErrorDialog("MQTT Broker", $"Failed to connect to MQTT broker '{SettingsControl.MqttBrokerHostname}'. " +
                        $"Got MQTT response code: {connectionResponse}. {mqttSubscriber.Status}");
                    break;
            }
        }

        /// <summary>
        /// Panel interaction handler. Sends a web request to home assistant or opens the popup control.
        /// Changes visual state of the panel to match.
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="panelData"></param>
        /// <param name="serviceToInvoke"></param>
        /// <param name="responseExpected"></param>
        private void HandleTouchEvent(Panel panel, PanelData panelData, string serviceToInvoke, ResponseExpected responseExpected)
        {
            if (string.IsNullOrEmpty(serviceToInvoke))
            {
                // This is a simple web request which has no associated JSON payload
                SendPanelDataSimple(panel, panelData, responseExpected);
            }
            else
            {
                // Reroute tap actions to launch a custom control panel when the requested name matches a named Popup control
                if (popupUserControlList.Any(x => string.Equals(x, serviceToInvoke, StringComparison.InvariantCultureIgnoreCase)))
                {
                    ShowPopupControl(serviceToInvoke, panelData.Entity, panelData.ChildrenEntities);

                    MarkPanelAsDefaultState(panel);
                }
                else
                {
                    SendPanelDataWithJson(panel, panelData, serviceToInvoke, responseExpected);
                }
            }
        }

        /// <summary>
        /// Tap and Hold on a Panel event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PanelElement_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e == null || e.HoldingState == HoldingState.Completed)
            {
                PanelData panelData = PanelData.GetPanelData(sender);

                HandleTouchEvent(sender as Panel, panelData, panelData.TapAndHoldHandler.GetServiceAction(panelData.Entity), panelData.TapAndHoldHandler.Response);
            }
        }

        /// <summary>
        /// Tap on a Panel event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PanelElement_Tapped(object sender, TappedRoutedEventArgs e)
        {
            PanelData panelData = PanelData.GetPanelData(sender);

            if (MousePressStartTime + TimeSpan.FromSeconds(3) > DateTime.Now &&
                MousePressStartTime + TimeSpan.FromMilliseconds(300) < DateTime.Now)
            {
                if (null != panelData.TapAndHoldHandler)
                {
                    // Convert this tap to simulate tap+hold when a mouse is being used
                    PanelElement_Holding(sender, null);
                }
                else
                {
                    MarkPanelAsDefaultState(sender as Panel);
                }
            }
            else
            {
                HandleTouchEvent(sender as Panel, panelData, panelData.TapHandler.GetServiceAction(panelData.Entity), panelData.TapHandler.Response);
            }
        }

        /// <summary>
        /// Returns panel opacity back to its current state.
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="responseExpected"></param>
        private void MarkPanelStatePerExpectedResponse(Panel panel, ResponseExpected responseExpected)
        {
            if (responseExpected == ResponseExpected.EntityUpdated)
            {
                MarkPanelAsUpdateRequired(panel);
            }
            else
            {
                MarkPanelAsDefaultState(panel);
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

        /// <summary>
        /// Send data request to Home Assistant with no JSON body in the request.
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="panelData"></param>
        private void SendPanelDataSimple(Panel panel, PanelData panelData, ResponseExpected responseExpected)
        {
            MarkPanelStatePerExpectedResponse(panel, responseExpected);

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
        private void SendPanelDataWithJson(Panel panel, PanelData panelData, string serviceToInvoke, ResponseExpected responseExpected)
        {
            MarkPanelStatePerExpectedResponse(panel, responseExpected);

            if (panelData.ChildrenEntities != null)
            {
                WebRequests.SendAction(serviceToInvoke, panelData.ChildrenEntities.Select(x => x.EntityId));

                Panel parentPanel = (Panel)VisualTreeHelper.GetParent(panel);
                int indexOfGroupPanel = parentPanel.Children.IndexOf(panel);

                for (int i = indexOfGroupPanel + 1; i < indexOfGroupPanel + 1 + panelData.ChildrenEntities.Count(); i++)
                {
                    MarkPanelStatePerExpectedResponse(panel, responseExpected);
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
            panel.Background = new SolidColorBrush(Colors.Lime);
            panel.Background.Opacity = StateIsOffOpacity;
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
            if (PanelData.GetPanelData(panel).Entity.IsInOffState())
            {
                panel.Background.Opacity = PanelBuilderBase.StateIsOffOpacity;
            }
            else
            {
                panel.Background.Opacity = PanelBuilderBase.DefaultOpacity;
            }
        }

        /// <summary>
        /// Gets the list of Entity IDs within the given entity's Attribute field. This simplifies the JArray type conversion
        /// as well as string parsing which contains superflous quotes and whitespace which need to be thrown out.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private IEnumerable<string> ParseEntityIdAttribute(Entity entity)
        {
            return (entity.Attributes["entity_id"] as Newtonsoft.Json.Linq.JArray).ToString().Split("\"").Where(x => x.Contains("."));
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
                // Update the main frame
                UpdateChildPanelIfneeded((FrameworkElement)this.Content, allEntities, entitiesToUpdate);

                // Update the popup control when open and the popup entity currently selected is one of the updated entities
                if (null != NotifyPopupEntityUpdate && 
                    entitiesToUpdate.Any(x => string.Equals(PopupEntity.EntityId, x.EntityId, StringComparison.InvariantCultureIgnoreCase)))
                {
                    PopupEntity = entitiesToUpdate.Where(x => string.Equals(PopupEntity.EntityId, x.EntityId, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                    // Get the children entities, if any
                    IEnumerable<Entity> childrenEntities = null;
                    if (PopupEntity.Attributes.ContainsKey("entity_id"))
                    {
                        childrenEntities = allEntities.Where(x => ParseEntityIdAttribute(PopupEntity).Any(childId => childId == x.EntityId));
                    }

                    if (PopupEntity.Attributes.ContainsKey("entity_picture"))
                    {
                        Debug.WriteLine($"{PopupEntity.Attributes["entity_picture"]}");
                    }

                    NotifyPopupEntityUpdate(PopupEntity, childrenEntities);
                }
            });
        }

        private async Task<DateTime> UpdateEntitiesSinceLastUpdate(DateTime lastUpdatedTime)
        {   
            // Get all entities not just the entities we think we want
            IEnumerable<Entity> allEntities = await GetStateData();

            if (null != allEntities)
            {
                // Get all entities which have been updated since the last time we've checked
                IEnumerable<Entity> entitiesToUpdate = allEntities.Where(x => x.LastUpdated > lastUpdatedTime).ToList();

                if (entitiesToUpdate.Any())
                {
                    // For all entities which need to be updated, get all group entities and check their children to see if the group entity needs to be updated as well
                    lastUpdatedTime = entitiesToUpdate.Select(x => x.LastUpdated).OrderByDescending(x => x).First();

                    foreach (Entity group in allEntities.Where(x => x.Attributes.ContainsKey("entity_id")))
                    {
                        IEnumerable<string> childrenEntityIds = ParseEntityIdAttribute(group);

                        if (childrenEntityIds.Any(x => entitiesToUpdate.Any(y => y.EntityId.Equals(x, StringComparison.InvariantCultureIgnoreCase))))
                        {
                            if (!entitiesToUpdate.Any(x => x.EntityId == group.EntityId))
                            {
                                // Add this group entity which is missing from the update requested list
                                Debug.WriteLine($"{nameof(UpdateEntitiesSinceLastUpdate)} manually adding group {group.EntityId} to update list.");
                                entitiesToUpdate = entitiesToUpdate.Union(new List<Entity>() { group });
                            }
                        }
                    }

                    Debug.WriteLine($"{nameof(UpdateEntitiesSinceLastUpdate)} has updated Entities: {string.Join(", ", entitiesToUpdate.Select(x => x.EntityId).ToList())}");

                    // Perform the update on the UI thread; don't block
                    UpdateEntityPanels(entitiesToUpdate, allEntities);
                }
            }

            return lastUpdatedTime;
        }

        /// <summary>
        /// MQTT callback to signal an update is requested. Queue up an event to wake up te MQTT worker thread.
        /// </summary>
        /// <param name="entityId"></param>
        private void OnEntityUpdated()
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

            if (EntityUpdateRequestedQuitCancellationToken == null)
            {
                throw new ArgumentException($"{nameof(MqttEntityUpdateRequestedResponseThread)} cancellation token was not set by caller.");
            }

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
                    lastUpdatedTime = await UpdateEntitiesSinceLastUpdate(lastUpdatedTime);
                }
            }

            // Terminating so disconnect from MQTT as well
            if (mqttSubscriber.IsSubscribed)
            {
                mqttSubscriber.Disconnect();
            }

            Debug.WriteLine($"{nameof(MqttEntityUpdateRequestedResponseThread)} is now terminating.");
        }

        /// <summary>
        /// Polling thread. Reconnects the MQTT broker if it was found to be in a disconnected state.
        /// </summary>
        private async void PeriodicMqttHealthCheckPollingThread()
        {
            Debug.WriteLine($"{nameof(PeriodicMqttHealthCheckPollingThread)} is starting.");

            if (PollingThreadQuitCancellationToken == null)
            {
                throw new ArgumentException($"{nameof(PeriodicMqttHealthCheckPollingThread)} cancellation token was not set by caller.");
            }

            PollingThreadResetTimerCancellationToken = new CancellationTokenSource();

            // Infinite loop until told to stop
            while (!PollingThreadQuitCancellationToken.IsCancellationRequested)
            {
                await Task.Delay(SettingsControl.HomeAssistantPollingInterval, PollingThreadResetTimerCancellationToken.Token).ContinueWith(tsk => { });

                Debug.WriteLine($"{nameof(PeriodicMqttHealthCheckPollingThread)} now processing.");

                if (!PollingThreadResetTimerCancellationToken.IsCancellationRequested)
                {
                    ResubscribeToMqttBrokerIfNeeded();
                }
            }

            Debug.WriteLine($"{nameof(PeriodicMqttHealthCheckPollingThread)} now terminating.");
        }

        /// <summary>
        /// Checks if we are still subscribed to MQTT topic and if not, attempt to reconnect.
        /// </summary>
        private void ResubscribeToMqttBrokerIfNeeded()
        {
            // Make sure our MQTT broker is connected if expected
            if (!string.IsNullOrEmpty(SettingsControl.MqttBrokerHostname))
            {
                if (!mqttSubscriber.IsSubscribed)
                {
                    Debug.WriteLine($"{nameof(ResubscribeToMqttBrokerIfNeeded)} found unexpected MQTT disconnect. Attempting to reconnect.");

                    StartMqttSubscriber();
                }
            }
        }

        /// <summary>
        /// Queries Home Assistant for state information and then populates the main view with panels to 
        /// show the state information.
        /// </summary>
        /// <returns></returns>
        private async Task LoadFrame()
        {
            ScrollViewer scrollViewer = this.FindName("MainScrollView") as ScrollViewer;
            scrollViewer.Background = ThemeControl.BackgroundBrush;

            IEnumerable<Entity> entities = null;

            if (!string.IsNullOrEmpty(SettingsControl.HomeAssistantHostname))
            {
                entities = await GetStateData();
            }

            scrollViewer.Content = CreateViews(entities);

            // By setting a center horizontal aligntment we trim off the edges so that the dead space around
            // the left and right edges of the screen will render black. This only works well when there's 
            // content which wraps the screen else we'll trim the background image as well, so guard against
            // null content before applying this setting.
            if (null != entities)
            {
                scrollViewer.HorizontalAlignment = HorizontalAlignment.Center;
            }
        }

        /// <summary>
        /// Queries home assistant for state data. If a connection error occurs, shows the error via UI and stops all background threads.
        /// </summary>
        /// <returns></returns>
        private async Task<IEnumerable<Entity>> GetStateData()
        {
            try
            {
                int attempt = 0;
                while (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() && attempt < 5)
                {
                    Debug.WriteLine($"{nameof(GetStateData)} no network connection availalbe. Attempt [{attempt}]. Time {DateTime.Now}");

                    await Task.Delay(100 + (attempt * 250));
                }

                Task<List<Entity>> task = WebRequests.GetData<List<Entity>>("api/states");

                return await task;
            }
            catch (Exception)
            {
                ShowErrorDialog("Connection Failure", $"Failed to connect to Home Assistant. URI attempted: {SettingsControl.HttpProtocol}://{SettingsControl.HomeAssistantHostname}:{SettingsControl.HomeAssistantPort}/api/states?apiPassword=[xxx]. Time {DateTime.Now.ToShortTimeString()}");
            }

            return null;
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
                    parentPanel.Children[indexOfElement] = panel;

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
                    { "entity_id", childrenEntities.Select(x => x.EntityId).ToList() },
                    { "order", 999 },
                    { "view", true },
            } };

            return CreateEntitiesInView(view, childrenEntities);
        }

        /// <summary>
        /// Create a header for the provided group view entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private Grid CreateHeaderTextBlockForGoupViewEntity(Entity entity)
        {
            Grid grid = new Grid();
            grid.Background = new SolidColorBrush(Colors.Black);
            grid.Background.Opacity = 0.4;
            grid.Padding = new Thickness(4);

            TextBlock textBlock = new TextBlock();
            textBlock.Text = Convert.ToString(entity.Attributes["friendly_name"]).ToUpper();
            textBlock.FontSize = 18;
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.Foreground = new SolidColorBrush(Colors.White);

            grid.Children.Add(textBlock);

            return grid;
        }

        /// <summary>
        /// Creates the main default view.
        /// </summary>
        /// <param name="allEntities"></param>
        /// <returns></returns>
        private StackPanel CreateViews(IEnumerable<Entity> allEntities)
        {
            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;
            stackPanel.HorizontalAlignment = HorizontalAlignment.Center;

            if (null != allEntities)
            {
                // Get all home assistant "group" entities which have the "view=true" attribute set in customizations.yaml
                IEnumerable<Entity> entityHeaders = allEntities.Where(group => group.Attributes.ContainsKey("view"));

                // Add all single and group entities which are tied to each view
                foreach (Entity entityHeader in entityHeaders)
                {
                    stackPanel.Children.Add(CreateHeaderTextBlockForGoupViewEntity(entityHeader));
                    stackPanel.Children.Add(CreateEntitiesInView(entityHeader, allEntities));
                }

                // Only add a header if we loaded user content as well
                Entity setupViewEntity = new Entity() { Attributes = new Dictionary<string, dynamic>() {
                    { "friendly_name", "settings" },
                    { "view", "yes" } } };

                stackPanel.Children.Add(CreateHeaderTextBlockForGoupViewEntity(setupViewEntity));
            }

            // Add the setup panels
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
                Entity entity = allEntities.FirstOrDefault(x => x.EntityId == groupEntityId);

                if (null == entity)
                {
                    ShowErrorDialog("Config Error", $"Unable to find entity '{groupEntityId}' in the '{entityHeader.EntityId}' group.");
                }
                else
                {
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
