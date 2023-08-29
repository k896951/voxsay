using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class Coeiroinkv2AccentPhrase
    {
        [DataMember]
        public Coeiroinkv2Mora[] moras { get; set; }

        [DataMember]
        public int accent { get; set; }

        [DataMember(Name = "pauseMora")]
        public VoiceVoxPauseMora pause_mora { get; set; }

        [DataMember]
        public bool isInterrogative { get; set; }
    }


}
