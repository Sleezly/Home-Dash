using Hashboard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace HashBoard
{
    public class MqttSubscriber
    {
        private MqttClient client;

        public delegate void OnEntityUpdated(string entityId);
        public delegate void OnConnectionResult(byte connectionResponse);

        private OnEntityUpdated EntityUpdatedCallback { get; set; }

        private OnConnectionResult OnConnectCallback { get; set; }

        private Dictionary<string, Dictionary<string, string>> topics = new Dictionary<string, Dictionary<string, string>>();

        private Dictionary<byte, string> ConnackResponseCodes = new Dictionary<byte, string>()
        {
            { 0, "Connection accepted." },
            { 1, "The Server does not support the level of the MQTT protocol requested by the Client." },
            { 2, "The Client identifier is correct UTF-8 but not allowed by the Server." },
            { 3, "The Network Connection has been made but the MQTT service is unavailable." },
            { 4, "The data in the user name or password is malformed." },
            { 5, "The Client is not authorized to connect." },
        };

        public bool IsSubscribed{ get; set; } 
        public string Status { get; set; }

        public MqttSubscriber(OnEntityUpdated onEntityUpdatedCallback, OnConnectionResult onConnectionResultCallback)
        {
            IsSubscribed = false;
            Status = "No connection has been attempted.";

            EntityUpdatedCallback = onEntityUpdatedCallback;
            OnConnectCallback = onConnectionResultCallback;
        }

        /// <summary>
        /// Attempt to connect and subscribe to the MQTT broker.
        /// </summary>
        /// <param name="entities"></param>
        public void Connect()
        {
            if (!IsSubscribed)
            {
                Task.Factory.StartNew(() =>
                {
                    client = new MqttClient(SettingsControl.MqttBrokerHostname);

                    client.MqttMsgPublishReceived += OnMessageReceviedWorker;

                    byte response = client.Connect(Guid.NewGuid().ToString(), SettingsControl.MqttUsername, SettingsControl.MqttPassword);

                    if (response == 0)
                    {
                        client.Subscribe(new string[] { SettingsControl.MqttStateStream }, new byte[] { 2 });

                        IsSubscribed = true;
                    }

                    Status = ConnackResponseCodes[response];

                    OnConnectCallback(response);
                });
            }
        }

        /// <summary>
        /// Disconnect from the MQTT broker.
        /// </summary>
        public void Disconnect()
        {
            if (IsSubscribed)
            {
                client.Disconnect();

                IsSubscribed = false;
            }
        }

        /// <summary>
        /// MQTT subscriber event notification received callback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageReceviedWorker(object sender, MqttMsgPublishEventArgs e)
        {
            //string message = Encoding.UTF8.GetString(e.Message);
            //string entityId = $"{e.Topic.Split('/')[1]}.{e.Topic.Split('/')[2]}";

            EntityUpdatedCallback(string.Empty);
        }
    }
}
