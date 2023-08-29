using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class VoiceVoxSpeakerStyle
    {
        [DataMember(Name="name")]
        public string Name { get; set; }

        [DataMember(Name="id")]
        public int Id { get; set; }
    }

}
