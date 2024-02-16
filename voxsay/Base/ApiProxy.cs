using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using SpeechLib;
using voxsay.Base.VoiceVox;

namespace voxsay
{
    public class ApiProxy : IDisposable
    {
        private HttpClient ConClient;
        private string BaseUri;
        private ProductInfo SelectedProdinfo;
        private Dictionary<int, SpObjectToken> SapiSpeakerList = null;

        private static SpVoice Sapi = new SpVoice();
        private static SpObjectTokenCategory SapiCat = new SpObjectTokenCategory();

        private static HttpClient CheckClient = null;
        private static Dictionary<ProdnameEnum, ProductInfo> ProdList = new Dictionary<ProdnameEnum, ProductInfo>()
        {
            { ProdnameEnum.voicevox, new ProductInfo("127.0.0.1", 50021, "", ProdnameEnum.voicevox) },
            { ProdnameEnum.voicevoxnemo, new ProductInfo("127.0.0.1", 50121, "", ProdnameEnum.voicevoxnemo) },
            { ProdnameEnum.coeiroink, new ProductInfo("127.0.0.1", 50031, "", ProdnameEnum.coeiroink) },
            { ProdnameEnum.coeiroinkv2, new ProductInfo("127.0.0.1", 50032, "/v1", ProdnameEnum.coeiroinkv2) },
            { ProdnameEnum.lmroid, new ProductInfo("127.0.0.1", 50073, "", ProdnameEnum.lmroid) },
            { ProdnameEnum.sharevox, new ProductInfo("127.0.0.1", 50025, "", ProdnameEnum.sharevox) },
            { ProdnameEnum.itvoice, new ProductInfo("127.0.0.1", 49540, "", ProdnameEnum.itvoice) },
            { ProdnameEnum.sapi, new ProductInfo("0.0.0.0", 00000, "", ProdnameEnum.sapi) }
        };

        /// <summary>
        /// 選択されている再生デバイス
        /// </summary>
        public string PlayDeviceName { get; set; }

