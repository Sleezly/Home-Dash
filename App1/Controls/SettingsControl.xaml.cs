using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hashboard
{
    public partial class SettingsControl : UserControl
    {
        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private static StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        public SettingsControl()
        {
            this.InitializeComponent();

            this.RequestedTheme = ThemeControl.GetApplicationTheme();

            if (null != HttpProtocol)
            {
                TextBox homeAssistantProtocol = this.FindName("HomeAssistantProtocolText") as TextBox;
                homeAssistantProtocol.Text = HttpProtocol;
            }
            if (null != HomeAssistantHostname)
            {
                TextBox homeAssistantHostname = this.FindName("HomeAssistantHostnameText") as TextBox;
                homeAssistantHostname.Text = HomeAssistantHostname;
            }
            if (null != HomeAssistantPort)
            {
                TextBox homeAssistantPort = this.FindName("HomeAssistantPortText") as TextBox;
                homeAssistantPort.Text = HomeAssistantPort;
            }
            if (null != HomeAssistantPassword)
            {
                PasswordBox homeAssistantPassword = this.FindName("HomeAssistantPasswordText") as PasswordBox;
                homeAssistantPassword.Password = HomeAssistantPassword;
            }
            if (null != HomeAssistantPollingInterval)
            {
                TextBox homeAssistantPollingInterval = this.FindName("PollingIntervalText") as TextBox;
                homeAssistantPollingInterval.Text = HomeAssistantPollingInterval.TotalSeconds.ToString();

                if (HomeAssistantPollingInterval == default(TimeSpan))
                {
                    homeAssistantPollingInterval.Text += " (zero for disabled)";
                }
            }
            if (null != MqttBrokerHostname)
            {
                TextBox mqttBrokerHostname = this.FindName("MqttBrokerHostnameText") as TextBox;
                mqttBrokerHostname.Text = MqttBrokerHostname;
            }
            if (null != MqttUsername)
            {
                TextBox mqttUsername = this.FindName("MqttUsernameText") as TextBox;
                mqttUsername.Text = MqttUsername;
            }
            if (null != MqttPassword)
            {
                PasswordBox mqttPassword = this.FindName("MqttPasswordText") as PasswordBox;
                mqttPassword.Password = MqttPassword;
            }
            if (null != MqttStateStream)
            {
                TextBox mqttStateStream = this.FindName("MqttStateStreamText") as TextBox;
                mqttStateStream.Text = MqttStateStream;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TextBox homeAssistantProtocol = this.FindName("HomeAssistantProtocolText") as TextBox;
            TextBox homeAssistantHostname = this.FindName("HomeAssistantHostnameText") as TextBox;
            TextBox homeAssistantPort = this.FindName("HomeAssistantPortText") as TextBox;
            PasswordBox homeAssistantPassword = this.FindName("HomeAssistantPasswordText") as PasswordBox;
            TextBox pollingInterval = this.FindName("PollingIntervalText") as TextBox;
            TextBox mqttBrokerHostname = this.FindName("MqttBrokerHostnameText") as TextBox;
            TextBox mqttUsername = this.FindName("MqttUsernameText") as TextBox;
            PasswordBox mqttPassword = this.FindName("MqttPasswordText") as PasswordBox;
            TextBox mqttStateStream = this.FindName("MqttStateStreamText") as TextBox;

            HttpProtocol = homeAssistantProtocol.Text;
            HomeAssistantHostname = homeAssistantHostname.Text;
            HomeAssistantPort = homeAssistantPort.Text;
            HomeAssistantPassword = homeAssistantPassword.Password;
            MqttBrokerHostname = mqttBrokerHostname.Text;
            MqttUsername = mqttUsername.Text;
            MqttPassword = mqttPassword.Password;
            MqttStateStream = mqttStateStream.Text;

            if (Int32.TryParse(pollingInterval.Text, out int interval))
            {
                HomeAssistantPollingInterval = TimeSpan.FromSeconds(Math.Max(0, interval));
            }
            else
            {
                HomeAssistantPollingInterval = default(TimeSpan);                
            }
        }
        
        /// <summary>
        /// Connection protocol for communicating with the home assistant server. This is either http or https.
        /// </summary>
        public static string HttpProtocol
        {
            get
            {
                return localSettings.Values["HomeAssistantProtocol"] as string;
            }
            private set
            {
                localSettings.Values["HomeAssistantProtocol"] = value;
            }
        }

        /// <summary>
        /// IP Address od the Home Assistant server.
        /// </summary>
        public static string HomeAssistantHostname
        {
            get
            {
                return localSettings.Values["HomeAssistantHostname"] as string;
            }
            private set
            {
                localSettings.Values["HomeAssistantHostname"] = value;
            }
        }

        /// <summary>
        /// Port for the Home Assistant server.
        /// </summary>
        public static string HomeAssistantPort
        {
            get
            {
                return localSettings.Values["HomeAssistantPort"] as string;
            }
            private set
            {
                localSettings.Values["HomeAssistantPort"] = value;
            }
        }

        /// <summary>
        /// Api Password for the Home Assistant server.
        /// </summary>
        public static string HomeAssistantPassword
        {
            get
            {
                return localSettings.Values["HomeAssistantPassword"] as string;
            }
            private set
            {
                localSettings.Values["HomeAssistantPassword"] = value;
            }
        }

        /// <summary>
        /// Frequency to request state data from Home Assistant.
        /// </summary>
        public static TimeSpan HomeAssistantPollingInterval
        {
            get
            {
                if (localSettings.Values.ContainsKey("HomeAssistantPollingInterval"))
                {
                    return TimeSpan.FromSeconds((double)localSettings.Values["HomeAssistantPollingInterval"]);
                }
                else
                {
                    return default(TimeSpan);
                }
            }
            private set
            {
                localSettings.Values["HomeAssistantPollingInterval"] = value.TotalSeconds;
            }
        }

        /// <summary>
        /// MQTT hostname of the Broker. This is typically 'hassio.local' if running the embedded MQTT broker from home assistant.
        /// </summary>
        public static string MqttBrokerHostname
        {
            get
            {
                return localSettings.Values["MqttBrokerHostname"] as string;
            }
            private set
            {
                localSettings.Values["MqttBrokerHostname"] = value;
            }
        }

        /// <summary>
        /// MQTT username for the Broker. This is typically 'homeassistant'.
        /// </summary>
        public static string MqttUsername
        {
            get
            {
                return localSettings.Values["MqttUsername"] as string;
            }
            private set
            {
                localSettings.Values["MqttUsername"] = value;
            }
        }

        /// <summary>
        /// MQTT broker password. This is typically the same as the Home Assistant ApiPassword.
        /// </summary>
        public static string MqttPassword
        {
            get
            {
                return localSettings.Values["MqttPassword"] as string;
            }
            private set
            {
                localSettings.Values["MqttPassword"] = value;
            }
        }

        /// <summary>
        /// Topic name for the MQTT state stream. Typically this is 'homeassistant/#'.
        /// </summary>
        public static string MqttStateStream
        {
            get
            {
                return localSettings.Values["MqttStateStream"] as string;
            }
            private set
            {
                localSettings.Values["MqttStateStream"] = value;
            }
        }
    }
}
