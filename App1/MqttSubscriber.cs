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

                    client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

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

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Message);

            OnMessageReceviedWorker(e.Topic, message);
        }

        private void OnMessageReceviedWorker(string topic, string message)
        {
            string entityId = $"{topic.Split('/')[1]}.{topic.Split('/')[2]}";

            Debug.WriteLine($"MQTT:  {entityId} - {message}");

            EntityUpdatedCallback(entityId);
        }

        /*
        private void OnMessageReceviedWorker2(string topic, string message)
        {
            bool updateEntity = false;
            Entity entity = null;

            lock (topics)
            {
                // Get the EntityId from the MQTT topic which is in the format of: 'homeassistant/[entity type]/[entity name]/[attributes]'
                string entityId = $"{topic.Split('/')[1]}.{topic.Split('/')[2]}";
                string action = topic.Split('/')[3];

                if (topics.ContainsKey(entityId))
                {
                    if (!topics[entityId].ContainsKey(action))
                    {
                        // Remove all quotes 
                        topics[entityId].Add(action, message.Replace("\"", ""));
                    }
                }
                else
                {
                    topics.Add(entityId, new Dictionary<string, string>() { { action, message } });
                }

                entity = allEntities.FirstOrDefault(x => x.EntityId == entityId);

                if (entity == null)
                {
                    // This is a new entity which we're not tracking. Need to restart to address for now ...
                    throw new NotImplementedException("A new entity has been added. Please restart the dashboard.");
                }

                Debug.WriteLine($"MQTT:  {entity.EntityId} - {action} - {message} - entity has {entity.Attributes.Count()}");

                // Check if we've received all updated values for this entity or not
                if (false && action.Equals("last_updated", StringComparison.InvariantCultureIgnoreCase))
                {
                    //string values = string.Empty;
                    foreach (KeyValuePair<string, string> fields in topics[entityId])
                    {
                        //values += $"- {fields.Key} : {fields.Value.ToString()}\n";
                        switch (fields.Key)
                        {
                            case "state":
                                entity.State = fields.Value;
                                break;

                            case "last_changed":
                                entity.LastChanged = Convert.ToDateTime(fields.Value);
                                break;

                            case "last_updated":
                                entity.LastUpdated = Convert.ToDateTime(fields.Value);
                                break;

                            default:
                                if (entity.Attributes.ContainsKey(fields.Key))
                                {
                                    // Aarray attributes should be split apart
                                    if (fields.Value.Contains("["))
                                    {
                                        entity.Attributes[fields.Key] = fields.Value.Replace("[", "").Replace("]", "").Replace(" ", "").Split(",");
                                    }
                                    else
                                    {
                                        entity.Attributes[fields.Key] = fields.Value;
                                    }
                                }
                                break;
                        }
                    }

                    //Debug.WriteLine($"MQTT updating entity: {entity.EntityId}\n" + values);
                    

                    // Parsed all updated data so no need to hold on to it any longer. Simply remove.
                    //topics.Remove(entityId);

                   // updateEntity = true;
                }
            }

            //Debug.WriteLine($"Updating: {entityId}");

            //if (updateEntity)
            //{
            //    EntityUpdatedCallback(entity);
            //}
        }
        */
    }
}