        /// <summary>
        /// 通信用ラッパー
        /// </summary>
        /// <param name="prodname">接続先製品</param>
        public ApiProxy(string prodname)
        {
            try
            {
                var prod = (ProdnameEnum)Enum.Parse(typeof(ProdnameEnum), prodname);

                if(prod != ProdnameEnum.sapi) // SAPI以外の時
                {
                    SelectedProdinfo = ProdList[prod];

                    ApiProxySub();
                }
                else
                {
                    SelectedProdinfo = ProdList[prod];
                    SapiSpeakerList = GetSapiTakerObjects();
                    ConClient = null;
                    BaseUri = "";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ApiProxy:{0}", e.Message);
            }
        }

        /// <summary>
        /// 通信用ラッパー
        /// </summary>
        /// <param name="prodname">接続先製品</param>
        /// <param name="host">接続先ホスト</param>
        /// <param name="port">接続先ポート</param>
        public ApiProxy(string prodname, string host, int? port)
        {
            try
            {
                var prod = (ProdnameEnum)Enum.Parse(typeof(ProdnameEnum), prodname);

                if (prod != ProdnameEnum.sapi) // SAPI以外の時
                {
                    SelectedProdinfo = ProdList[prod];

                    if (host != null) SelectedProdinfo.Hostname = host;
                    if (port != null) SelectedProdinfo.Portnumber = (int)port;

                    ApiProxySub();
                }
                else
                {
                    SelectedProdinfo = ProdList[prod];
                    SapiSpeakerList = GetSapiTakerObjects();
                    ConClient = null;
                    BaseUri = "";
                }
            }
            catch (Exception e  )
            {
                Console.WriteLine("ApiProxy:{0}", e.Message);
            }
        }

        private void ApiProxySub()
        {
            try
            {
                ConClient = new HttpClient();
                BaseUri = string.Format(@"http://{0}:{1}{2}", SelectedProdinfo.Hostname, SelectedProdinfo.Portnumber, SelectedProdinfo.Context);
            }
            catch (Exception)
            {
                ConClient?.Dispose();
                CheckClient?.Dispose();
                ConClient = null;
                CheckClient = null;
            }
        }

        public void Dispose()
        {
            ConClient?.Dispose();
        }

        /// <summary>
        /// 正しい製品指定か？
        /// </summary>
        /// <param name="prodname">音声合成製品の文字列表現</param>
        /// <returns>扱えるならTrue</returns>
        public static bool IsValidProduct(string prodname)
        {
            ProdnameEnum prod;

            return Enum.TryParse(prodname, out prod) && Enum.IsDefined(typeof(ProdnameEnum), prod);
        }

        /// <summary>
        /// 再生デバイス一覧取得
        /// </summary>
        /// <returns>利用できる再生デバイスの一覧</returns>
        public static List<string> GetMMDeviceList()
        {
            var list = new List<string>();

            foreach (var item in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                list.Add(item.FriendlyName);
            }
            return list;
        }

        /// <summary>
        /// 利用可能確認
        /// </summary>
        /// <returns>利用可能ならtrue</returns>
        public bool CheckConnectivity()
        {
            bool ans = false;
            HttpResponseMessage response = null;

            if (CheckClient is null)
            {
                CheckClient = new HttpClient();
            }

            switch (SelectedProdinfo.Product)
            {
                case ProdnameEnum.sapi:
                    ans = true;    // 固定で使えることにする
                    break;

                case ProdnameEnum.coeiroinkv2:
                default:

                    Task.Run(async () => {
                        try
                        {
                            response = await ConClient.GetAsync(string.Format("{0}/speakers", BaseUri));

                            if (response.StatusCode == System.Net.HttpStatusCode.OK) ans = true;
                        }
                        catch (Exception)
                        {
                            ans = false;
                        }

                    }).Wait();
                    break;
            }

            return ans;
        }

        /// <summary>
        /// 利用可能製品一覧取得
        /// </summary>
        /// <returns>利用可能製品一覧</returns>
        public static List<ProdnameEnum> ConnectivityList()
        {
            List<ProdnameEnum> ans = new List<ProdnameEnum>();
            HttpResponseMessage response = null;
            Task[] tasks = new Task[ProdList.Count - 1]; // SAPI分を引いておく
            int taskarrayIndex = 0;

            if (CheckClient is null)
            {
                CheckClient = new HttpClient();
            }

            foreach (var item in ProdList)
            {
                switch(item.Key)
                {
                    case ProdnameEnum.sapi:
                        ans.Add(item.Value.Product); // 固定で使えることにする
                        break;

                    case ProdnameEnum.coeiroinkv2:
                    default:
                        tasks[taskarrayIndex] = Task.Run(async () => {
                            try
                            {
                                string baseUri = string.Format(@"http://{0}:{1}{2}", item.Value.Hostname, item.Value.Portnumber, item.Value.Context);

                                response = await CheckClient.GetAsync(string.Format("{0}/speakers", baseUri));

                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    ans.Add(item.Value.Product);
                                }
                            }
                            catch (Exception)
                            {
                                //
                            }
                        });
                        taskarrayIndex++;
                        break;
                }
            }

            Task.WaitAll(tasks);

            return ans;
        }

