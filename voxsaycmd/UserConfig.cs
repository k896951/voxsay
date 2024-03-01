using System.Runtime.Serialization;

namespace voxsaycmd
{
    [DataContract]
    public class UserConfig
    {
        [DataMember(Name = "prod")]
        public string SpecifiedProduct { get; private set; } = null;

        [DataMember(Name = "host")]
        public string SpecifiedHost { get; private set; } = null;

        [DataMember(Name = "port")]
        public int? SpecifiedPort { get; private set; } = null;

        [DataMember(Name = "speed")]
        public double? SpeedScale { get; private set; } = null;

        [DataMember(Name ="pitch")]
        public double? PitchScale { get; private set; } = null;

        [DataMember(Name ="intonation")]
        public double? IntonationScale { get; private set; } = null;
        
        [DataMember(Name ="volume")]
        public double? VolumeScale { get; private set; } = null;
        
        [DataMember(Name = "prephonemelength")]
        public double? PrePhonemeLength { get; private set; } = null;
        
        [DataMember(Name = "postphonemelength")]
        public double? PostPhonemeLength { get; private set; } = null;
        
        [DataMember(Name ="samplingrate")]
        public int? OutputSamplingRate { get; private set; } = null;

        [DataMember(Name ="outputdevice")]
        public string OutputDevice { get; private set; } = null;

        [DataMember(Name ="index")]
        public int? Index { get; private set; } = null;

        [DataMember(Name = "renderingmode")]
        public string RenderingMode { get; private set; } = null;

        [DataMember(Name = "mf")]
        public string InputfilenameM { get; private set; } = null;

        [DataMember(Name = "sf")]
        public string InputfilenameS { get; private set; } = null;
    }
}
