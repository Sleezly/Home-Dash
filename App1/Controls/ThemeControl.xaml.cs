using HashBoard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Hashboard
{
    public partial class ThemeControl : UserControl
    {
        public delegate Task OnBackgroundBrushChanged();
        private OnBackgroundBrushChanged BackgroundBrushChangedCallback;

        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private static StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        private readonly List<string> ThemeChoices = new List<string>()
        {
            "Dark",
            "Light",
        };

        private readonly List<string> ColorChoices = new List<string>()
        {
            "Navy",
            "MidnightBlue",
            "SlateGray",
            "DarkSlateBlue",
            "DarkSlateGray",
            "DarkOrange",
            "DarkRed",
            "DarkBlue",
            "DarkGreen",
            "DarkOrchid",
            "Maroon",
            "IndianRed",
            "Goldenrod",
            "Purple",
            "Brown",
        };

        private const string SelectNewImageFileText = "Select New";
        private const string BackgroundWavesBlue = "Blues Waves";
        private const string BackgroundWavesRed = "Red Waves";

        private List<string> AppDataBackgroundImages;

        private static readonly Dictionary<string, string> AppXBackgroundImages = new Dictionary<string, string>()
        {
            { BackgroundWavesBlue, "background-blue.jpg" },
            { BackgroundWavesRed, "background-red.png" },
        };

        private readonly List<string> BackgroundChoices = new List<string>()
        {
            SelectNewImageFileText,
            BackgroundWavesBlue,
            BackgroundWavesRed,
            "Black",
            "Gray",
            "SlateGray",
        };

        public static ElementTheme GetApplicationTheme()
        {
            switch (ApplicationTheme)
            {
                case "Light":
                    return ElementTheme.Light;

                case "Dark":
                default:
                    return ElementTheme.Dark;
            }
        }

        public ThemeControl(OnBackgroundBrushChanged backgroundBrushChangedCallback)
        {
            this.InitializeComponent();

            this.RequestedTheme = GetApplicationTheme();

            BackgroundBrushChangedCallback = backgroundBrushChangedCallback;

            InitializeUI();
        }

        private void InitializeUI()
        {
            ComboBox comboThemeStyle = this.FindName("ComboThemeStyle") as ComboBox;
            ComboBox comboAccentColorTheme = this.FindName("ComboAccentColor") as ComboBox;

            comboThemeStyle.DataContext = ThemeChoices;
            comboAccentColorTheme.DataContext = ColorChoices;

            if (null != ApplicationTheme)
            {
                comboThemeStyle.SelectedItem = ApplicationTheme;
            }
            else
            {
                comboThemeStyle.SelectedIndex = 0;
            }

            if (null != localSettings.Values["AccentColorBrush"] as string)
            {
                comboAccentColorTheme.SelectedItem = localSettings.Values["AccentColorBrush"] as string;
            }
            else
            {
                comboAccentColorTheme.SelectedIndex = 0;
            }

            PopulateBackgroundDropdown();
            
            comboThemeStyle.SelectionChanged += Combo_SelectionChanged;
            comboAccentColorTheme.SelectionChanged += Combo_SelectionChanged;

            comboAccentColorTheme.Background = AccentColorBrush;
        }

        private async void PopulateBackgroundDropdown()
        {
            IReadOnlyList<StorageFile> files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
            AppDataBackgroundImages = new List<string>();

            foreach (StorageFile file in files)
            {
                BackgroundChoices.Add(file.Name);
                AppDataBackgroundImages.Add(file.Name);
            }

            ComboBox comboBackgroundImage = this.FindName("ComboBackgroundImage") as ComboBox;

            comboBackgroundImage.DataContext = BackgroundChoices;

            if (null != localSettings.Values["AppDataBackgroundImageFileSelected"])
            {
                comboBackgroundImage.SelectedItem = localSettings.Values["AppDataBackgroundImageFileSelected"] as string;
            }
            else if (null != localSettings.Values["AppXBackgroundImageFileSelected"])
            {
                comboBackgroundImage.SelectedItem = AppXBackgroundImages.FirstOrDefault(x => x.Value == localSettings.Values["AppXBackgroundImageFileSelected"].ToString()).Key;
            }
            else if (null != localSettings.Values["SolidColorBackground"])
            {
                comboBackgroundImage.SelectedItem = localSettings.Values["SolidColorBackground"] as string;
            }
            else
            {
                comboBackgroundImage.SelectedItem = AppXBackgroundImages.FirstOrDefault(x => x.Value == AppXBackgroundImages[BackgroundWavesBlue].ToString()).Key;
            }

            comboBackgroundImage.SelectionChanged += Combo_SelectionChanged;
        }

        /// <summary>
        /// Theme Style.
        /// </summary>
        public static string ApplicationTheme
        {
            get
            {
                return localSettings.Values["ApplicationTheme"] as string;
            }
            set
            {
                localSettings.Values["ApplicationTheme"] = value;
            }
        }

        /// <summary>
        /// Accent color.
        /// </summary>
        public static SolidColorBrush AccentColorBrush
        {
            get
            {
                if (null != localSettings.Values["AccentColorBrush"])
                {
                    System.Drawing.Color color = System.Drawing.Color.FromName(localSettings.Values["AccentColorBrush"] as string);
                    return new SolidColorBrush(Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B));
                }
                else
                {
                    return new SolidColorBrush(Windows.UI.Color.FromArgb(255, Windows.UI.Colors.RoyalBlue.R, Windows.UI.Colors.RoyalBlue.G, Windows.UI.Colors.RoyalBlue.B));
                }
            }
        }

        /// <summary>
        /// Background image.
        /// </summary>
        public static Brush BackgroundBrush
        {
            get
            {
                if (null != localSettings.Values["AppDataBackgroundImageFileSelected"])
                {
                    return Imaging.LoadAppDataImageBrush(localSettings.Values["AppDataBackgroundImageFileSelected"] as string);
                }
                else if (null != localSettings.Values["AppXBackgroundImageFileSelected"])
                {
                    return Imaging.LoadAppXImageBrush(localSettings.Values["AppXBackgroundImageFileSelected"] as string);
                }
                else if (null != localSettings.Values["SolidColorBackground"])
                {
                    System.Drawing.Color color = System.Drawing.Color.FromName(localSettings.Values["SolidColorBackground"] as string);
                    return new SolidColorBrush(Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B)); ;
                }
                else
                {
                    return Imaging.LoadAppXImageBrush(AppXBackgroundImages[BackgroundWavesBlue]);
                }
            }
        }

        private void Combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            switch (comboBox.Name)
            {
                case "ComboThemeStyle":
                    ApplicationTheme = comboBox.SelectedItem as string;
                    RequestedTheme = GetApplicationTheme();

                    BackgroundBrushChangedCallback();
                    break;

                case "ComboAccentColor":
                    localSettings.Values["AccentColorBrush"] = comboBox.SelectedItem as string;
                    comboBox.Background = AccentColorBrush;

                    BackgroundBrushChangedCallback();
                    break;

                case "ComboBackgroundImage":
                    switch (comboBox.SelectedItem.ToString())
                    {
                        case SelectNewImageFileText:
                            // Select and save a new custom image
                            OpenFilePicker();
                            break;

                        case BackgroundWavesBlue:
                        case BackgroundWavesRed:
                            // Select a stock image stored in AppX folder
                            localSettings.Values["AppDataBackgroundImageFileSelected"] = null;
                            localSettings.Values["AppXBackgroundImageFileSelected"] = AppXBackgroundImages[comboBox.SelectedItem.ToString()];
                            localSettings.Values["SolidColorBackground"] = null;

                            BackgroundBrushChangedCallback();
                            break;

                        default:
                            if (AppDataBackgroundImages.Any(x => x == comboBox.SelectedItem.ToString()))
                            {
                                // Load a previously saved custom image store in application data folder
                                localSettings.Values["AppDataBackgroundImageFileSelected"] = comboBox.SelectedItem.ToString();
                                localSettings.Values["AppXBackgroundImageFileSelected"] = null;
                                localSettings.Values["SolidColorBackground"] = null;
                            }
                            else
                            {
                                // Select a solid color
                                localSettings.Values["AppDataBackgroundImageFileSelected"] = null;
                                localSettings.Values["AppXBackgroundImageFileSelected"] = null;
                                localSettings.Values["SolidColorBackground"] = comboBox.SelectedItem as string;
                            }

                            BackgroundBrushChangedCallback();
                            break;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async void OpenFilePicker()
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                localSettings.Values["AppDataBackgroundImageFileSelected"] = file.Name;
                localSettings.Values["AppXBackgroundImageFileSelected"] = null;
                localSettings.Values["SolidColorBackground"] = null;

                // Copy the file locally when not already present
                IStorageItem storageItem = await ApplicationData.Current.LocalFolder.TryGetItemAsync(file.Name);
                if (storageItem == null)
                {
                    StorageFile newFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(file.Name, CreationCollisionOption.FailIfExists);

                    await file.CopyAndReplaceAsync(newFile);
                }

                // Inform caller the background brush has changed
                await BackgroundBrushChangedCallback();
            }
        }
    }
}
