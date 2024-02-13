using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class VoiceVoxFramePhoneme
    {
        [DataMember(Name = "phoneme")]
        public string Phoneme { get; set; }

        [DataMember(Name = "frame_length")]
        public int Frame_Length { get; set; }
    }
}