        /// <summary>
        /// 話者パラメタの取り出し
        /// </summary>
        /// <param name="speaker">話者番号（StyleId）</param>
        /// <returns>パラメタ情報</returns>
        public SpeakerParams GetAvatorParams(int speaker)
        {
            SpeakerParams ans;
            switch(SelectedProdinfo.Product)
            {
                // SAPI初期値
                case ProdnameEnum.sapi:
                    ans = new SapiParams();

                    ans.speedScale = 0.0;          // Range: -10.0  ..  10.0 step 1.0
                    ans.volumeScale = 100.0;       // Range:   0.0  .. 100.0 step 1.0
                    break;

                // 各種パラメタ値の取得が出来そうなAPIが見つけられなかったので、GUIに設定されていた設定値デフォルトとして適用する
                case ProdnameEnum.coeiroinkv2:
                    ans = new Coeiroinkv2Params();

                    ans.intonationScale = 1.0;     // Range:  0    .. 2
                    ans.pitchScale = 0.0;          // Range: -0.15 .. 0.15
                    ans.speedScale = 1.0;          // Range:  0.5  .. 2
                    ans.volumeScale = 1.0;         // Range:  0    .. 2
                    ans.prePhonemeLength = 0.1;    // Range:  0    .. 1.5
                    ans.postPhonemeLength = 0.1;   // Range:  0    .. 1.5
                    ans.outputSamplingRate = 44100;
                    break;

                // 面倒なのでVOICEVOXのデフォルト値を適用する。
                default:
                    ans = new VoiceVoxParams();

                    ans.intonationScale = 1.0;     // Range:  0    .. 2
                    ans.pitchScale = 0.0;          // Range: -0.15 .. 0.15
                    ans.speedScale = 1.0;          // Range:  0.5  .. 2
                    ans.volumeScale = 1.0;         // Range:  0    .. 2
                    ans.prePhonemeLength = 0.1;    // Range:  0    .. 1.5
                    ans.postPhonemeLength = 0.1;   // Range:  0    .. 1.5
                    ans.outputSamplingRate = 44100;
                    break;
            }

            return ans;
        }

        /// <summary>
        /// 利用可能な話者の取り出し
        /// </summary>
        /// <returns>話者番号と名称の組み合わせのリスト</returns>
        public List<KeyValuePair<int, string>> AvailableCasts()
        {
            switch(SelectedProdinfo.Product)
            {
                case ProdnameEnum.sapi:
                    return SapiAvailableCasts();

                case ProdnameEnum.coeiroinkv2:
                    return Coeiroinkv2AvailableCasts();

                default:
                    return VoiceVoxAvailableCasts();
            }
        }

        /// <summary>
        /// 利用可能な歌手の取り出し
        /// </summary>
        /// <returns>歌手番号と名称の組み合わせのリスト</returns>
        public List<KeyValuePair<int, string>> AvailableSingers()
        {
            switch (SelectedProdinfo.Product)
            {
                case ProdnameEnum.voicevox:
                    return VoiceVoxAvailableSingers();

                default:
                    return new List<KeyValuePair<int, string>>();
            }
        }

        /// <summary>
        /// 音声保存
        /// </summary>
        /// <param name="speaker">話者番号（StyleId）</param>
        /// <param name="param">エフェクト</param>
        /// <param name="text">発声させるテキスト</param>
        /// <param name="WavFilePath">保存するファイル名</param>
        public bool Save(int speaker, SpeakerParams param, string text, string WavFilePath)
        {
            switch(SelectedProdinfo.Product)
            {
                case ProdnameEnum.sapi:
                    return SynthesisSapi(speaker, param, text, WavFilePath);

                case ProdnameEnum.coeiroinkv2:
                    var coeiroinkv2aq = GetCoeiroinkv2AudioQuery(text, speaker);

                    if ((coeiroinkv2aq != null) && (param != null))
                    {
                        coeiroinkv2aq.volumeScale = param.volumeScale;
                        coeiroinkv2aq.intonationScale = param.intonationScale;
                        coeiroinkv2aq.pitchScale = param.pitchScale;
                        coeiroinkv2aq.speedScale = param.speedScale;
                        coeiroinkv2aq.prePhonemeLength = param.prePhonemeLength;
                        coeiroinkv2aq.postPhonemeLength = param.postPhonemeLength;
                        coeiroinkv2aq.outputSamplingRate = param.outputSamplingRate;
                    }

                    return PostCoeiroinkv2SynthesisQuery(coeiroinkv2aq, speaker, WavFilePath);

                default:
                    var voicevoxaq = GetVoiceVoxAudioQuery(text, speaker);

                    if ((voicevoxaq != null) && (param != null))
                    {
                        voicevoxaq.volumeScale = param.volumeScale;
                        voicevoxaq.intonationScale = param.intonationScale;
                        voicevoxaq.pitchScale = param.pitchScale;
                        voicevoxaq.speedScale = param.speedScale;
                        voicevoxaq.prePhonemeLength = param.prePhonemeLength;
                        voicevoxaq.postPhonemeLength = param.postPhonemeLength;
                        voicevoxaq.outputSamplingRate = param.outputSamplingRate;
                    }

                    return PostVoiceVoxSynthesisQuery(voicevoxaq, speaker, WavFilePath);
            }
        }

