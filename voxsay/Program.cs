using System;
using System.Text.RegularExpressions;

namespace voxsay
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Opts opt = new Opts(args);

            if (!opt.IsSafe) return;

            if (opt.IsRequestDevList)
            {
                foreach (var item in ApiProxy.GetMMDeviceList())
                {
                    Console.WriteLine(string.Format(@"device: ""{0}""", item.FriendlyName));
                }

                return;
            }

            var api = new ApiProxy(opt.ProductUrl);

            if ((opt.IsRequestList) && (opt.Product != null))
            {
                foreach(var item in api.AvailableCasts())
                {
                    Console.WriteLine(string.Format(@"index: {0},  speaker:{1}", item.Key, item.Value));
                }

                return;
            }

            if (!api.CheckConnectivity())
            {
                Console.WriteLine(String.Format(@"Error: Unable to connect to {0}", opt.Product));
                return;
            }

            if (opt.OutputDevice != "")
            {
                api.PlayDeviceName = opt.OutputDevice;
            }

            if (opt.Index != null)
            {
                VoiceVoxParams pm = api.GetAvatorParams((int)opt.Index);
                if (opt.SpeedScale != null) pm.speedScale = (double)opt.SpeedScale;
                if (opt.PitchScale != null) pm.pitchScale = (double)opt.PitchScale;
                if (opt.VolumeScale != null) pm.volumeScale = (double)opt.VolumeScale;
                if (opt.IntonationScale != null) pm.intonationScale = (double)opt.IntonationScale;
                if (opt.PrePhonemeLength != null) pm.prePhonemeLength = (double)opt.PrePhonemeLength;
                if (opt.PostPhonemeLength != null) pm.postPhonemeLength = (double)opt.PostPhonemeLength;
                if (opt.OutputSamplingRate != null) pm.outputSamplingRate = (int)opt.OutputSamplingRate;

                if (opt.SaveFile != null)
                {
                    string f = opt.SaveFile;
                    Regex ext = new Regex(@"\.[wW][aA][vV][eE]{0,1}$");

                    if (!ext.IsMatch(f)) f = String.Format(@"{0}.wav", f);

                    api.Save((int)opt.Index, pm, opt.TalkTest, f);
                }
                else
                {
                    api.Speak((int)opt.Index, pm, opt.TalkTest);
                }
            }
        }
    }
}
