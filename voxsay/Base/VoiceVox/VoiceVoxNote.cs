using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class VoiceVoxNote
    {
        [DataMember(Name ="key")]
        public int? Key { get; set; }

        [DataMember(Name = "frame_length")]
        public int Frame_Length { get; set; }

        [DataMember(Name = "lyric")]
        public string Lyric { get; set; }

        [DataMember(Name = "notelen")]
        public string NoteLen { get; set; }
    }
}
