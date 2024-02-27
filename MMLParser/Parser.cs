using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MMLParser
{
    /// <summary>
    /// MMLパーサ
    /// </summary>
    public class Parser
    {
        private int currentTempo;
        private int currentOctave;
        private string currentNotelen;
        private readonly LyricParser lyricParser;
        private readonly MMLParser mmlParser;

        private const double tickDuration = 0.00104166666666666666666666666667; // BPM=120時、1tick辺りの時間

        /// <summary>
        /// 音符の長さと係数のマップ
        /// </summary>
        private readonly Dictionary<string, double> NoteLengthToCoefficientMap = new Dictionary<string, double>()
        {
            {   "1.",  9.0     },
            {   "1" ,  6.0     },
            {   "2.",  3.0     },
            {   "2" ,  2.0     },
            {   "4.",  1.5     },
            {   "4" ,  1       },
            {   "8.",  0.75    },
            {   "8" ,  0.5     },
            {  "16.",  0.375   },
            {  "16" ,  0.25    },
            {  "32.",  0.1875  },
            {  "32" ,  0.125   },
            {  "64.",  0.09375 },
            {  "64",   0.0625  },
            { "128.",  0.046875},
            { "128" ,  0.03125 }
        };

        /// <summary>
        /// オクターブと最初の C のキー位置のマップ
        /// </summary>
        private readonly Dictionary<int, int> OctaveToKeyMap = new Dictionary<int, int>()
        {
            { 0,  12},
            { 1,  24},
            { 2,  36},
            { 3,  48},
            { 4,  60},
            { 5,  72},
            { 6,  84},
            { 7,  96},
            { 8, 108},
            { 9, 120},
        };

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
        /// ノートとキー相対位置のマップ
        /// </summary>
        private readonly Dictionary<string, int> NoteToKeyDispMap = new Dictionary<string, int>()
        {
            { "C",  0},
            { "C#", 1},
            { "C+", 1},
            { "D-", 1},
            { "D",  2},
            { "D#", 3},
            { "D+", 3},
            { "E-", 3},
            { "E",  4},
            { "F",  5},
            { "F#", 6},
            { "F+", 6},
            { "G-", 6},
            { "G",  7},
            { "G#", 8},
            { "G+", 8},
            { "A-", 8},
            { "A",  9},
            { "A#",10},
            { "A+",10},
            { "B-",10},
            { "B", 11}
        };

        /// <summary>
        /// 1tick辺りの時間
        /// </summary>
        public double TickDuration
        {
            get
            {
                return tickDuration;
            }
        }

        /// <summary>
        /// 楽譜に設定するフレーム数計算
        /// </summary>
        /// <param name="tempo">テンポ</param>
        /// <param name="notelen">ノート長</param>
        /// <returns>フレーム数</returns>
        public int CalcFrameLength(int tempo, string notelen)
        {
            double Ticks = (60.0 / tempo) / tickDuration; // 指定テンポ時４分音符のフレーム数

            return Convert.ToInt32(Ticks * NoteLengthToCoefficientMap[notelen]);
        }

        /// <summary>
        /// 楽譜に設定するフレーム数計算
        /// </summary>
        /// <param name="notelen">ノート長</param>
        /// <returns>フレーム数</returns>
        public int CalcFrameLength(string notelen)
        {
            double Ticks = (60.0 / Bpm) / tickDuration; // 指定テンポ時４分音符のフレーム数

            return Convert.ToInt32(Ticks * NoteLengthToCoefficientMap[notelen]);
        }

        /// <summary>
        /// キー相対位置からノートを計算する
        /// </summary>
        /// <param name="key">キー 0～127</param>
        /// <returns>対応するノート</returns>
        private string KeyToNote(int key)
        {
            return KeyDispToNoteMap[key % 12];
        }

        /// <summary>
        /// 既定のテンポを再設定
        /// </summary>
        public int Bpm
        {
            get
            {
                return currentTempo;
            }

            set
            {
                if ((value >= 30) && (value <= 800)) currentTempo = value;
            }
        }

        /// <summary>
        /// 既定のオクターブをを再設定
        /// </summary>
        public int Octave
        {
            get
            {
                return currentOctave;
            }

            set
            {
                if (OctaveToKeyMap.ContainsKey(value)) currentOctave = value;
            }
        }

        /// <summary>
        /// 既定の音符・休符長さを再設定
        /// </summary>
        public string DefaultNoteLen
        {
            get
            {
                return currentNotelen;
            }

            set
            {
                if (NoteLengthToCoefficientMap.ContainsKey(value)) currentNotelen = value;
            }
        }

        /// <summary>
        /// パーサー
        /// </summary>
        public Parser()
        {
            // デフォルト T120, O4, L4 に設定

            Bpm = 120;
            Octave = 4;
            DefaultNoteLen = "4";
            mmlParser = new MMLParser(Bpm, Octave, DefaultNoteLen);
            lyricParser = new LyricParser();
        }

        /// <summary>
        /// 歌詞、MMLの文字列からノート情報のリストを生成する
        /// </summary>
        /// <param name="singtext">歌詞、MMLの文字列</param>
        /// <returns>生成されたノード情報のリスト</returns>
        /// <exception cref="Exception"></exception>
        public List<NoteInfo> ParseSingString(string singtext)
        {
            List<NoteInfo> mynoteinfo = new List<NoteInfo>();

            List<List<LyricInfo>> lyriclist;
            List<MMLInfo> mmllist;
            List<NoteInfo> notelist;

            // カンマで分割した単位で解析する
            foreach (var measure in Regex.Split(singtext, @"[,]"))
            {
                try
                {
                    var parts = Regex.Split(measure.Trim(), @"[:：]+");
                    switch (parts.Length)
                    {
                        case 1: // MMLだけある場合
                            mmllist = mmlParser.ParseMMLString(parts[0].Trim());
                            notelist = GenNoteInfoFromMMLInfo(ref mmllist);
                            break;

                        case 2:  // 歌詞とMMLがある場合
                            lyriclist = lyricParser.ParseLyricString(parts[0].Trim());
                            mmllist = mmlParser.ParseMMLString(parts[1].Trim());
                            notelist = GenNoteInfoFromMMLInfo(ref mmllist);
                            AssignLyricToNoteInfo(ref lyriclist, ref notelist);
                            break;

                        default:
                            throw new Exception(string.Format(@"Incorrect definition."));
                    }

                    // 解析結果を追加

                    mynoteinfo.AddRange(notelist);

                }
                catch (Exception e)
                {
                    throw new Exception(string.Format(@"measure:[{0}], {1}", measure.Trim(), e.Message), e);
                }
            }

            return mynoteinfo;
        }

        /// <summary>
        /// MML解析結果をノート情報に変換する
        /// </summary>
        /// <param name="mmllist">MML解析結果のリスト</param>
        /// <returns>MML解析結果から生成したノート情報リスト</returns>
        private List<NoteInfo> GenNoteInfoFromMMLInfo(ref List<MMLInfo> mmllist)
        {
            List<NoteInfo> mynotes = new List<NoteInfo>();

            foreach (var item in mmllist)
            {
                string macro = item.MacroName.Substring(0, 1);
                switch (macro)
                {
                    case "T":
                        Bpm = item.Tempo;
                        break;

                    case "O":
                        Octave = item.Octave;
                        break;

                    case "L":
                        DefaultNoteLen = item.NoteLen;
                        break;

                    case ">":
                    case "<":
                        Octave = item.Octave;
                        break;

                    case "S":
                        break;

                    default:
                        // 音符・休符の生成
                        var noteItem = new NoteInfo();
                        noteItem.Note = item.MacroName;
                        noteItem.NoteLen = item.NoteLen;
                        noteItem.Key = macro == "N" ? item.Key : OctaveToKeyMap[item.Octave] + (macro == "R" ? 0 : NoteToKeyDispMap[item.MacroName]);
                        noteItem.Ticks = CalcFrameLength(Bpm, noteItem.NoteLen);
                        noteItem.Tempo = Bpm;
                        noteItem.Notes = new List<SubNoteInfo>();
                        mynotes.Add(noteItem);

                        var subNoteItem = new SubNoteInfo();
                        subNoteItem.Note = item.MacroName;
                        subNoteItem.NoteLen = item.NoteLen;
                        subNoteItem.Key = macro == "N" ? item.Key : OctaveToKeyMap[item.Octave] + (macro == "R" ? 0 : NoteToKeyDispMap[item.MacroName]);
                        subNoteItem.Ticks = CalcFrameLength(Bpm, subNoteItem.NoteLen);
                        subNoteItem.Tempo = Bpm;
                        subNoteItem.Lyric = item.SampleLyric;
                        subNoteItem.defaultLyric = true;
                        noteItem.Notes.Add(subNoteItem);

                        break;
                }
            }

            return mynotes;
        }

        /// <summary>
        /// ノート情報に歌詞を割り当てる
        /// </summary>
        /// <param name="lyriclist">歌詞解析結果のリスト</param>
        /// <param name="notelist">ノート情報のリスト</param>
        private void AssignLyricToNoteInfo(ref List<List<LyricInfo>> lyriclist, ref List<NoteInfo> notelist)
        {
            int lyricindex = 0;
            int noteIndex = 0;

            if (lyriclist.Count == 0) return;

            while (noteIndex < notelist.Count)
            {
                string macro = notelist[noteIndex].Note.Substring(0, 1);
                switch (macro) {
                    case "T":
                    case "O":
                    case "L":
                        noteIndex++;
                        break;

                    case "R":
                        noteIndex++;
                        break;

                    default:
                        // C～Bの音符に歌詞を割り当てる
                        if(lyriclist.Count > lyricindex)
                        {
                            if (lyriclist[lyricindex].Count == 1)
                            {
                                // 音符を分割しない場合
                                notelist[noteIndex].Notes[0].Lyric = lyriclist[lyricindex][0].Lyric;
                                notelist[noteIndex].Notes[0].defaultLyric = false;
                                notelist[noteIndex].Notes[0].Key += lyriclist[lyricindex][0].keyUpdown;
                                lyricindex++;
                                noteIndex++;
                            }
                            else
                            {
                                // 音符が分割される場合

                                // 歌詞の数でフレームを分割
                                var dividedNoteFrames = notelist[noteIndex].Ticks  / lyriclist[lyricindex].Count;

                                var localKey = notelist[noteIndex].Notes[0].Key;
                                var localTempo = notelist[noteIndex].Notes[0].Tempo;

                                // 不足分の音符追加
                                for (int cnt = 0; cnt < (lyriclist[lyricindex].Count - 1); cnt++)
                                {
                                    var newSubNoteItem = new SubNoteInfo();
                                    notelist[noteIndex].Notes.Insert(noteIndex, newSubNoteItem);
                                }

                                // 歌詞の再割り当て
                                var subItemIndex = 0;
                                for (int cnt = 0; cnt < lyriclist[lyricindex].Count; cnt++)
                                {
                                    notelist[noteIndex].Notes[subItemIndex].Key = localKey + lyriclist[lyricindex][cnt].keyUpdown;
                                    notelist[noteIndex].Notes[subItemIndex].Note = KeyToNote(notelist[noteIndex].Notes[subItemIndex].Key);
                                    notelist[noteIndex].Notes[subItemIndex].NoteLen = ""; // 奇数に分割されたら一致しないので保持を止める
                                    notelist[noteIndex].Notes[subItemIndex].Lyric = lyriclist[lyricindex][cnt].Lyric;
                                    notelist[noteIndex].Notes[subItemIndex].defaultLyric = false;
                                    notelist[noteIndex].Notes[subItemIndex].Tempo = localTempo;
                                    notelist[noteIndex].Notes[subItemIndex].Ticks = dividedNoteFrames;

                                    subItemIndex++;
                                }

                                lyricindex++;
                                noteIndex++;
                            }
                        }
                        else
                        {
                            noteIndex++;
                        }
                        break; 
                }
            }
        }

    }
}
