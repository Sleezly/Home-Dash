using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
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

        private DateTime MousePressStartTime;

        private List<PanelBuilderBase> CustomEntities;

        private CancellationTokenSource cancellationTokenSource;

        //private CancellationTokenSource cancellationTokenSourceContentDialog;

        public MainPage()
        {
            this.InitializeComponent();

            LoadEntityHandler();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await LoadFrame();
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

                new GenericPanelBuilder() { EntityIdStartsWith = string.Empty },
            };
        }

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
                            popupContent = new Hashboard.LightControl(panelData.Entity);
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

            if (MousePressStartTime + TimeSpan.FromSeconds(5) > now &&
                MousePressStartTime + TimeSpan.FromMilliseconds(500) < now)
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

        private void SendPanelDataWithJson(Panel panel, PanelData panelData)
        {
            if (panelData.ChildrenEntities != null)
            {
                WebRequests.SendAction(
                    panelData.ChildrenEntities.Select(x => x.EntityId), 
                    panelData.ServiceToInvokeOnTap);
            }
            else
            {
                WebRequests.SendAction(panelData.Entity.EntityId, panelData.ServiceToInvokeOnTap);
            }

            panelData.Entity.State = GetToggledState(panelData.Entity);
            panelData.Entity.LastUpdated = DateTime.Now;

            UpdateChildPanelIfneeded(panel, new List<Entity>() { panelData.Entity });
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
        private void UpdateChildPanelIfneeded(FrameworkElement element, List<Entity> allEntities)
        {
            PanelData panelData = PanelData.GetPanelData(element);
            
            // Update this control
            if (panelData != null)
            {
                Debug.WriteLine($"{nameof(UpdateChildPanelIfneeded)} checking {panelData.Entity.EntityId}.");

                // Get this entity
                Entity entity = allEntities.First(x => x.EntityId == panelData.Entity.EntityId);

                // Check if we need to update the dashboard with fresh data
                if (entity.LastUpdated > panelData.LastDashboardtaUpdate)
                {
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

            //TextBlock textBlock = new TextBlock();
            //textBlock.Text = entityHeader.Attributes["friendly_name"];

            //wrapPanel.Children.Add(textBlock);

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

        //private Panel CreateHubSectionGroup(Entity groupEntity, IEnumerable<Entity> allEntities)
        //{
            //WrapPanel wrapPanel = new WrapPanel();
            //wrapPanel.Orientation = Orientation.Vertical;

            //TextBlock textBlock = new TextBlock();
            //textBlock.Text = groupEntity.Attributes["friendly_name"];

            //wrapPanel.Children.Add(textBlock);

            //return CreateGroupEntityPanel(groupEntity, allEntities);

            //if (customEntity != null)
            //{
            //    IEnumerable<Entity> childrenEntities = allStates.Where(s => entityIds.Any(e => e.ToString() == s.EntityId));

            //    //Panel panel = CreateGroupEntityPanel(entity, childrenEntities);
            //    Panel panel = customEntity.CreateGroupPanel(entity, childrenEntities);

            //    if (panel != null)
            //    {
            //        actionPanel.Children.Add(panel);
            //    }
            //}
            //return wrapPanel;
        //}

        //private StackPanel CreateTopPanel(IEnumerable<Entity> states)
        //{
        //    StackPanel stackPanel = new StackPanel();
        //    stackPanel.BorderThickness = new Thickness(1, 1, 1, 1);
        //    stackPanel.Orientation = Orientation.Horizontal;
        //    stackPanel.Background = new SolidColorBrush(Colors.LightBlue);

        //    foreach (Entity state in states)
        //    {
        //        TextBlock textBlock = new TextBlock();
        //        textBlock.Text = state.Attributes["friendly_name"];

        //        stackPanel.Children.Add(textBlock);
        //    }

        //    return stackPanel;
        //}

        //private WrapPanel CreateActionPanel(IEnumerable<Entity> allGroups, IEnumerable<Entity> allStates)
        //{
        //    WrapPanel actionPanel = new WrapPanel();
        //    actionPanel.Orientation = Orientation.Horizontal;
        //    actionPanel.HorizontalAlignment = HorizontalAlignment.Center;

        //    foreach (Entity group in allGroups)
        //    {
                //Newtonsoft.Json.Linq.JArray entityIds = entity.Attributes["entity_id"];

                //PanelBuilderBase customEntity = CustomEntities.FirstOrDefault(x =>
                //    entityIds.Any(y => y.ToString().StartsWith(x.EntityIdStartsWith)));

                //if (customEntity != null)
                //{
                //    IEnumerable<Entity> childrenEntities = allStates.Where(s => entityIds.Any(e => e.ToString() == s.EntityId));

                //    //Panel panel = CreateGroupEntityPanel(entity, childrenEntities);
                //    Panel panel = customEntity.CreateGroupPanel(entity, childrenEntities);

                //    if (panel != null)
                //    {
                //        actionPanel.Children.Add(panel);
                //    }
                //}

                /*
                Panel panel = CreateGroupEntityPanel(entity, allStates);

                if (panel != null)
                {
                    actionPanel.Children.Add(panel);
                }

                // Children
                foreach (string entityId in entity.Attributes["entity_id"])
                {
                    Entity childEntity = allStates.First(s => s.EntityId == entityId);

                    Panel childPanel = CreateChildEntityPanel(childEntity);

                    if (childPanel != null)
                    {
                        actionPanel.Children.Add(childPanel);
                    }
                }
                */
       //     }
       //
       //     return actionPanel;
       // }

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
