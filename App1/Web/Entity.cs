using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HashBoard
{
    [DataContract]
    public class Entity
    {
        [DataMember(Name = "state")]
        public string State { get; set; }

        [DataMember(Name = "entity_id")]
        public string EntityId { get; set; }

        [DataMember(Name = "last_changed")]
        public DateTime LastChanged { get; set; }

        [DataMember(Name = "last_updated")]
        public DateTime LastUpdated { get; set; }

        [DataMember(Name = "attributes")]
        public Dictionary<string, dynamic> Attributes { get; set; }


        /// <summary>
        /// Takes best effort guess to return an toggled state from current state.
        /// </summary>
        /// <returns></returns>
        public string GetToggledState()
        {
            switch (State)
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
                case "1":
                    return "0";
                case "0":
                    return "1";

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
