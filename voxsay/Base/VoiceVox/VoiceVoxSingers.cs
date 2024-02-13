using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class VoiceVoxSingers
    {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string speaker_uuid { get; set; }
        [DataMember]
        public VoiceVoxSpeakerStyle[] styles { get; set; }
        [DataMember]
        public string version { get; set; }
    }
}
