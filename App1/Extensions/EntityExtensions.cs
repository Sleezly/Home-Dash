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
        public static string GetToggledState(this Entity entity)
        {
            switch (entity.State)
            {
                case "true":
                    return "false";
                case "false":
                    return "true";
                case "on":
                    return "off";
                case "off":
                    return "on";
                case "playing":
                    return "paused";
                case "paused":
                    return "playing";
                case "idle":
                    return "playing";
                case "1":
                    return "0";
                case "0":
                    return "1";

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Takes best effort guess to return an toggled state from current state.
        /// </summary>
        /// <returns></returns>
        public static bool IsInOffState(this Entity entity)
        {
            switch (entity.State)
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
        /// Converts the 'supported_features' entity attribute.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static SupportedFeatures GetSupportedFeatures(this Entity entity, IEnumerable<Entity> childrenEntities = null)
        {
            // For group entities, scan the children entities
            if (childrenEntities != null)
            {
                if (childrenEntities.Any(x => x.GetSupportedFeatures() == SupportedFeatures.Colors))
                {
                    return SupportedFeatures.Colors;
                }
                else if (childrenEntities.Any(x => x.GetSupportedFeatures() == SupportedFeatures.ColorTemperature))
                {
                    return SupportedFeatures.ColorTemperature;
                }
                else if (childrenEntities.Any(x => x.GetSupportedFeatures() == SupportedFeatures.BrightnessOnly))
                {
                    return SupportedFeatures.BrightnessOnly;
                }

                throw new ArgumentException($"Group entity {entity.EntityId} does not reference any entities with a" +
                    "'supported_features' attribute.");
            }

            if (!entity.Attributes.ContainsKey("supported_features"))
            {
                throw new ArgumentException($"Entity {entity.EntityId} is missing 'supported_features' attribute.");
            }

            return (SupportedFeatures)Convert.ToInt32(entity.Attributes["supported_features"]);
        }
    }
}
