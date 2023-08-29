using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class Coeiroinkv2Mora
    {
        [DataMember]
        public string text { get; set; }

        [DataMember]
        public string consonant { get; set; }

        [DataMember(Name = "consonantLength")]
        public double? consonant_length { get; set; }

        [DataMember]
        public string vowel { get; set; }

        [DataMember(Name = "vowelLength")]
        public double? vowel_length { get; set; }

        [DataMember]
        public double? pitch { get; set; }
    }

}
