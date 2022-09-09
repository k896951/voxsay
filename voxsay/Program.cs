﻿using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace voxsay
{
    internal class Program
    {
        static Dictionary<string, ApiProxy> apis = new Dictionary<string, ApiProxy>
        {
            { "voicevox",   new ApiProxy("http://127.0.0.1:50021") },
            { "coeiroink",  new ApiProxy("http://127.0.0.1:50031") },
            { "lmroid",     new ApiProxy("http://127.0.0.1:50073") },
            { "sharevox",   new ApiProxy("http://127.0.0.1:50025") }
        };

        static void Main(string[] args)
        {
            Opts opt = new Opts(args);

            if (!opt.IsSafe) return;

            var api = apis[opt.Product];
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

            if (opt.Index != null)
            {
                VoiceVoxParams pm = api.GetAvatorParams((int)opt.Index);
                if (opt.SpeedScale != null) pm.speedScale = (double)opt.SpeedScale;
                if (opt.PitchScale != null) pm.pitchScale = (double)opt.PitchScale;
                if (opt.VolumeScale != null) pm.volumeScale = (double)opt.VolumeScale;
                if (opt.IntonationScale != null) pm.intonationScale = (double)opt.IntonationScale;
                if (opt.PrePhonemeLength != null) pm.prePhonemeLength = (double)opt.PrePhonemeLength;
                if (opt.PostPhonemeLength != null) pm.postPhonemeLength = (double)opt.PostPhonemeLength;

                if(opt.SaveFile != null)
                {
                    api.Save((int)opt.Index, pm, opt.TalkTest, opt.SaveFile);
                }
                else
                {
                    api.Speak((int)opt.Index, pm, opt.TalkTest);
                }
            }
        }
    }
}
