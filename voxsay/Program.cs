using System;
using System.Text.RegularExpressions;

namespace voxsay
{
    internal class Program
    {
        static int Main(string[] args)
        {
            int rcd = 0;
            Opts opt = new Opts(args);

            if (!opt.IsSafe) return 8;

            if (opt.IsRequestDevList)
            {
                foreach (var item in ApiProxy.GetMMDeviceList())
                {
                    Console.WriteLine(string.Format(@"device: ""{0}""", item.FriendlyName));
                }

                return 0;
            }

            var api = new ApiProxy(opt.ProductHostInfo);

            if (!api.CheckConnectivity())
            {
                Console.WriteLine(String.Format(@"Error: Unable to connect to {0}", opt.SelectedProd));
                return 8;
            }

            if ((opt.IsRequestList) && (opt.SelectedProd != ""))
            {
                foreach(var item in api.AvailableCasts())
                {
                    Console.WriteLine(string.Format(@"index: {0},  speaker:{1}", item.Key, item.Value));
                }

                return 0;
            }

            if (opt.OutputDevice != "")
            {
                api.PlayDeviceName = opt.OutputDevice;
            }

            if (opt.Index != null)
            {
                SpeakerParams pm = api.GetAvatorParams((int)opt.Index);
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

                    if (!api.Save((int)opt.Index, pm, opt.TalkTest, f)) rcd = 8;
                }
                else
                {
                    if (!api.Speak((int)opt.Index, pm, opt.TalkTest)) rcd = 8;
                }
            }

            return rcd;
        }
    }
}
