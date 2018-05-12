using HashBoard;
using System.Collections.Generic;
using System.Linq;

namespace Hashboard
{
    public class PanelTouchHandler
    {
        public enum ResponseExpected
        {
            EntityUpdated,
            None,
        };

        public ResponseExpected Response { get; }

        private Dictionary<uint, string> ServiceActionForSupportedFeatureMap { get; }

        /// <summary>
        /// Returns the HomeAssistant Service Action to invoke based on the given entity's 'supported_features' attribute.
        /// For example, if a media_player supports PlayMedia, service action to return could be 'media_play_pause'. If the 
        /// media_player doesn't support play but does support TurnOn, then service action could be 'turn_on' or 'toggle'.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public string GetServiceAction(Entity entity)
        {
            if (ServiceActionForSupportedFeatureMap.Count == 0)
            {
                return string.Empty;
            }

            if (ServiceActionForSupportedFeatureMap.Count == 1)
            {
                return ServiceActionForSupportedFeatureMap.First().Value;
            }

            return ServiceActionForSupportedFeatureMap.SingleOrDefault(x => entity.HasSupportedFeatures(x.Key)).Value;
        }

        public PanelTouchHandler(string serviceAction, ResponseExpected responseExpected)
        {
            ServiceActionForSupportedFeatureMap = new Dictionary<uint, string>() { { 0, serviceAction } };
            Response = responseExpected;
        }

        public PanelTouchHandler(Dictionary<uint, string> serviceAction, ResponseExpected responseExpected)
        {
            ServiceActionForSupportedFeatureMap = serviceAction;
            Response = responseExpected;
        }
    }
}
