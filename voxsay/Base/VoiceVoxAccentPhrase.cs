using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class VoiceVoxAccentPhrase
    {
        [DataMember]
        public VoiceVoxMora[] moras { get; set; }
        [DataMember]
        public int accent { get; set; }
        [DataMember]
        public VoiceVoxPauseMora pause_mora { get; set; }
    }


}