        /// <summary>
        /// 発声
        /// </summary>
        /// <param name="speaker">話者番号（StyleId）</param>
        /// <param name="param">エフェクト</param>
        /// <param name="text">発声させるテキスト</param>
        public bool Speak(int speaker, SpeakerParams param, string text)
        {
            switch (SelectedProdinfo.Product)
            {
                case ProdnameEnum.sapi:
                    return SynthesisSapi(speaker, param, text, "");

                case ProdnameEnum.coeiroinkv2:
                    var coeiroinkv2aq = GetCoeiroinkv2AudioQuery(text, speaker);

                    if ((coeiroinkv2aq != null) && (param != null))
                    {
                        coeiroinkv2aq.volumeScale = param.volumeScale;
                        coeiroinkv2aq.intonationScale = param.intonationScale;
                        coeiroinkv2aq.pitchScale = param.pitchScale;
                        coeiroinkv2aq.speedScale = param.speedScale;
                        coeiroinkv2aq.prePhonemeLength = param.prePhonemeLength;
                        coeiroinkv2aq.postPhonemeLength = param.postPhonemeLength;
                        coeiroinkv2aq.outputSamplingRate = param.outputSamplingRate;
                    }

                    return PostCoeiroinkv2SynthesisQuery(coeiroinkv2aq, speaker, "");

                default:
                    var voicevoxaq = GetVoiceVoxAudioQuery(text, speaker);

                    if ((voicevoxaq != null) && (param != null))
                    {
                        voicevoxaq.volumeScale = param.volumeScale;
                        voicevoxaq.intonationScale = param.intonationScale;
                        voicevoxaq.pitchScale = param.pitchScale;
                        voicevoxaq.speedScale = param.speedScale;
                        voicevoxaq.prePhonemeLength = param.prePhonemeLength;
                        voicevoxaq.postPhonemeLength = param.postPhonemeLength;
                        voicevoxaq.outputSamplingRate = param.outputSamplingRate;
                    }

                    return PostVoiceVoxSynthesisQuery(voicevoxaq, speaker, "");
            }
        }

        /// <summary>
        /// 非同期発声
        /// </summary>
        /// <param name="speaker">話者番号（StyleId）</param>
        /// <param name="param">エフェクト</param>
        /// <param name="text">発声させるテキスト</param>
        public void AsyncSpeak(int speaker, SpeakerParams param, string text)
        {
            switch (SelectedProdinfo.Product)
            {
                case ProdnameEnum.sapi:
                    AsyncSynthesisSapi(speaker, param, text);
                    break;

                case ProdnameEnum.coeiroinkv2:
                    var coeiroinkv2aq = GetCoeiroinkv2AudioQuery(text, speaker);

                    if ((coeiroinkv2aq != null) && (param != null))
                    {
                        coeiroinkv2aq.volumeScale = param.volumeScale;
                        coeiroinkv2aq.intonationScale = param.intonationScale;
                        coeiroinkv2aq.pitchScale = param.pitchScale;
                        coeiroinkv2aq.speedScale = param.speedScale;
                        coeiroinkv2aq.prePhonemeLength = param.prePhonemeLength;
                        coeiroinkv2aq.postPhonemeLength = param.postPhonemeLength;
                        coeiroinkv2aq.outputSamplingRate = param.outputSamplingRate;
                    }

                    AsyncPostCoeiroinkv2SynthesisQuery(coeiroinkv2aq, speaker);
                    break;

                default:
                    var voicevoxaq = GetVoiceVoxAudioQuery(text, speaker);

                    if ((voicevoxaq != null) && (param != null))
                    {
                        voicevoxaq.volumeScale = param.volumeScale;
                        voicevoxaq.intonationScale = param.intonationScale;
                        voicevoxaq.pitchScale = param.pitchScale;
                        voicevoxaq.speedScale = param.speedScale;
                        voicevoxaq.prePhonemeLength = param.prePhonemeLength;
                        voicevoxaq.postPhonemeLength = param.postPhonemeLength;
                        voicevoxaq.outputSamplingRate = param.outputSamplingRate;
                    }

                    AsyncPostVoiceVoxSynthesisQuery(voicevoxaq, speaker);
                    break;
            }

        }

