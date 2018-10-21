using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Hashboard
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Uses reflection to itemize all property and value pairs in to a dictionary(string,string). Useful for diagnostics, incident generation, etc.
        /// </summary>
        /// <returns>Dictionary of property and value KeyValuePairs</returns>
        public static Dictionary<string, string> ToDictionary<T>(this T myObject)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();

            foreach (PropertyInfo propertyInfo in myObject.GetType().GetProperties())
            {
                // Only serialize public properties
                if (propertyInfo.PropertyType.IsPublic)
                {
                    string value = propertyInfo.GetValue(myObject)?.ToString();

                    if (string.IsNullOrEmpty(value))
                    {
                        properties[propertyInfo.Name] = $"Null {propertyInfo.Name}";
                    }
                    else
                    {
                        DisplayNameAttribute displayNameAttribute = (DisplayNameAttribute)propertyInfo.GetCustomAttribute(typeof(DisplayNameAttribute));

                        if (null != displayNameAttribute)
                        {
                            properties[displayNameAttribute.DisplayName] = value;
                        }
                        else
                        {
                            properties[propertyInfo.Name] = value;
                        }
                    }
                }
            }

            return properties;
        }
    }
}