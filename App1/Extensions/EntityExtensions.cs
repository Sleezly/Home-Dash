using HashBoard;
using System;
using System.Collections.Generic;
using System.Linq;
using static HashBoard.Entity;

namespace Hashboard
{
    public static class EntityExtensions
    {
        /// <summary>
        /// Takes best effort guess to return an toggled state from current state.
        /// </summary>
        /// <returns></returns>
        public static bool IsInOffState(this Entity entity)
        {
            switch (entity.State.ToLower())
            {
                case "false":
                case "off":
                case "paused":
                case "0":
                case "idle":
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns and RGB calculation of current color for this light bulb entity. Only applies if light-specific
        /// attributes are present.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static RGB GetColor(this Entity entity)
        {
            if (entity.Attributes.ContainsKey("rgb_color"))
            {
                byte r = Convert.ToByte(entity.Attributes["rgb_color"][0]);
                byte g = Convert.ToByte(entity.Attributes["rgb_color"][1]);
                byte b = Convert.ToByte(entity.Attributes["rgb_color"][2]);

                return new RGB(r, g, b);
            }
            else if (entity.Attributes.ContainsKey("color_temp"))
            {
                int colorTemperature = Convert.ToInt32(entity.Attributes["color_temp"]);
                return ColorConverter.MiredToRGB(colorTemperature);
            }
            else
            {
                // Default to 2700 Kelvin
                return ColorConverter.MiredToRGB(370);
            }
        }

        /// <summary>
        /// Checks if this entity supports the requested feature.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool HasSupportedFeatures(this Entity entity, uint supportedFeatures, IEnumerable<Entity> childrenEntities = null)
        {
            return (GetSupportedFeatures(entity, childrenEntities) & supportedFeatures) == supportedFeatures;
        }

        /// <summary>
        /// Gets the 'supported_features' entity attribute.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static uint GetSupportedFeatures(this Entity entity, IEnumerable<Entity> childrenEntities = null)
        {
            // For group entities also scan the children entities
            uint supportedFeatures = 0;

            if (null != childrenEntities)
            {
                foreach (Entity child in childrenEntities)
                {
                    supportedFeatures |= GetSupportedFeatures(child, null);
                }
            }

            return supportedFeatures | Convert.ToUInt32(entity.Attributes["supported_features"]);
        }
    }
}
