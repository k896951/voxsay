using System.Collections.Generic;
using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class Coeiroinkv2AudioQuery : AudioQuery
    {
        [DataMember]
        public string speakerUuid { get; set; }

        [DataMember]
        public int styleId { get; set; }

        [DataMember]
        public string text { get; set; }

        [DataMember]
        public List<List<Coeiroinkv2ProsodyDetail>> prosodyDetail { get; set; }

        [DataMember]
        public double? speedScale { get; set; }

        [DataMember]
        public double? pitchScale { get; set; }

        [DataMember]
        public double? intonationScale { get; set; }

        [DataMember]
        public double? volumeScale { get; set; }

        [DataMember]
        public double? prePhonemeLength { get; set; }

        [DataMember]
        public double? postPhonemeLength { get; set; }

        [DataMember]
        public int? outputSamplingRate { get; set; }

        [DataMember]
        public bool outputStereo { get; set; }
    }

}
