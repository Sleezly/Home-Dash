using Hashboard;
using HomeDash;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace HashBoard
{
    public class WebRequests
    {
        /// <summary>
        /// Waits for the network connection to become available.
        /// </summary>
        /// <returns></returns>
        public static async Task WaitForNetworkAvailable()
        {
            while (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                Telemetry.TrackEvent(nameof(WaitForNetworkAvailable));
            }
        }

        public static async Task<List<Entity>> GetData()
        {
            const string apiAction = @"api/states";
            const uint maxRetries = 3;

            Uri uri = new Uri($"{SettingsControl.HttpProtocol}://{SettingsControl.HomeAssistantHostname}:{SettingsControl.HomeAssistantPort}/{apiAction}");

            await WaitForNetworkAvailable();

            int attempt = 0;
            while (attempt < maxRetries)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
                    request.Headers.Add("Authorization", $"Bearer {Secrets.BearerToken}");

                    WebResponse webResponse = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);

                    Stream stream = webResponse.GetResponseStream();

                    StreamReader strReader = new StreamReader(stream);
                    string text = await strReader.ReadToEndAsync();

                    return JsonConvert.DeserializeObject<List<Entity>>(text);
                }
                catch
                {
                    Telemetry.TrackTrace($"{ nameof(GetData)} failed to get state data from HomeAssistant. Attempt [{attempt}]. Time {DateTime.Now}.");

                    await Task.Delay(100 + (attempt * 250));

                    attempt++;
                }
            }

            return new List<Entity>();
        }

        public static void SendActionNoData(string entityId)
        {
            WebRequests.SendData(entityId.Split('.')[0], entityId.Split('.')[1], string.Empty);
        }

        public static void SendAction(string entityId, string action)
        {
            string json = "{\"entity_id\":\"" + entityId + "\"}";

            WebRequests.SendData(entityId.Split('.')[0], action, json);
        }

        public static void SendAction(string action, IEnumerable<string> entityIds)
        {
            string json = "{\"entity_id\":[" + string.Join(',', entityIds.Select(x => "\"" + x + "\"").ToList()) + "]}";

            WebRequests.SendData(entityIds.First().Split('.')[0], action, json);
        }

        public static void SendAction(string domain, string action, Dictionary<string, string> data)
        {
            string json = "{" + ParseDictionaryToJson(data) + "}";

            WebRequests.SendData(domain, action, json);
        }

        public static void SendAction(string action, IEnumerable<string> entityIds, Dictionary<string, string> data)
        {
            string json = "{\"entity_id\":[" + string.Join(',', entityIds.Select(x => "\"" + x + "\"").ToList()) + "],";
            json += ParseDictionaryToJson(data) + "}";

            WebRequests.SendData(entityIds.First().Split('.')[0], action, json);
        }

        private static string ParseDictionaryToJson(Dictionary<string, string> data)
        {
            return string.Join(',',
                data.Select(x =>
                    double.TryParse(x.Value, out double integerValue) ?
                        ("\"" + x.Key + "\":" + x.Value) :          // No quotes around numeric data values
                        x.Value.Contains('[') ?
                            ("\"" + x.Key + "\":" + x.Value) :      // Arrays are provided custom by caller so don't add quotes
                    ("\"" + x.Key + "\":\"" + x.Value + "\"")       // Normal case is strings which will need quotes added
                ));
        }

        private static void SendData(string domain, string action, string data)
        {
            Task.Factory.StartNew(async () =>
            {
                Uri uri = new Uri($"{SettingsControl.HttpProtocol}://{SettingsControl.HomeAssistantHostname}:{SettingsControl.HomeAssistantPort}/api/services/{domain}/{action}");

                Telemetry.TrackTrace($"{nameof(SendData)} Uri:{SettingsControl.HttpProtocol}://{SettingsControl.HomeAssistantHostname}:{SettingsControl.HomeAssistantPort}/api/services/{domain}/{action} Json:{data}");

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
                request.Headers.Add("Authorization", $"Bearer {Secrets.BearerToken}");

                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                byte[] bytes = encoding.GetBytes(data);

                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = bytes.Length;

                // Send the data.
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(bytes, 0, bytes.Length);
                }

                var response = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
            });
        }
    }
}
