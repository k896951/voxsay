namespace voxsay
{
    public class VoiceVoxParams : SpeakerParams
    {
        public VoiceVoxParams()
        {
            speedScale = 1.0f;
            pitchScale = 0.0f;
            intonationScale  = 1.0f;
            volumeScale = 1.0f;
            prePhonemeLength = 0.1f;
            postPhonemeLength = 0.1f;
            outputSamplingRate = 44100;
        }
    }
}
