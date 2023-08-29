using System.Runtime.Serialization;

namespace voxsay
{
    [DataContract]
    public class Coeiroinkv2SpeakerStyle
    {
        [DataMember(Name= "styleName")]
        public string Name { get; set; }

        [DataMember(Name= "styleId")]
        public int Id { get; set; }
    }

}
