using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using voxsay.Base.VoiceVox;
using MMLParser;
using System.Runtime.Serialization.Json;
using System.IO;

namespace voxsay
{
    public class VoiceVoxNoteGenerator
    {
        //private int currentTempo;
        //private int currentOctave;
        //private string currentNotelen;
        private Parser mmlParser;

        private const double frameDuration = 0.01075268817204301075268817204301;    // BPM=120時、１フレームの時間
        private const double framsQuarterNote = 46.5;                               // BPM=120時、４分音符のフレーム数
        private const double frameConvRate = 0.096875;                              // 46.5 ÷ 480 , VOICEVOXフレーム数への変換レート

        /// <summary>
        /// キー相対位置とノートのマップ
        /// </summary>
        private readonly Dictionary<int, string> KeyDispToNoteMap = new Dictionary<int, string>()
        {
            {  0, "C" },
            {  1, "C#"},
            {  2, "D" },
            {  3, "D#"},
            {  4, "E" },
            {  5, "F" },
            {  6, "F#"},
            {  7, "G" },
            {  8, "G#"},
            {  9, "A" },
            { 10, "A#"},
            { 11, "B" }
        };

        /// <summary>
        /// 1tick辺りの時間
        /// </summary>
        public double FrameDuration
        {
            get
            {
                return frameDuration;
            }
        }

        /// <summary>
        /// 4分音符当たりのフレーム数
        /// </summary>
        public double FramsQuarterNote
        {
            get
            {
                return framsQuarterNote;
            }
        }

        /// <summary>
        /// キーからノートを計算する
        /// </summary>
        /// <param name="key">キー 0～127</param>
        /// <returns>対応するノート</returns>
        private string KeyToNote(int key)
        {
            return KeyDispToNoteMap[key % 12];
        }

        /// <summary>
        /// キーからオクターブを計算する
        /// </summary>
        /// <param name="key">キー 12～127</param>
        /// <returns>対応するノート</returns>
        private int KeyToOctave(int key)
        {
            if (key < 12) throw new ArgumentException("範囲が不正");
            return (key / 12) - 1;
        }

        /// <summary>
        /// 楽譜生成
        /// </summary>
        public VoiceVoxNoteGenerator()
        {
            // デフォルト T120, O4, L4 に設定

            mmlParser = new Parser();
            mmlParser.Bpm = 120;
            mmlParser.Octave = 4;
            mmlParser.DefaultNoteLen = "4";
        }

        /// <summary>
        /// 解析を行って情報リストを作成する
        /// </summary>
        /// <param name="singtext">MML文字列、または歌詞付きのMML文字列</param>
        /// <returns>解析結果の情報リスト</returns>
        /// <exception cref="Exception">解析中のエラー(書式エラーなど)</exception>
        public List<NoteInfo> ParseSingString(string singtext)
        {
            try
            {
                return mmlParser.ParseSingString(singtext);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(@"{0}", e.Message), e);
            }
        }

