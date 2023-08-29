using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class Coeiroinkv2StyleidToSpeakerMeta
    {
        [DataMember]
        public string speakerUuid;

        [DataMember]
        public string speakerName;

        [DataMember]
        public int styleId;

        [DataMember]
        public string styleName;
    }
}
