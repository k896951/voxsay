using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class Coeiroinkv2Speaker
    {
        [DataMember( Name = "speakerName")]
        public string name { get; set; }

        [DataMember( Name = "speakerUuid")]
        public string speaker_uuid { get; set; }

        [DataMember]
        public Coeiroinkv2SpeakerStyle[] styles { get; set; }

        //[DataMember]
        //public string version { get; set; }
    }
}
