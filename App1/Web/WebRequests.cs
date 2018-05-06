using Hashboard;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace HashBoard
{
    public class WebRequests
    {
        private const string ApiPassword = "api_password";

        public static async Task<T> GetData<T>(string apiAction)
        {
            Uri uri = new Uri($"{SettingsControl.HttpProtocol}://{SettingsControl.HomeAssistantHostname}:{SettingsControl.HomeAssistantPort}/{apiAction}?{ApiPassword}={SettingsControl.HomeAssistantPassword}");

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            var response = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);

            Stream stream = response.GetResponseStream();

            StreamReader strReader = new StreamReader(stream);
            string text = await strReader.ReadToEndAsync();

            return JsonConvert.DeserializeObject<T>(text);
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
            //json += string.Join(",", data.Select(x => "\"" + x.Key + "\":\"" + x.Value + "\"").ToList()) + "}";
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
                Uri uri = new Uri($"{SettingsControl.HttpProtocol}://{SettingsControl.HomeAssistantHostname}:{SettingsControl.HomeAssistantPort}/api/services/{domain}/{action}?{ApiPassword}={SettingsControl.HomeAssistantPassword}");

                Debug.WriteLine($"{nameof(SendData)} Uri:{SettingsControl.HttpProtocol}://{SettingsControl.HomeAssistantHostname}:{SettingsControl.HomeAssistantPort}/api/services/{domain}/{action}?{ApiPassword}=[xxxx] Json:{data}");

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);

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

                //Stream stream = response.GetResponseStream();

                //StreamReader strReader = new StreamReader(stream);

                //string text = await strReader.ReadToEndAsync();

                //return;
            });
            //request.BeginGetResponse((x) =>
            //{
            //    using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(x))
            //    {
            //        Stream webStream = response.GetResponseStream();

            //        StreamReader responseReader = new StreamReader(webStream);

            //        string text = responseReader.ReadToEnd();
            //    }
            //}, null);

        }
    }
}
