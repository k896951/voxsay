using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class VoiceVoxAudioQuery : AudioQuery
    {
        [DataMember]
        public VoiceVoxAccentPhrase[] accent_phrases { get; set; }

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

        [DataMember]
        public string kana { get; set; }
    }

}
