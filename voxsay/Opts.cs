using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace voxsay
{
    internal class Opts
    {
        public string Product { get; private set; } = null;
        public double? SpeedScale { get; private set; } = null;
        public double? PitchScale { get; private set; } = null;
        public double? IntonationScale { get; private set; } = null;
        public double? VolumeScale { get; private set; } = null;
        public double? PrePhonemeLength { get; private set; } = null;
        public double? PostPhonemeLength { get; private set; } = null;
        public int? outputSamplingRate { get; set; } = null;
        public int? Index { get; private set; } = null;
        public string TalkTest { get; private set; } = null;
        public string SaveFile { get; private set; } = null;
        public string OutputDevice { get; private set; } = null;
        public bool IsRequestList { get; private set; } = false;
        public bool IsRequestDevList { get; private set; } = false;
        public bool IsSafe { get; private set; } = false;

        List<string> ProdList = new List<string>() { "voicevox", "coeiroink", "lmroid", "sharevox", "itvoice" };

        public Opts(string[] args)
        {
            bool tonly = false;

            if (args.Length == 0)
            {
                help();
                return;
            }

            IsSafe = true;

            for(int i=0; i<args.Length; i++)
            {
                if(tonly)
                {
                    TalkTest = TalkTest + args[i];
                    continue;
                }

                switch (args[i].ToLower())
                {
                    case "-t":
                        tonly = true;
                        if (i + 1 <= args.Length)
                        {
                            TalkTest = args[i + 1];
                            i++;
                        }
                        break;

                    case "-prod":
                        if (i + 1 <= args.Length)
                        {
                            Product = args[i + 1].ToLower();
                            i++;

                            if(!ProdList.Contains(Product))
                            {
                                Product = null;
                                IsSafe = false;
                                Console.WriteLine(@"Error: unknown prod specification.");
                            }
                        }
                        else
                        {
                            Console.WriteLine(@"Error: Incorrect prod specification.");
                            IsSafe = false;
                        }
                        break;

                    case "-list":
                        IsRequestList = true;
                        break;

                    case "-devlist":
                        IsRequestDevList = true;
                        break;

                    case "-save":
                        if (i + 1 <= args.Length)
                        {
                            SaveFile = args[i + 1];
                            i++;
                        }
                        else
                        {
                            Console.WriteLine(@"Error: Incorrect save specification.");
                            IsSafe = false;
                        }
                        break;

                    case "-samplingrate":
                    case "-rsr":
                        if (i + 1 <= args.Length)
                        {
                            int result;
                            outputSamplingRate = null;
                            if (int.TryParse(args[i + 1], out result))
                            {
                                outputSamplingRate = result;
                            }
                            else
                            {
                                Console.WriteLine(@"Error: Invalid samplingrate specification.");
                                IsSafe = false;
                            }
                            i++;
                        }
                        else
                        {
                            Console.WriteLine(@"Error: Incorrect samplingrate specification.");
                            IsSafe = false;
                        }
                        break;

                    case "-index":
                    case "-i":
                        if (i + 1 <= args.Length)
                        {
                            int result;
                            Index = null;
                            if (int.TryParse(args[i + 1], out result))
                            {
                                Index = result;
                            }
                            else
                            {
                                Console.WriteLine(@"Error: Invalid index specification.");
                                IsSafe = false;
                            }
                            i++;
                        }
                        else
                        {
                            Console.WriteLine(@"Error: Incorrect index specification.");
                            IsSafe = false;
                        }
                        break;

                    case "-outputdevice":
                    case "-od":
                        if (i + 1 <= args.Length)
                        {
                            OutputDevice = args[i + 1];
                            i++;
                        }
                        else
                        {
                            Console.WriteLine(@"Error: Incorrect outputdevice specification.");
                            IsSafe = false;
                        }
                        break;

                    case "-speed":
                    case "-spd":
                        if (i + 1 <= args.Length)
                        {
                            double result;
                            SpeedScale = null;
                            if (double.TryParse(args[i + 1], out result))
                            {
                                SpeedScale = result;
                            }
                            else
                            {
                                Console.WriteLine(@"Error: Invalid speed specification.");
                                IsSafe = false;
                            }
                            i++;
                        }
                        else
                        {
                            Console.WriteLine(@"Error: Incorrect speed specification.");
                            IsSafe = false;
                        }
                        break;

                    case "-pitch":
                    case "-pch":
                        if (i + 1 <= args.Length)
                        {
                            double result;
                            PitchScale = null;
                            if (double.TryParse(args[i + 1], out result))
                            {
                                SpeedScale = result;
                            }
                            else
                            {
                                Console.WriteLine(@"Error: Invalid pitch specification.");
                                IsSafe = false;
                            }
                            i++;
                        }
                        else
                        {
                            Console.WriteLine(@"Error: Incorrect pitch specification.");
                            IsSafe = false;
                        }
                        break;

                    case "-intonation":
                    case "-itn":
                        if (i + 1 <= args.Length)
                        {
                            double result;
                            IntonationScale = null;
                            if (double.TryParse(args[i + 1], out result))
                            {
                                IntonationScale = result;
                            }
                            else
                            {
                                Console.WriteLine(@"Error: Invalid intonation specification.");
                                IsSafe = false;
                            }
                            i++;
                        }
                        else
                        {
                            Console.WriteLine(@"Error: Incorrect intonation specification.");
                            IsSafe = false;
                        }
                        break;

                    case "-volume":
                    case "-vol":
                        if (i + 1 <= args.Length)
                        {
                            double result;
                            VolumeScale = null;
                            if (double.TryParse(args[i + 1], out result))
                            {
                                VolumeScale = result;
                            }
                            else
                            {
                                Console.WriteLine(@"Error: Invalid volume specification.");
                                IsSafe = false;
                            }
                            i++;
                        }
                        else
                        {
                            Console.WriteLine(@"Error: Incorrect volume specification.");
                            IsSafe = false;
                        }
                        break;

                    case "-prephonemelength":
                    case "-prelen":
                        if (i + 1 <= args.Length)
                        {
                            double result;
                            PrePhonemeLength = null;
                            if (double.TryParse(args[i + 1], out result))
                            {
                                PrePhonemeLength = result;
                            }
                            else
                            {
                                Console.WriteLine(@"Error: Invalid prephonemelength specification.");
                                IsSafe = false;
                            }
                            i++;
                        }
                        else
                        {
                            Console.WriteLine(@"Error: Incorrect prephonemelength specification.");
                            IsSafe = false;
                        }
                        break;

                    case "-postphonemelength":
                    case "-postlen":
                        if (i + 1 <= args.Length)
                        {
                            double result;
                            PostPhonemeLength = null;
                            if (double.TryParse(args[i + 1], out result))
                            {
                                PostPhonemeLength = result;
                            }
                            else
                            {
                                Console.WriteLine(@"Error: Invalid postphonemelength specification.");
                                IsSafe = false;
                            }
                            i++;
                        }
                        else
                        {
                            Console.WriteLine(@"Error: Incorrect postphonemelength specification.");
                            IsSafe = false;
                        }
                        break;
                }

            }

        }

        private void help()
        {
            Console.WriteLine(
                @"
voxsay command (c)2022 by k896951

command line exsamples:
    voxsay -devlist
    voxsay <-prod TTS> -list
    voxsay <-prod TTS> <-index N> [-samplingrate Hz] [ -save FILENAME | -outputdevice DEV ] [option [option [... [option] ] ] ] -t TALKTEXT

Options:
    -devlist              : List playback device.
    -prod TTS             : Select tts product. TTS := <voicevox | coeiroink | lmroid | sharevox | itvoice>
    -list                 : List speakers for a given product.

    -index N              : specify the speaker index.
                            Example: -index 4 -> Speak with the 4th speaker.

    -samplingrate Hz      : Change audio sampling rate.
                            Example : -samplingrate 8000 -> Change the sampling rate to 8khz.
                            Note: Quantization bit number is 16bit only.

    -save FILENAME        : Save audio with specified file name.
                            Example: -save Hellow  -> Output audio to file ""Hellow.wav"".
                            Note: No audio playback with this option.

    -outputdevice DEV     : Change playback device.
                            Example: -outputdevice ""OUT(UA-4FX)"" -> Output audio to device ""OUT(UA-4FX)""

    -speed P              : specify the speedScale.
    -pitch P              : specify the pitchScale.
    -intonation P         : specify the intonationScale.
    -volume P             : specify the volumeScale.
    -prephonemelength P   : specify the prephonemelength.
    -postphonemelength P  : specify the postphonemelength.

    -t TALKTEXT           : Text to output in tts.
                            Example : -t Hellow world! -> say ""Hello world!""

        * Anything specified after -t is treated as tts text.
        * Please refer to the value of the editor for each product for the range of P.
"
            );
        }
    }
}