        /// <summary>
        /// 歌唱
        /// </summary>
        /// <param name="speaker">話者番号（StyleId）</param>
        /// <param name="param">エフェクト</param>
        /// <param name="mynotes">歌唱させる楽譜</param>
        public bool Sing(int speaker, SpeakerParams param, List<MyNoteInfo> mynotes)
        {
            switch (SelectedProdinfo.Product)
            {
                case ProdnameEnum.voicevox:
                    var voicevoxFrameQuery = GetVoiceVoxFrameAudioQuery(mynotes, 6000); // 現時点では6000固定

                    if ((voicevoxFrameQuery != null) && (param != null))
                    {
                        voicevoxFrameQuery.VolumeScale = param.volumeScale;
                        voicevoxFrameQuery.OutputSamplingRate = param.outputSamplingRate;
                    }

                    return PostVoiceVoxFrameSynthesisQuery(voicevoxFrameQuery, speaker, "");

                default:
                    return false;
            }
        }

        /// <summary>
        /// 非同期歌唱
        /// </summary>
        /// <param name="speaker">話者番号（StyleId）</param>
        /// <param name="param">エフェクト</param>
        /// <param name="mynotes">歌唱させる楽譜</param>
        public void AsyncSing(int speaker, SpeakerParams param, List<MyNoteInfo> mynotes)
        {
            switch (SelectedProdinfo.Product)
            {
                case ProdnameEnum.voicevox:
                    var voicevoxFrameQuery = GetVoiceVoxFrameAudioQuery(mynotes, 6000); // 現時点では6000固定

                    if ((voicevoxFrameQuery != null) && (param != null))
                    {
                        voicevoxFrameQuery.VolumeScale = param.volumeScale;
                        voicevoxFrameQuery.OutputSamplingRate = param.outputSamplingRate;
                    }

                    AsyncPostVoiceVoxFrameSynthesisQuery(voicevoxFrameQuery, speaker);
                    break;

                default:
                    break;
            }

        }

        /// <summary>
        /// 歌唱保存
        /// </summary>
        /// <param name="speaker">話者番号（StyleId）</param>
        /// <param name="param">エフェクト</param>
        /// <param name="mynotes">歌唱させる楽譜</param>
        /// <param name="WavFilePath">保存するファイル名</param>
        public bool SaveSong(int speaker, SpeakerParams param, List<MyNoteInfo> mynotes, string WavFilePath)
        {
            switch (SelectedProdinfo.Product)
            {
                case ProdnameEnum.voicevox:
                    var voicevoxFrameQuery = GetVoiceVoxFrameAudioQuery(mynotes, 6000);// 現時点では6000固定

                    if ((voicevoxFrameQuery != null) && (param != null))
                    {
                        voicevoxFrameQuery.VolumeScale = param.volumeScale;
                        voicevoxFrameQuery.OutputSamplingRate = param.outputSamplingRate;
                    }

                    return PostVoiceVoxFrameSynthesisQuery(voicevoxFrameQuery, speaker, WavFilePath);

                default:

                    return false;
            }
        }