        /// <summary>
        /// 解析を行って情報リストを作成する
        /// </summary>
        /// <param name="filePath">MML文字列、または歌詞付きのMML文字列格納ファイル</param>
        /// <returns>解析結果の情報リスト</returns>
        /// <exception cref="Exception">解析中のエラー(書式エラーなど)</exception>
        public List<List<NoteInfo>> ParseSingFile(string filePath)
        {
            try
            {
                return mmlParser.ParseSingFile2(filePath);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(@"{0}", e.Message), e);
            }
        }

        /// <summary>
        /// 情報リストから VOICEVOX API /sing_frame_audio_query で利用する楽譜情報へ変換する
        /// </summary>
        /// <param name="mynoteinfo">情報リスト</param>
        /// <returns>楽譜情報(VOICEVOXのノート情報)</returns>
        /// <exception cref="Exception">変換中のエラー</exception>
        public VoiceVoxNotes ConvertScoreInfo(List<NoteInfo> mynoteinfo)
        {
            var voiceVoxNotes = new VoiceVoxNotes();
            voiceVoxNotes.Notes = new List<VoiceVoxNote>();

            try
            {
                // 最初に休符を入れておく
                var noteRinfo = new VoiceVoxNote
                {
                    Lyric = "",
                    Frame_Length = 2, // 多分最小
                    Key = null,
                    NoteLen = "R"
                };
                voiceVoxNotes.Notes.Add(noteRinfo);

                foreach (var noteItem in mynoteinfo)
                {
                    // 音符・休符分割前のフレーム数に変換レートを掛ける
                    var parentFrames = Convert.ToInt32(noteItem.Ticks * frameConvRate);

                    int frames = 0;
                    foreach (var subNoteItem in noteItem.Notes)
                    {

                        // 音符分割後のフレーム数に変換レートを掛ける
                        var subFrames = Convert.ToInt32(subNoteItem.Ticks * frameConvRate);

                        var note = new VoiceVoxNote
                        {
                            Key = subNoteItem.Key,
                            Lyric = subNoteItem.Lyric,
                            Frame_Length = subFrames,
                            NoteLen = subNoteItem.Note + subNoteItem.NoteLen
                        };

                        // 分割した音符のフレーム数合計
                        frames += subFrames;

                        // 休符の時はキーをnullにする
                        if (subNoteItem.Note == "R") note.Key = null;

                        voiceVoxNotes.Notes.Add(note);
                    }
                    // 分割した音符のフレーム数合計と分割前音符のフレーム数に差異があれば、分割した音符で最後の物に差分を足し込む
                    voiceVoxNotes.Notes[voiceVoxNotes.Notes.Count - 1].Frame_Length += (parentFrames - frames);
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(@"{0}", e.Message), e);
            }

            return voiceVoxNotes;
        }

        /// <summary>
        /// 情報リストから VOICEVOX API /sing_frame_audio_query で利用する楽譜情報へ変換する
        /// </summary>
        /// <param name="mynoteinfo">情報リスト</param>
        /// <returns>楽譜情報(VOICEVOXのノート情報)</returns>
        /// <exception cref="Exception">変換中のエラー</exception>
        public VoiceVoxNotes ConvertScoreInfo(List<List<NoteInfo>> mynoteinfo)
        {
            var voiceVoxNote = new VoiceVoxNotes();
            int linecounter = 1;
            bool rskip = false;

            voiceVoxNote.Notes = new List<VoiceVoxNote>();
            try
            {
                foreach(var listItem in mynoteinfo)
                {
                    var scoreitem = ConvertScoreInfo(listItem);

                    if(!rskip)
                    {
                        voiceVoxNote.Notes.AddRange(scoreitem.Notes);
                        rskip = true;
                    }
                    else
                    {
                        if ((scoreitem.Notes[0].Frame_Length == 2) && (scoreitem.Notes[0].Key is null))
                        {
                            voiceVoxNote.Notes.AddRange(scoreitem.Notes.Where((v,idx)=>idx>0).ToList());
                        }
                        else
                        {
                            voiceVoxNote.Notes.AddRange(scoreitem.Notes);
                        }
                    }

                    linecounter++;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(@"line {0}, {1}", linecounter, e.Message), e);
            }

            return voiceVoxNote;
        }

        /// <summary>
        /// 情報リストから VOICEVOX API /sing_frame_audio_query で利用する楽譜情報リストへ変換する
        /// </summary>
        /// <param name="mynoteinfo">情報リスト</param>
        /// <returns>楽譜情報(VOICEVOXのノート情報)</returns>
        /// <exception cref="Exception">変換中のエラー</exception>
        public List<VoiceVoxNotes> ConvertScoreInfoList(List<List<NoteInfo>> mynoteinfo)
        {
            var voiceVoxNoteList = new List<VoiceVoxNotes>();
            int linecounter = 1;

            try
            {
                foreach (var listItem in mynoteinfo)
                {
                    var scoreitem = ConvertScoreInfo(listItem);

                    if(scoreitem.Notes.Count > 0) voiceVoxNoteList.Add(ConvertScoreInfo(listItem));

                    linecounter++;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(@"line {0}, {1}", voiceVoxNoteList.Count - 1, e.Message), e);
            }

            return voiceVoxNoteList;
        }

        /// <summary>
        /// VOICEVOX API /sing_frame_audio_query で利用する楽譜情報をJSONでエクスポートする
        /// </summary>
        /// <param name="mynotes">ノート情報</param>
        /// <returns>JSON(文字列) </returns>
        public string ExportNotes(VoiceVoxNotes mynotes)
        {
            string ans;
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
            settings.UseSimpleDictionaryFormat = true;

            using (MemoryStream ms = new MemoryStream())
            using (var jw = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.UTF8, true, true))
            {
                var jsonNotes = new DataContractJsonSerializer(typeof(List<VoiceVoxNotes>));

                jsonNotes.WriteObject(jw, mynotes);
                jw.Flush();
                ans = Encoding.UTF8.GetString(ms.ToArray());

                jw?.Close();
                ms?.Close();
            }

            return ans;

        }

        /// <summary>
        /// 楽譜情報の内容を表示する
        /// </summary>
        /// <param name="score">ノート情報</param>
        public void PrintAssignInfo(VoiceVoxNotes score)
        {
            Console.WriteLine(@"   # KEY FRAMES O NOTE   Lyric");
            Console.WriteLine(@"---- --- ------ - ------ ---------------");

            int noteindex = 0;
            foreach (var note in score.Notes)
            {
                if(note.Key is null)
                {
                    // "R"
                    Console.WriteLine(@"{0,4:D}     {1,6:D} {2,1:G} {3,-6:D}", noteindex, note.Frame_Length, "", note.NoteLen);
                }
                else
                {
                    // other
                    var noteStr = KeyToNote((int)note.Key);
                    var noteOctave = KeyToOctave((int)note.Key);

                    Console.WriteLine(@"{0,4:D} {1,3:G} {2,6:D} {3,1:G} {4,-6:D} {5}", noteindex, note.Key, note.Frame_Length, noteOctave, note.NoteLen, note.Lyric);
                }

                noteindex++;
            }

            Console.WriteLine(@"");

        }

        /// <summary>
        /// 楽譜情報の内容を表示する
        /// </summary>
        /// <param name="score">ノート情報</param>
        /// <param name="line">行</param>
        public void PrintAssignInfo(VoiceVoxNotes score, int line)
        {
            Console.WriteLine(@"LINE {0}", line);

            PrintAssignInfo(score);
        }
    }
}
