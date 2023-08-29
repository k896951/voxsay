using System.Collections.Generic;
using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class Coeiroinkv2Prosody
    {
        [DataMember]
        public string[] plain;

        [DataMember]
        public List<List<Coeiroinkv2ProsodyDetail>> detail;

    }
}
