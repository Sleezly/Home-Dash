using HashBoard;
using System;
using System.Collections.Generic;

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
            // Due to HomeAssistant rework of the Climate entity it's no longer possible to 
            // differentiate between an 'off' and 'eco' climate control state. So let's just always
            // assume the control is 'On' so that the control uses 'eco' mode panel color blending.
            if (entity.EntityId.StartsWith("climate."))
            {
                return false;
            }

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
        /// Determines if the entity is unavailable or not.
        /// </summary>
        /// <returns></returns>
        public static bool IsUnavailable(this Entity entity)
        {
            switch (entity.State.ToLower())
            {
                case "unavailable":
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the name of the Entity object.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <returns>Name</returns>
        public static string Name(this Entity entity)
        {
            return entity.Attributes.ContainsKey("friendly_name") ?
                entity.Attributes["friendly_name"] ??
                entity.EntityId.Split(".")[1].Replace("_", " ") :
                entity.EntityId.Split(".")[1].Replace("_", " ");
        }

        /// <summary>
        /// Returns and RGB calculation of current color for this light bulb entity. Only applies if light-specific
        /// attributes are present.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static RGB GetColor(this Entity entity)
        {
            return 
                entity.GetColorRgb() ?? 
                entity.GetColorTemperature() ??
                entity.GetColorDefault();
        }

        public static RGB GetColorRgb(this Entity entity)
        {
            if (entity.Attributes.ContainsKey("rgb_color"))
            {
                byte r = Convert.ToByte(entity.Attributes["rgb_color"][0]);
                byte g = Convert.ToByte(entity.Attributes["rgb_color"][1]);
                byte b = Convert.ToByte(entity.Attributes["rgb_color"][2]);

                return new RGB(r, g, b);
            }

            return null;
        }

        public static RGB GetColorTemperature(this Entity entity)
        {
            if (entity.Attributes.ContainsKey("color_temp"))
            {
                int colorTemperature = Convert.ToInt32(entity.Attributes["color_temp"]);
                return ColorConverter.MiredToRGB(colorTemperature);
            }

            return null;
        }

        public static RGB GetColorDefault(this Entity entity)
        {
            // Default to 2700 Kelvin
            return ColorConverter.MiredToRGB(370);
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
