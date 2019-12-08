using Hashboard;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HashBoard
{
    public class MqttSubscriber
    {
        /// <summary>
        /// Callback delegate definitions.
        /// </summary>
        public delegate void OnEntityUpdated();
        public delegate void OnConnectionResult(bool isConnected);

        private readonly OnEntityUpdated OnEntityUpdatedCallback;
        private readonly OnConnectionResult OnConnectionResultCallback;

        /// <summary>
        /// MQTT Client.
        /// </summary>
        private readonly MqttFactory MqttFactory;
        private readonly IMqttClient MqttClient;

        /// <summary>
        /// Expected connection state. Used to determine if a disconnected session should attempt to reconnect
        /// when a disconnect occurs.
        /// </summary>
        private bool AttemptReconnectOnDisconnect = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MqttSubscriber(OnEntityUpdated onEntityUpdatedCallback, OnConnectionResult onConnectionResultCallback)
        {
            MqttFactory = new MqttFactory();
            MqttClient = MqttFactory.CreateMqttClient();

            OnEntityUpdatedCallback = onEntityUpdatedCallback ?? throw new ArgumentNullException(nameof(onEntityUpdatedCallback));
            OnConnectionResultCallback = onConnectionResultCallback ?? throw new ArgumentNullException(nameof(onConnectionResultCallback));

            // Handle message received callbacks
            MqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                string entityId = $"{e.ApplicationMessage.Topic.Split('/')[1]}.{e.ApplicationMessage.Topic.Split('/')[2]}";

                Telemetry.TrackTrace(
                    nameof(MqttClient.ApplicationMessageReceivedHandler),
                    new Dictionary<string, string>
                    {
                        [nameof(Entity.EntityId)] = entityId,
                    });

                OnEntityUpdatedCallback();
            });

            // Handle subscription connection
            MqttClient.UseConnectedHandler(async e =>
            {
                MqttClientSubscribeResult result = await MqttClient.SubscribeAsync(new TopicFilterBuilder()
                    .WithTopic(SettingsControl.MqttTopic)
                    .Build());

                Telemetry.TrackEvent(
                    nameof(MqttClient.ConnectedHandler), 
                    new Dictionary<string, string>
                    {
                        [nameof(MqttClient.IsConnected)] = MqttClient.IsConnected.ToString(),
                        [nameof(MqttClientSubscribeResultCode)] = result.Items.FirstOrDefault()?.ResultCode.ToString(),
                    });

                OnConnectionResultCallback(MqttClient.IsConnected);
            });

            // Handle disconnects
            MqttClient.UseDisconnectedHandler(async e =>
            {
                // Only attempt to reconnect when desired
                if (AttemptReconnectOnDisconnect)
                {
                    Telemetry.TrackEvent(
                        nameof(MqttClient.DisconnectedHandler),
                        new Dictionary<string, string>(e.ToDictionary())
                        {
                            [nameof(MqttClient.IsConnected)] = MqttClient.IsConnected.ToString(),
                        });

                    await Task.Delay(TimeSpan.FromSeconds(1));

                    await Connect();
                }
            });
        }

        /// <summary>
        /// Attempt to connect and subscribe to the MQTT broker.
        /// </summary>
        /// <param name="entities"></param>
        public async Task Connect()
        {
            if (string.IsNullOrEmpty(SettingsControl.MqttUsername))
            {
                throw new ArgumentNullException(nameof(SettingsControl.MqttUsername));
            }

            if (string.IsNullOrEmpty(SettingsControl.MqttPassword))
            {
                throw new ArgumentNullException(nameof(SettingsControl.MqttPassword));
            }

            if (string.IsNullOrEmpty(SettingsControl.MqttBrokerHostname))
            {
                throw new ArgumentNullException(nameof(SettingsControl.MqttBrokerHostname));
            }

            // Create TCP-based connection options
            IMqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(SettingsControl.MqttBrokerHostname)
                .WithCredentials(SettingsControl.MqttUsername, SettingsControl.MqttPassword)
                .WithCleanSession()
                .Build();

            await WebRequests.WaitForNetworkAvailable();

            // If we disconnect for any reason, attempt to reconnect
            AttemptReconnectOnDisconnect = true;

            while (!MqttClient.IsConnected)
            {
                try
                {
                    await MqttClient.ConnectAsync(mqttClientOptions);
                }
                catch (Exception e)
                {
                    Telemetry.TrackException(nameof(Connect), e);

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }

        /// <summary>
        /// Disconnect from the MQTT broker.
        /// </summary>
        /// <param name="entities"></param>
        public async Task Disconnect()
        {
            // Do not attempt to reconnect when we disconnect
            AttemptReconnectOnDisconnect = false;

            await MqttClient.DisconnectAsync();
        }
    }
}
