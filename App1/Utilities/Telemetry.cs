using Microsoft.HockeyApp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hashboard
{
    public static class Telemetry
    {
        public static void TrackEvent(string key, Dictionary<string, string> properties = null)
        {
            Debug.WriteLine($"{key} {properties?.ToString()}");
            HockeyClient.Current.TrackEvent(key, properties);
        }

        public static void TrackTrace(string message, Dictionary<string, string> properties = null)
        {
            Debug.WriteLine($"{message} {properties?.ToString()}");
        }

        public static void TrackException(string method, Exception ex)
        {
            if (null != ex)
            {
                HockeyClient.Current.TrackException(ex);
                HockeyClient.Current.TrackEvent(method, ex.ToDictionary());
            }
            else
            {
                HockeyClient.Current.TrackEvent(method);
            }
        }
    }
}
