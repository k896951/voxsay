﻿using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using voxsay;
using static System.Net.Mime.MediaTypeNames;

namespace voxsaycmd
{
    internal class Program
    {
        static int Main(string[] args)
        {
            int rcd = 0;
            Opts opt = new Opts(args);


            if (!opt.IsSafe) return 8;

            // 再生デバイス一覧表示
            if (opt.IsRequestDevList)
            {
                foreach (var item in ApiProxy.GetMMDeviceList())
                {
                    Console.WriteLine(string.Format(@"device: ""{0}""", item));
                }

                return 0;
            }

            // 稼働している音声合成製品一覧表示（標準指定）
            if (opt.IsRequestActiveProductList)
            {
                foreach (var item in ApiProxy.ConnectivityList())
                {
                    Console.WriteLine(string.Format(@"product: {0}", item.ToString()));
                }

                return 0;
            }

            // 知らない製品が指定された
            if(!ApiProxy.IsValidProduct(opt.SpecifiedProduct))
            {
                Console.WriteLine(String.Format(@"Error: Unknown product {0}", opt.SpecifiedProduct));
                return 8;
            }

            ApiProxy api = new ApiProxy(opt.SpecifiedProduct, opt.SpecifiedHost, opt.SpecifiedPort);

            // 再生デバイス指定があれば設定
            if (opt.OutputDevice != "")
            {
                api.PlayDeviceName = opt.OutputDevice;
            }

            // 製品への接続性確認
            if (!api.CheckConnectivity())
            {
                Console.WriteLine(String.Format(@"Error: Unable to connect to {0}", opt.SpecifiedProduct));
                return 8;
            }

            // 話者一覧表示
            if (opt.IsRequestSpeakerList)
            {
                switch (opt.RenderingMode)
                {
                    case "sing":
                        foreach (var item in api.AvailableSingers())
                        {
                            Console.WriteLine(string.Format(@"index: {0},  speaker:{1}", item.Key, item.Value));
                        }
                        break;

                    case "talk":
                    default:
                        foreach (var item in api.AvailableCasts())
                        {
                            Console.WriteLine(string.Format(@"index: {0},  speaker:{1}", item.Key, item.Value));
                        }
                        break;
                }

                return 0;
            }

            // 発声もしくは保存処理
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

                Regex ext = new Regex(@"\.[wW][aA][vV][eE]{0,1}$");

                // とりあえずの呼び出し処理を追加
                if (opt.RenderingMode == "sing")
                {
                    try
                    {
                        var mmlObj = new VoiceVoxNoteGenerator();

                        if (("" + opt.Inputfilename) == "")
                        {
                            var parseInfo = mmlObj.ParseSingString(opt.TalkText);

                            var notes = mmlObj.ConvertScoreInfo(parseInfo);

                            if (opt.ExportNote) File.WriteAllText(@".\MyScore.json", mmlObj.ExportNotes(notes));
                            if (opt.PrintNote) mmlObj.PrintAssignInfo(notes);

                            if (opt.SaveFile != null)
                            {
                                string f = opt.SaveFile;

                                if (!ext.IsMatch(f)) f = String.Format(@"{0}.wav", f);

                                if (!api.SaveSong((int)opt.Index, pm, notes, f)) rcd = 8;
                            }
                            else
                            {
                                if (!api.Sing((int)opt.Index, pm, notes)) rcd = 8;
                            }
                        }
                        else
                        {
                            var parseInfoList = mmlObj.ParseSingFile(opt.Inputfilename);

                            switch (opt.SingWaveGenType)
                            {
                                case SingWavGenTypeEnum.allnote:
                                    var notes = mmlObj.ConvertScoreInfo(parseInfoList);

                                    if (opt.ExportNote) File.WriteAllText(@".\MyScore.json", mmlObj.ExportNotes(notes));
                                    if (opt.PrintNote) mmlObj.PrintAssignInfo(notes);

                                    if (opt.SaveFile != null)
                                    {
                                        string f = opt.SaveFile;

                                        if (!ext.IsMatch(f)) f = string.Format(@"{0}.wav", f);

                                        if (!api.SaveSong((int)opt.Index, pm, notes, f)) rcd = 8;
                                    }
                                    else
                                    {
                                        if (!api.Sing((int)opt.Index, pm, notes)) rcd = 8;
                                    }

                                    break;

                                case SingWavGenTypeEnum.splitnote:
                                    int fileTailNumber = 1;
                                    foreach (var noteItem in parseInfoList)
                                    {
                                        var note = mmlObj.ConvertScoreInfo(noteItem);

                                        if (note.Notes.Count > 1)
                                        {
                                            if (opt.PrintNote) mmlObj.PrintAssignInfo(note, fileTailNumber);

                                            if (opt.SaveFile != null)
                                            {
                                                string f = opt.SaveFile;

                                                if (ext.IsMatch(f)) f = ext.Replace(f, "");
                                                f = string.Format(@"{0}_{1:D8}.wav", f, fileTailNumber);

                                                if (!api.SaveSong((int)opt.Index, pm, note, f)) rcd = 8;
                                            }
                                            else
                                            {
                                                if (!api.Sing((int)opt.Index, pm, note)) rcd = 8;
                                            }
                                        }

                                        fileTailNumber++;
                                    }

                                    break;
                            }

                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(string.Format(@"sing error: {0}", e.Message));
                    }

                }
                else
                {
                    try
                    {
                        if (("" + opt.Inputfilename) == "")
                        {
                            if (opt.SaveFile != null)
                            {
                                string f = opt.SaveFile;

                                if (!ext.IsMatch(f)) f = String.Format(@"{0}.wav", f);

                                if (!api.Save((int)opt.Index, pm, opt.TalkText, f)) rcd = 8;
                            }
                            else
                            {
                                if (!api.Speak((int)opt.Index, pm, opt.TalkText)) rcd = 8;
                            }
                        }
                        else
                        {
                            Stream fp;

                            if ("-" == opt.Inputfilename)
                            {
                                fp = Console.OpenStandardInput();
                            }
                            else
                            {
                                fp = new FileStream(opt.Inputfilename, FileMode.Open, FileAccess.Read, FileShare.Read);
                            }

                            using (StreamReader sr = new StreamReader(fp))
                            {
                                switch (opt.TalkWaveGenType)
                                {
                                    case TalkWavGenTypeEnum.allline:
                                        StringBuilder sb = new StringBuilder();
                                        sb.Clear();

                                        while (!sr.EndOfStream)
                                        {
                                            sb.AppendLine(sr.ReadLine());
                                        }

                                        if (opt.SaveFile != null)
                                        {
                                            string f = opt.SaveFile;

                                            if (!ext.IsMatch(f)) f = String.Format(@"{0}.wav", f);

                                            if (!api.Save((int)opt.Index, pm, sb.ToString(), f)) rcd = 8;
                                        }
                                        else
                                        {
                                            if (!api.Speak((int)opt.Index, pm, sb.ToString())) rcd = 8;
                                        }

                                        break;

                                    case TalkWavGenTypeEnum.splitline:
                                        int fileTailNumber = 1;
                                        while (!sr.EndOfStream)
                                        {
                                            if (opt.SaveFile != null)
                                            {
                                                string f = opt.SaveFile;

                                                if (ext.IsMatch(f)) f = ext.Replace(f, "");
                                                f = string.Format(@"{0}_{1:D8}.wav", f, fileTailNumber);

                                                if (!api.Save((int)opt.Index, pm, sr.ReadLine(), f)) rcd = 8;
                                            }
                                            else
                                            {
                                                if (!api.Speak((int)opt.Index, pm, sr.ReadLine())) rcd = 8;
                                            }

                                            fileTailNumber++;

                                        }

                                        break;
                                }

                                sr?.Close();
                            }

                            fp?.Close();
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(string.Format(@"talk error: {0}", e.Message));
                    }
                }

            }

            return rcd;
        }
    }
}
