using HashBoard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashboard
{
    public static class EntityExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        public enum SupportedFeatures
        {
            Colors = 63,
            ColorTemperature = 43,
            BrightnessOnly = 41,
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
