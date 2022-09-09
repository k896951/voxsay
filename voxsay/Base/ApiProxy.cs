using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace voxsay
{
    public class ApiProxy : IDisposable
    {
        private HttpClient client;

        private string BaseUri = @"";

        public string PlayDeviceName { get; set; }

        public ApiProxy(string ApiUrl)
        {
            try
            {
                client = new HttpClient();
                BaseUri = ApiUrl;
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

        private void PostSynthesisQuery(VoiceVoxAudioQuery aq, int speaker, string saveFileName = "")
        {
            var json = new DataContractJsonSerializer(typeof(VoiceVoxAudioQuery));
            MemoryStream ms = new MemoryStream();
            json.WriteObject(ms, aq);

            var content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");

            Task.Run(async () => {

                SettingJsonHeader();

                try
                {
                    var response = await client.PostAsync(string.Format(@"{0}/synthesis?speaker={1}", BaseUri, speaker), content);

                    if (response.StatusCode== System.Net.HttpStatusCode.OK)
                    {
                        string tempFileName = saveFileName == "" ? Path.GetTempFileName() : saveFileName;
                        using (FileStream tempfile = new FileStream(tempFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(tempfile);
                        }

                        if (saveFileName == "")
                        {
                            PlayWaveFile(tempFileName);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("PostSynthesisQuery:{0}", e.Message);
                }
            }).Wait();
        }

        private VoiceVoxAudioQuery GetAudioQuery(string text, int speaker)
        {
            var content = new StringContent("{}", Encoding.UTF8, @"application/json");
            VoiceVoxAudioQuery ans = null;
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();

            settings.UseSimpleDictionaryFormat = true;

            Task.Run(async () => {
                SettingJsonHeader();

                try
                {
                    var response = await client.PostAsync(string.Format(@"{0}/audio_query?text={1}&speaker={2}", BaseUri, text, speaker), content);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var json = new DataContractJsonSerializer(typeof(VoiceVoxAudioQuery), settings);
                        ans = (VoiceVoxAudioQuery)json.ReadObject(await response.Content.ReadAsStreamAsync());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("GetAudioQuery:{0}", e.Message);
                }
            }).Wait();

            return ans;
        }

        /// <summary>
        /// 話者パラメタの取り出し
        /// </summary>
        /// <param name="speaker">話者番号</param>
        /// <returns>パラメタ情報</returns>
        public VoiceVoxParams GetAvatorParams(int speaker)
        {
            VoiceVoxParams ans = new VoiceVoxParams();

            try
            {
                VoiceVoxAudioQuery aq = GetAudioQuery("あ", speaker);

                if (aq!=null)
                {
                    ans.intonationScale = (double)aq.intonationScale;
                    ans.pitchScale = (double)aq.pitchScale;
                    ans.speedScale = (double)aq.speedScale;
                    ans.volumeScale = (double)aq.volumeScale;
                    ans.prePhonemeLength = (double)aq.prePhonemeLength;
                    ans.postPhonemeLength = (double)aq.postPhonemeLength;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("GetAvatorParams:{0}", e.Message);
            }

            return ans;
        }

        /// <summary>
        /// 利用可能な話者の取り出し
        /// </summary>
        /// <returns>話者番号と名称の組み合わせのリスト</returns>
        public List<KeyValuePair<int, string>> AvailableCasts()
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

                        ans = speakers.SelectMany(v1 => v1.styles.Select(v2 => new { id = v2.Id, name = string.Format("{0}（{1}）", v1.name, v2.Name) }))
                                      .OrderBy(v => v.id)
                                      .Select(v => new KeyValuePair<int, string>(v.id, v.name)).ToList();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("AvailableCasts:{0}", e.Message);
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
        public void Speak(int speaker, VoiceVoxParams param, string text)
        {

            VoiceVoxAudioQuery aq = GetAudioQuery(text, speaker);

            if (param != null)
            {
                aq.volumeScale = param.volumeScale;
                aq.intonationScale = param.intonationScale;
                aq.pitchScale = param.pitchScale;
                aq.speedScale = param.speedScale;
                aq.prePhonemeLength = param.prePhonemeLength;
                aq.postPhonemeLength = param.postPhonemeLength;
            }

            PostSynthesisQuery(aq, speaker);
        }

        /// <summary>
        /// 音声保存
        /// </summary>
        /// <param name="speaker">話者番号</param>
        /// <param name="param">エフェクト</param>
        /// <param name="text">発声させるテキスト</param>
        /// <param name="WavFilePath">保存するファイル名</param>
        public void Save(int speaker, VoiceVoxParams param, string text, string WavFilePath)
        {
            VoiceVoxAudioQuery aq = GetAudioQuery(text, speaker);

            if (param != null)
            {
                aq.volumeScale = param.volumeScale;
                aq.intonationScale = param.intonationScale;
                aq.pitchScale = param.pitchScale;
                aq.speedScale = param.speedScale;
                aq.prePhonemeLength = param.prePhonemeLength;
                aq.postPhonemeLength = param.postPhonemeLength;
            }

            PostSynthesisQuery(aq, speaker, WavFilePath);
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
            }

            return true;
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
    }

}
