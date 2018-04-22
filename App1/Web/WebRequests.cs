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
        public static async Task<T> GetData<T>(string hostname, string apiAction, string apiPassword)
        {
            Uri uri = new Uri($"http://{hostname}/{apiAction}?{apiPassword}");

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            var response = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);

            Stream stream = response.GetResponseStream();

            StreamReader strReader = new StreamReader(stream);
            string text = await strReader.ReadToEndAsync();

            return JsonConvert.DeserializeObject<T>(text);
        }

        public static void SendActionNoData(string entityId)
        {
            WebRequests.SendData(MainPage.hostname, entityId.Split('.')[0], entityId.Split('.')[1], MainPage.apiPassword, string.Empty);
        }

        public static void SendAction(string entityId, string action)
        {
            string json = "{\"entity_id\":\"" + entityId + "\"}";

            WebRequests.SendData(MainPage.hostname, entityId.Split('.')[0], action, MainPage.apiPassword, json);
        }

        public static void SendAction(string domain, string action, Dictionary<string, string> data)
        {
            string json = "{" + string.Join(',', data.Select(x => "\"" + x.Key + "\":\"" + x.Value + "\"")) + "}";

            WebRequests.SendData(MainPage.hostname, domain, action, MainPage.apiPassword, json);
        }

        public static void SendAction(IEnumerable<string> entityIds, string action)
        {
            string json = "{\"entity_id\":[" + string.Join(',', entityIds.Select(x => "\"" + x + "\"").ToList()) + "]}";

            WebRequests.SendData(MainPage.hostname, entityIds.First().Split('.')[0], action, MainPage.apiPassword, json);
        }

        private static void SendData(string hostname, string domain, string action, string apiPassword, string data)
        {
            Task.Factory.StartNew(async () =>
            { 
                Uri uri = new Uri($"http://{hostname}/api/services/{domain}/{action}?{apiPassword}");

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

                Stream stream = response.GetResponseStream();

                StreamReader strReader = new StreamReader(stream);

                string text = await strReader.ReadToEndAsync();

                return;
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
