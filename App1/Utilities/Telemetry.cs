using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
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
            Analytics.TrackEvent(key, properties);
        }

        public static void TrackTrace(string message, IDictionary<string, string> properties = null)
        {
            Debug.WriteLine($"{message} {(null != properties ? string.Join(", ", properties) : string.Empty)}");
        }

        public static void TrackException(string method, Exception ex)
        {
            if (null != ex)
            {
                Analytics.TrackEvent(method, ex.ToDictionary());
                Crashes.TrackError(ex, ex.ToDictionary());
            }
            else
            {
                Analytics.TrackEvent(method);
            }
        }
    }
}
