using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class VoiceVoxMora
    {
        [DataMember]
        public string text { get; set; }
        [DataMember]
        public string consonant { get; set; }
        [DataMember]
        public double? consonant_length { get; set; }
        [DataMember]
        public string vowel { get; set; }
        [DataMember]
        public double? vowel_length { get; set; }
        [DataMember]
        public double? pitch { get; set; }
    }

}