        private void SettingJsonHeader()
        {
            ConClient.DefaultRequestHeaders.Accept.Clear();
            ConClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ConClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/wav"));
            ConClient.DefaultRequestHeaders.Add("User-Agent", "AssistantSeika Driver");
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
                    var response = await ConClient.PostAsync(string.Format(@"{0}/synthesis?speaker={1}", BaseUri, speaker), content);

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

        private bool PostVoiceVoxFrameSynthesisQuery(VoiceVoxFrameAudioQuery query, int speaker, string saveFileName)
        {
            var json = new DataContractJsonSerializer(typeof(VoiceVoxFrameAudioQuery));
            MemoryStream ms = new MemoryStream();
            bool ans = true;

            json.WriteObject(ms, query);

            var content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");

            Task.Run(async () => {

                SettingJsonHeader();

                try
                {
                    var response = await ConClient.PostAsync(string.Format(@"{0}/frame_synthesis?speaker={1}", BaseUri, speaker), content);

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
                    Console.WriteLine("PostVoiceVoxFrameSynthesisQuery:{0}", e.Message);
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
                    var response = await ConClient.PostAsync(string.Format(@"{0}/synthesis", BaseUri), content);

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

        private bool SynthesisSapi(int speaker, SpeakerParams param, string text, string saveFileName)
        {
            bool ans = false;

            if (SapiSpeakerList.Count == 0)
            {
                SapiAvailableCasts();
            }

            Task.Run(() => {

                try
                {
                    var backupSapi = Sapi.Voice;

                    Sapi.Voice = SapiSpeakerList[speaker];

                    SpFileStream ss = new SpFileStream();
                    string tempFileName = saveFileName == "" ? Path.GetTempFileName() : saveFileName;

                    ss.Open(tempFileName, SpeechStreamFileMode.SSFMCreateForWrite);
                    Sapi.AudioOutputStream = ss;

                    Sapi.Volume = Convert.ToInt32(param.volumeScale);
                    Sapi.Rate = Convert.ToInt32(param.speedScale);

                    Thread t = new Thread(() => {
                        Sapi.Speak(text, SpeechVoiceSpeakFlags.SVSFIsXML);
                    });
                    t.SetApartmentState(ApartmentState.STA);
                    t.Start();
                    t.Join();
                    ss.Close();

                    Sapi.Voice = backupSapi;

                    if (saveFileName == "")
                    {
                        PlayWaveFile(tempFileName);
                        File.Delete(tempFileName);
                    }

                    ans = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("SynthesisSapi:{0}", e.Message);
                }
            }).Wait();

            return ans;
        }

        private void AsyncPostVoiceVoxSynthesisQuery(VoiceVoxAudioQuery aq, int speaker)
        {
            var json = new DataContractJsonSerializer(typeof(VoiceVoxAudioQuery));
            MemoryStream ms = new MemoryStream();

            json.WriteObject(ms, aq);

            var content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");

            Task.Run(async () => {

                SettingJsonHeader();

                try
                {
                    var response = await ConClient.PostAsync(string.Format(@"{0}/synthesis?speaker={1}", BaseUri, speaker), content);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string tempFileName = Path.GetTempFileName();

                        using (FileStream tempfile = new FileStream(tempFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(tempfile);
                        }

                        PlayWaveFile(tempFileName);
                        File.Delete(tempFileName);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("AsyncPostVoiceVoxSynthesisQuery:{0}", e.Message);
                }
            });

        }

        private void AsyncPostVoiceVoxFrameSynthesisQuery(VoiceVoxFrameAudioQuery query, int speaker)
        {
            var jsonFrameQuery = new DataContractJsonSerializer(typeof(VoiceVoxFrameAudioQuery));
            MemoryStream ms = new MemoryStream();

            jsonFrameQuery.WriteObject(ms, query);

            var content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");

            Task.Run(async () => {

                SettingJsonHeader();

                try
                {
                    var response = await ConClient.PostAsync(string.Format(@"{0}/frame_synthesis?speaker={1}", BaseUri, speaker), content);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string tempFileName = Path.GetTempFileName();

                        using (FileStream tempfile = new FileStream(tempFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(tempfile);
                        }

                        PlayWaveFile(tempFileName);
                        File.Delete(tempFileName);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("AsyncPostVoiceVoxFrameSynthesisQuery:{0}", e.Message);
                }
            });

        }

        private void AsyncPostCoeiroinkv2SynthesisQuery(Coeiroinkv2AudioQuery aq, int speaker)
        {
            var json = new DataContractJsonSerializer(typeof(Coeiroinkv2AudioQuery));
            MemoryStream ms = new MemoryStream();

            json.WriteObject(ms, aq);

            var content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");

            Task.Run(async () => {

                SettingJsonHeader();

                try
                {
                    var response = await ConClient.PostAsync(string.Format(@"{0}/synthesis", BaseUri), content);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string tempFileName = Path.GetTempFileName();

                        using (FileStream tempfile = new FileStream(tempFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(tempfile);
                        }

                        PlayWaveFile(tempFileName);
                        File.Delete(tempFileName);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("AsyncPostCoeiroinkv2SynthesisQuery:{0}", e.Message);
                }
            });

        }

        private void AsyncSynthesisSapi(int speaker, SpeakerParams param, string text)
        {
            Task.Run(() => {

                try
                {
                    var backupSapi = Sapi.Voice;

                    Sapi.Voice = SapiSpeakerList[speaker];

                    SpFileStream ss = new SpFileStream();
                    string tempFileName = Path.GetTempFileName();

                    ss.Open(tempFileName, SpeechStreamFileMode.SSFMCreateForWrite);
                    Sapi.AudioOutputStream = ss;

                    Sapi.Volume = Convert.ToInt32(param.volumeScale);
                    Sapi.Rate = Convert.ToInt32(param.speedScale);

                    Thread t = new Thread(() => {
                        Sapi.Speak(text, SpeechVoiceSpeakFlags.SVSFIsXML);
                    });
                    t.SetApartmentState(ApartmentState.STA);
                    t.Start();
                    t.Join();
                    ss.Close();

                    Sapi.Voice = backupSapi;

                    PlayWaveFile(tempFileName);
                }
                catch (Exception e)
                {
                    Console.WriteLine("SynthesisSapi:{0}", e.Message);
                }
            });

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
                    var response = await ConClient.PostAsync(url, content);

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

        private VoiceVoxFrameAudioQuery GetVoiceVoxFrameAudioQuery(List<MyNoteInfo> mynotes, int speaker)
        {
            string url = string.Format(@"{0}/sing_frame_audio_query?speaker={1}", BaseUri, speaker);
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
            VoiceVoxFrameAudioQuery ans = null;

            settings.UseSimpleDictionaryFormat = true;

            var Score = new VoiceVoxNotes();
            
            Score.Notes = new List<VoiceVoxNote>();
            foreach (var note in mynotes)
            {
                var noteobj = new VoiceVoxNote();
                noteobj.Lyric = note.Lyric;
                noteobj.Key = note.Key;
                noteobj.Frame_Length = Convert.ToInt32(note.FrameLength);

                if ((note.Lyric == "") && (note.Note.Substring(0, 1) == "R"))
                {
                    noteobj.Key = null;
                }
                Score.Notes.Add(noteobj);
            }

            var jsonNotes = new DataContractJsonSerializer(typeof(List<VoiceVoxNotes>));
            MemoryStream ms = new MemoryStream();

            jsonNotes.WriteObject(ms, Score);

            var content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");

            Task.Run(async () => {
                SettingJsonHeader();

                try
                {
                    var response = await ConClient.PostAsync(url, content);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var json = new DataContractJsonSerializer(typeof(VoiceVoxFrameAudioQuery), settings);
                        ans = (VoiceVoxFrameAudioQuery)json.ReadObject(await response.Content.ReadAsStreamAsync());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("GetVoiceVoxFrameAudioQuery:{0}", e.Message);
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
                    var response1 = await ConClient.PostAsync(url_getspeakerinfo, content_speaker);

                    if (response1.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var json = new DataContractJsonSerializer(typeof(Coeiroinkv2StyleidToSpeakerMeta), settings);
                        speakerinfo = (Coeiroinkv2StyleidToSpeakerMeta)json.ReadObject(await response1.Content.ReadAsStreamAsync());

                        ans.speakerUuid = speakerinfo.speakerUuid;
                        ans.styleId = speakerinfo.styleId;
                    }

                    var response2 = await ConClient.PostAsync(url_estimateprosody, content_estimateprosody);

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

        private List<KeyValuePair<int, string>> VoiceVoxAvailableCasts()
        {
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
            List<VoiceVoxSpeaker> speakers = new List<VoiceVoxSpeaker>();
            var ans = new List<KeyValuePair<int, string>>();

            Task.Run(async () => {
                SettingJsonHeader();

                try
                {
                    var response = await ConClient.GetAsync(string.Format("{0}/speakers", BaseUri));

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
                    Console.WriteLine("VoiceVoxAvailableCasts:{0}", e.Message);
                    ans = null;
                }
            }).Wait();

            return ans;
        }

        private List<KeyValuePair<int, string>> VoiceVoxAvailableSingers()
        {
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
            List<VoiceVoxSingers> speakers = new List<VoiceVoxSingers>();
            var ans = new List<KeyValuePair<int, string>>();

            Task.Run(async () => {
                SettingJsonHeader();

                try
                {
                    var response = await ConClient.GetAsync(string.Format("{0}/singers", BaseUri));

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var json = new DataContractJsonSerializer(typeof(List<VoiceVoxSingers>), settings);

                        speakers = (List<VoiceVoxSingers>)json.ReadObject(await response.Content.ReadAsStreamAsync());

                        ans = speakers.SelectMany(v1 => v1.styles.Select(v2 => new { id = v2.Id, speaker_uuid = v1.speaker_uuid, name = string.Format("{0}（{1}）", v1.name, v2.Name) }))
                                      .OrderBy(v => v.id)
                                      .Select(v => new KeyValuePair<int, string>(v.id, v.name)).ToList();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("VoiceVoxAvailableSingers:{0}", e.Message);
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
                    var response = await ConClient.GetAsync(string.Format("{0}/speakers", BaseUri));

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
                    Console.WriteLine("Coeiroinkv2AvailableCasts:{0}", e.Message);
                    ans = null;
                }
            }).Wait();

            return ans;
        }

        private Dictionary<int, SpObjectToken> GetSapiTakerObjects()
        {
            var ans = new Dictionary<int, SpObjectToken>();

            try
            {
                ans.Clear();
                SapiCat.SetId(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices", false);

                int idx = 0;
                foreach (SpObjectToken token in SapiCat.EnumerateTokens())
                {
                    ans.Add(idx, token);
                    idx++;
                }
                foreach (SpObjectToken token in Sapi.GetVoices("", ""))
                {
                    if (!ans.ContainsValue(token))
                    {
                        ans.Add(idx, token);
                        idx++;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("GetSapiTakerObjects:{0}", e.Message);
                ans = null;
            }

            return ans;
        }

        private List<KeyValuePair<int, string>> SapiAvailableCasts()
        {
            var ans = new List<KeyValuePair<int, string>>();

            try
            {
                int idx = 0;
                foreach (SpObjectToken token in SapiCat.EnumerateTokens())
                {
                    ans.Add(new KeyValuePair<int, string>(idx, token.GetDescription()));
                    idx++;
                }
                foreach (SpObjectToken token in Sapi.GetVoices("", ""))
                {
                    if (!SapiSpeakerList.ContainsValue(token))
                    {
                        ans.Add(new KeyValuePair<int, string>(idx, token.GetDescription()));
                        idx++;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("SapiAvailableCasts:{0}", e.Message);
                ans = null;
            }

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

    }

}
