using System.Collections.Generic;
using System.Runtime.Serialization;

namespace voxsay.Base.VoiceVox
{
    [DataContract]
    public class VoiceVoxNotes
    {
        [DataMember(Name = "notes")]
        public List<VoiceVoxNote> Notes;
    }
}
