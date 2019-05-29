using Microsoft.HockeyApp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hashboard
{
    public static class Telemetry
    {
        public static void TrackEvent(string key, IDictionary<string, string> properties = null)
        {
            Debug.WriteLine($"{key} {(null != properties ? string.Join(", ", properties) : string.Empty)}");
            HockeyClient.Current.TrackEvent(key, properties);
        }

        public static void TrackTrace(string message, IDictionary<string, string> properties = null)
        {
            Debug.WriteLine($"{message} {(null != properties ? string.Join(", ", properties) : string.Empty)}");
        }

        public static void TrackException(string method, Exception ex)
        {
            if (null != ex)
            {
                HockeyClient.Current.TrackException(ex);
                HockeyClient.Current.TrackEvent($"Exception: {method}", ex.ToDictionary());
            }
            else
            {
                HockeyClient.Current.TrackEvent(method);
            }
        }
    }
}
