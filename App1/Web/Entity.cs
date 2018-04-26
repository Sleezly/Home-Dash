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
    }
}
