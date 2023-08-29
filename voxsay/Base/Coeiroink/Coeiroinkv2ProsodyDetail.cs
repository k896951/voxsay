using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class Coeiroinkv2ProsodyDetail
    {
        [DataMember]
        public string phoneme;

        [DataMember]
        public string hira;

        [DataMember]
        public int? accent;
    }
}
