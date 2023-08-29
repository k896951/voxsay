using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace voxsay
{
    public class ApiProxy : IDisposable
    {
        private HttpClient client;

        private string BaseUri = @"";

        private ProdnameEnum SelectedProd = ProdnameEnum.voicevox; //デフォルトにしておく

        public string PlayDeviceName { get; set; }

        public ApiProxy(ProductMap prodinfo)
        {
            try
            {
                client = new HttpClient();
                BaseUri = string.Format(@"http://{0}:{1}{2}", prodinfo.Hostname, prodinfo.Portnumber, prodinfo.Context);
                SelectedProd = prodinfo.Product;
            }
            catch (Exception)
            {
                client?.Dispose();
                client = null;
            }
        }

        public void Dispose()
        {
            client?.Dispose();
        }

        private void SettingJsonHeader()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/wav"));
            client.DefaultRequestHeaders.Add("User-Agent", "AssistantSeika Driver");
        }

        private bool PostSynthesisQuery(AudioQuery aq, int speaker, string saveFileName)
        {
            if (SelectedProd == ProdnameEnum.coeiroinkv2)
            {
                return PostCoeiroinkv2SynthesisQuery(aq as Coeiroinkv2AudioQuery, speaker, saveFileName);
            }
            else
            {
                return PostVoiceVoxSynthesisQuery(aq as VoiceVoxAudioQuery, speaker, saveFileName);
            }
        }

        private bool PostVoiceVoxSynthesisQuery(VoiceVoxAudioQuery aq, int speaker, string saveFileName)
        {
            var json = new DataContractJsonSerializer(typeof(VoiceVoxAudioQuery));
            MemoryStream ms = new MemoryStream();
            bool ans = true;

            json.WriteObject(ms, aq);

            var content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");

            Task.Run(async () => {

                SettingJsonHeader();

                try
                {
                    var response = await client.PostAsync(string.Format(@"{0}/synthesis?speaker={1}", BaseUri, speaker), content);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string tempFileName = saveFileName == "" ? Path.GetTempFileName() : saveFileName;

                        using (FileStream tempfile = new FileStream(tempFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(tempfile);
                        }

                        if (saveFileName == "")
                        {
                            PlayWaveFile(tempFileName);
                            File.Delete(tempFileName);
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("PostVoiceVoxSynthesisQuery:{0}", e.Message);
                    ans = false;
                }
            }).Wait();

            return ans;
        }

        private bool PostCoeiroinkv2SynthesisQuery(Coeiroinkv2AudioQuery aq, int speaker, string saveFileName)
        {
            var json = new DataContractJsonSerializer(typeof(Coeiroinkv2AudioQuery));
            MemoryStream ms = new MemoryStream();
            bool ans = true;

            json.WriteObject(ms, aq);

            var content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");

            Task.Run(async () => {

                SettingJsonHeader();

                try
                {
                    var response = await client.PostAsync(string.Format(@"{0}/synthesis", BaseUri), content);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string tempFileName = saveFileName == "" ? Path.GetTempFileName() : saveFileName;

                        using (FileStream tempfile = new FileStream(tempFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(tempfile);
                        }

                        if (saveFileName == "")
                        {
                            PlayWaveFile(tempFileName);
                            File.Delete(tempFileName);
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("PostCoeiroinkv2SynthesisQuery:{0}", e.Message);
                    ans = false;
                }
            }).Wait();

            return ans;
        }

        private AudioQuery GetAudioQuery(string text, int speaker)
        {
            if (SelectedProd == ProdnameEnum.coeiroinkv2)
            {
                return GetCoeiroinkv2AudioQuery(text, speaker);
            }
            else
            {
                return GetVoiceVoxAudioQuery(text, speaker);
            }
        }

        private VoiceVoxAudioQuery GetVoiceVoxAudioQuery(string text, int speaker)
        {
            string url = string.Format(@"{0}/audio_query?text={1}&speaker={2}", BaseUri, text, speaker);
            var content = new StringContent("{}", Encoding.UTF8, @"application/json");
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
            VoiceVoxAudioQuery ans = null;

            settings.UseSimpleDictionaryFormat = true;

            Task.Run(async () => {
                SettingJsonHeader();

                try
                {
                    var response = await client.PostAsync(url, content);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var json = new DataContractJsonSerializer(typeof(VoiceVoxAudioQuery), settings);
                        ans = (VoiceVoxAudioQuery)json.ReadObject(await response.Content.ReadAsStreamAsync());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("GetVoiceVoxAudioQuery:{0}", e.Message);
                    ans = null;
                }
            }).Wait();

            return ans;
        }

        private Coeiroinkv2AudioQuery GetCoeiroinkv2AudioQuery(string text, int speaker)
        {
            Coeiroinkv2AudioQuery ans = new Coeiroinkv2AudioQuery();

            string url_getspeakerinfo = string.Format(@"{0}/style_id_to_speaker_meta?styleId={1}", BaseUri, speaker);
            var content_speaker = new StringContent("", Encoding.UTF8, @"application/json");

            string url_estimateprosody = string.Format(@"{0}/estimate_prosody", BaseUri);
            var content_estimateprosody = new StringContent(@"{" + string.Format(@"""text"":""{0}""", text) + @"}", Encoding.UTF8, @"application/json");

            Coeiroinkv2Prosody prosody = null;
            Coeiroinkv2StyleidToSpeakerMeta speakerinfo = null;

            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();

            settings.UseSimpleDictionaryFormat = true;

            Task.Run(async () => {
                SettingJsonHeader();

                try
                {
                    var response1 = await client.PostAsync(url_getspeakerinfo, content_speaker);

                    if (response1.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var json = new DataContractJsonSerializer(typeof(Coeiroinkv2StyleidToSpeakerMeta), settings);
                        speakerinfo = (Coeiroinkv2StyleidToSpeakerMeta)json.ReadObject(await response1.Content.ReadAsStreamAsync());

                        ans.speakerUuid = speakerinfo.speakerUuid;
                        ans.styleId = speakerinfo.styleId;
                    }

                    var response2 = await client.PostAsync(url_estimateprosody, content_estimateprosody);

                    if (response2.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var json = new DataContractJsonSerializer(typeof(Coeiroinkv2Prosody), settings);
                        prosody = (Coeiroinkv2Prosody)json.ReadObject(await response2.Content.ReadAsStreamAsync());
                        ans.prosodyDetail = prosody.detail.ToList();
                    }

                    ans.text = text;
                }
                catch (Exception e)
                {
                    Console.WriteLine("GetCoeiroinkAudioQuery:{0}", e.Message);
                    ans = null;
                }
            }).Wait();

            return ans;
        }

        /// <summary>
        /// 話者パラメタの取り出し
        /// </summary>
        /// <param name="speaker">話者番号</param>
        /// <returns>パラメタ情報</returns>
        public SpeakerParams GetAvatorParams(int speaker)
        {
            SpeakerParams ans;

            if (SelectedProd == ProdnameEnum.coeiroinkv2)
            {
                // 各種パラメタ値の取得が出来そうなAPIが見つけられなかったので、GUIに設定されていた設定値デフォルトとして適用する
                ans = new Coeiroinkv2Params();

                ans.intonationScale = 1.0;     // Range:  0    .. 2
                ans.pitchScale = 0.0;          // Range: -0.15 .. 0.15
                ans.speedScale = 1.0;          // Range:  0.5  .. 2
                ans.volumeScale = 1.0;         // Range:  0    .. 2
                ans.prePhonemeLength = 0.1;    // Range:  0    .. 1.5
                ans.postPhonemeLength = 0.1;   // Range:  0    .. 1.5
                ans.outputSamplingRate = 44100;
            }
            else
            {
                // 面倒なのでVOICEVOXのデフォルト値を適用する。
                ans = new VoiceVoxParams();

                ans.intonationScale = 1.0;     // Range:  0    .. 2
                ans.pitchScale = 0.0;          // Range: -0.15 .. 0.15
                ans.speedScale = 1.0;          // Range:  0.5  .. 2
                ans.volumeScale = 1.0;         // Range:  0    .. 2
                ans.prePhonemeLength = 0.1;    // Range:  0    .. 1.5
                ans.postPhonemeLength = 0.1;   // Range:  0    .. 1.5
                ans.outputSamplingRate = 44100;
            }

            return ans;
        }

        /// <summary>
        /// 利用可能な話者の取り出し
        /// </summary>
        /// <returns>話者番号と名称の組み合わせのリスト</returns>
        public List<KeyValuePair<int, string>> AvailableCasts()
        {
            if(SelectedProd == ProdnameEnum.coeiroinkv2)
            {
                return Coeiroinkv2AvailableCasts();
            }
            else
            {
                return VoiceVoxAvailableCasts();
            }
        }

        private List<KeyValuePair<int, string>> VoiceVoxAvailableCasts()
        {
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
            List<VoiceVoxSpeaker> speakers = new List<VoiceVoxSpeaker>();
            var ans = new List<KeyValuePair<int, string>>();

            Task.Run(async () => {
                SettingJsonHeader();

                try
                {
                    var response = await client.GetAsync(string.Format("{0}/speakers", BaseUri));

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var json = new DataContractJsonSerializer(typeof(List<VoiceVoxSpeaker>), settings);

                        speakers = (List<VoiceVoxSpeaker>)json.ReadObject(await response.Content.ReadAsStreamAsync());

                        ans = speakers.SelectMany(v1 => v1.styles.Select(v2 => new { id = v2.Id, speaker_uuid = v1.speaker_uuid, name = string.Format("{0}（{1}）", v1.name, v2.Name) }))
                                      .OrderBy(v => v.id)
                                      .Select(v => new KeyValuePair<int, string>(v.id, v.name)).ToList();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("AvailableCasts:{0}", e.Message);
                    ans = null;
                }
            }).Wait();

            return ans;
        }

        private List<KeyValuePair<int, string>> Coeiroinkv2AvailableCasts()
        {
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
            var speakers = new List<Coeiroinkv2Speaker>();
            var ans = new List<KeyValuePair<int, string>>();

            Task.Run(async () => {
                SettingJsonHeader();

                try
                {
                    var response = await client.GetAsync(string.Format("{0}/speakers", BaseUri));

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var json = new DataContractJsonSerializer(typeof(List<Coeiroinkv2Speaker>), settings);

                        speakers = (List<Coeiroinkv2Speaker>)json.ReadObject(await response.Content.ReadAsStreamAsync());

                        ans = speakers.SelectMany(v1 => v1.styles.Select(v2 => new { id = v2.Id, speaker_uuid = v1.speaker_uuid, name = string.Format("{0}（{1}）", v1.name, v2.Name) }))
                                      .OrderBy(v => v.id)
                                      .Select(v => new KeyValuePair<int, string>(v.id, v.name)).ToList();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("AvailableCasts:{0}", e.Message);
                    ans = null;
                }
            }).Wait();

            return ans;
        }

        /// <summary>
        /// 発声
        /// </summary>
        /// <param name="speaker">話者番号</param>
        /// <param name="param">エフェクト</param>
        /// <param name="text">発声させるテキスト</param>
        public bool Speak(int speaker, SpeakerParams param, string text)
        {
            if (SelectedProd == ProdnameEnum.coeiroinkv2)
            {
                return Coeiroinkv2Speak(speaker, param, text);
            }
            else
            {
                return VoiceVoxSpeak(speaker, param, text);
            }
        }

        private bool VoiceVoxSpeak(int speaker, SpeakerParams param, string text)
        {
            VoiceVoxAudioQuery aq = GetAudioQuery(text, speaker) as VoiceVoxAudioQuery;

            if ((aq != null) && (param != null))
            {
                aq.volumeScale = param.volumeScale;
                aq.intonationScale = param.intonationScale;
                aq.pitchScale = param.pitchScale;
                aq.speedScale = param.speedScale;
                aq.prePhonemeLength = param.prePhonemeLength;
                aq.postPhonemeLength = param.postPhonemeLength;
                aq.outputSamplingRate = param.outputSamplingRate;
            }

            return PostSynthesisQuery(aq, speaker, "");
        }

        private bool Coeiroinkv2Speak(int speaker, SpeakerParams param, string text)
        {
            Coeiroinkv2AudioQuery aq = GetAudioQuery(text, speaker) as Coeiroinkv2AudioQuery;

            if ((aq != null) && (param != null))
            {
                aq.volumeScale = param.volumeScale;
                aq.intonationScale = param.intonationScale;
                aq.pitchScale = param.pitchScale;
                aq.speedScale = param.speedScale;
                aq.prePhonemeLength = param.prePhonemeLength;
                aq.postPhonemeLength = param.postPhonemeLength;
                aq.outputSamplingRate = param.outputSamplingRate;
            }

            return PostSynthesisQuery(aq, speaker, "");
        }

        /// <summary>
        /// 音声保存
        /// </summary>
        /// <param name="speaker">話者番号</param>
        /// <param name="param">エフェクト</param>
        /// <param name="text">発声させるテキスト</param>
        /// <param name="WavFilePath">保存するファイル名</param>
        public bool Save(int speaker, SpeakerParams param, string text, string WavFilePath)
        {
            if(SelectedProd == ProdnameEnum.coeiroinkv2)
            {
                return Coeiroinkv2Save(speaker, param as Coeiroinkv2Params, text, WavFilePath);
            }
            else
            {
                return VoiceVoxSave(speaker, param as VoiceVoxParams, text, WavFilePath);
            }
        }

        private bool VoiceVoxSave(int speaker, VoiceVoxParams param, string text, string WavFilePath)
        {
            VoiceVoxAudioQuery aq = GetVoiceVoxAudioQuery(text, speaker);

            if ((aq != null) && (param != null))
            {
                aq.volumeScale = param.volumeScale;
                aq.intonationScale = param.intonationScale;
                aq.pitchScale = param.pitchScale;
                aq.speedScale = param.speedScale;
                aq.prePhonemeLength = param.prePhonemeLength;
                aq.postPhonemeLength = param.postPhonemeLength;
                aq.outputSamplingRate = param.outputSamplingRate;
            }

            return PostSynthesisQuery(aq, speaker, WavFilePath);
        }

        private bool Coeiroinkv2Save(int speaker,Coeiroinkv2Params param, string text, string WavFilePath)
        {
            Coeiroinkv2AudioQuery aq = GetCoeiroinkv2AudioQuery(text, speaker);

            if ((aq != null) && (param != null))
            {
                aq.volumeScale = param.volumeScale;
                aq.intonationScale = param.intonationScale;
                aq.pitchScale = param.pitchScale;
                aq.speedScale = param.speedScale;
                aq.prePhonemeLength = param.prePhonemeLength;
                aq.postPhonemeLength = param.postPhonemeLength;
                aq.outputSamplingRate = param.outputSamplingRate;
            }

            return PostSynthesisQuery(aq, speaker, WavFilePath);
        }

        public bool CheckConnectivity()
        {
            bool ans = true;
            HttpResponseMessage response = null;

            Task.Run(async () => {

                try
                {
                    response = await client.GetAsync(string.Format("{0}/speakers", BaseUri));

                    if (response.StatusCode == System.Net.HttpStatusCode.OK) ans = true;
                }
                catch (Exception)
                {
                    ans = false;
                }

            }).Wait();

            return ans;
        }

        private bool PlayWaveFile(string WavFilePath)
        {
            bool ans = true;

            try
            {
                var dev = GetMMDevice(PlayDeviceName);
                using (var audioFile = new AudioFileReader(WavFilePath))
                using (var outputDevice = new WasapiOut(dev, AudioClientShareMode.Shared, false, 0))
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(5);
                    }
                }
                dev?.Dispose();
            }
            catch (Exception f2sd)
            {
                Console.WriteLine("PlayWaveFile:{0}", f2sd.Message);
                ans = false;
            }

            return ans;
        }

        private MMDevice GetMMDevice(string mmDeviceName)
        {
            MMDevice mmCapDev = null;

            if (mmDeviceName == "")
            {
                mmCapDev = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            else
            {
                foreach (var item in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    if (item.FriendlyName == mmDeviceName)
                    {
                        mmCapDev = item;
                        break;
                    }
                }
                if (mmCapDev == null)
                {
                    mmCapDev = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                }
            }

            return mmCapDev;
        }

        public static List<MMDevice> GetMMDeviceList()
        {
            List<MMDevice> list = new List<MMDevice>();

            foreach (var item in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                list.Add(item);
            }
            return list;
        }


    }

}
