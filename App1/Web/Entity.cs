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
        /// homeassistant/components/light/__init__.py
        /// </summary>
        public enum LightPlatformSupportedFeatures
        {
            Brightness = 1,
            ColorTemperature = 2,
            Effect = 4,
            Flash = 8,
            Color = 16,
            Transition = 32,
            WhiteValue = 128,
        }

        /// <summary>
        /// homeassistant/components/media_player/__init__.py
        /// </summary>
        public enum MediaPlatformSupportedFeatures
        {
            Pause = 1,
            Seek = 2,
            VolumeSet = 4,
            VolumeMute = 8,
            PreviousTack = 16,
            NextTrack = 32,

            TurnOn = 128,
            TurnOff = 256,
            PlayMedia = 512,
            VolumeStep = 1024,
            SelectSource = 2048,
            Stop = 4096,
            ClearPlaylist = 8192,
            Play = 16384,
            ShuffleSet = 32768
        }
    }
}
