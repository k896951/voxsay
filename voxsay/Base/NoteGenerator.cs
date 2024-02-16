using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using voxsay.Base.VoiceVox;

namespace voxsay
{
    public class NoteGenerator
    {
        private const double FrameDurationParTick = 0.01;
        private const double QuarterNoteFrameLength = 50.0; // BPM=120

        private const string MacroMatchReg = @"^[TOLRCDEFGAB]\d{0,}\.{0,1}[#+-]{0,1}";
        private const string MacroNumMatchReg = @"\d{1,}";
        private const string MacroDotMatchReg = @"\.";
        private const string MacroKeyModifierMatchReg = @"[#-+]";
        private const string MacroNoteLenMatchReg = @"\d{0,}\.{0,1}";

        private int tempo = 120;
        private int octave = 4;

        //private int currentNoteLen;
        //private int currentKeyBase;

        private Dictionary<int, double> NoteLengthToFrameLengthMap = new Dictionary<int, double>()
        {
            {  1, 200.0 },
            {  2, 100.0 },
            {  4,  50.0 },
            {  8,  25.0 },
            { 16,  12.5 },
            { 32,   6.25}
        };

        private Dictionary<int, int> OctaveToKeyMap = new Dictionary<int, int>()
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

        private Dictionary<string, string> NoteToSampleLyricMap = new Dictionary<string, string>()
        {
            { "C", "ど"  },
            { "D", "れ"  },
            { "E", "み"  },
            { "F", "ふぁ"},
            { "G", "そ"  },
            { "A", "ら"  },
            { "B", "し"  }
        };

        private Dictionary<string, int> NoteToKeyDispMap = new Dictionary<string, int>()
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

        private void Bpm(int value)
        {
            // たぶん800が限界

            tempo = value;

            double magnification = Convert.ToDouble(60.0 / tempo) / FrameDurationParTick;

            NoteLengthToFrameLengthMap[1] = magnification * 4;  // 　　全音符フレーム数
            NoteLengthToFrameLengthMap[2] = magnification * 2;  // 　２分音符フレーム数
            NoteLengthToFrameLengthMap[4] = magnification;      // 　４分音符フレーム数
            NoteLengthToFrameLengthMap[8] = magnification / 2;  // 　８分音符フレーム数
            NoteLengthToFrameLengthMap[16] = magnification / 4; // １６分音符フレーム数
            NoteLengthToFrameLengthMap[32] = magnification / 8; // ３２分音符フレーム数
        }

        private int Octave
        {
            get
            {
                return octave;
            }

            set
            {
                if (OctaveToKeyMap.ContainsKey(value)) octave = value;
            }
        }

        public NoteGenerator()
        {
            Bpm(120);
        }

        public List<MyNoteInfo> ParseSingString(string singtext)
        {
            List<MyNoteInfo> ans = new List<MyNoteInfo>();
            int currentNoteListIndex = 0;
            int currentNoteLen = 4;
            int currentKeyBase = OctaveToKeyMap[octave];

        // 最初にごく短い休符を入れておく
        ans.Add(new MyNoteInfo());
            ans[0].Lyric = "";
            ans[0].Note = "R";
            ans[0].Key = OctaveToKeyMap[octave];
            ans[0].FrameLength = 2;

            foreach (var measure in Regex.Split(singtext, @"[,]"))
            {
                try
                {
                    var parts = Regex.Split(measure.Trim(), @"[:]+");
                    switch (parts.Length)
                    {
                        case 1:
                            ParseMMLString(parts[0].Trim(), ref currentNoteLen, ref currentKeyBase, ref ans);
                            break;

                        case 2:
                            ParseMMLString(parts[1].Trim(), ref currentNoteLen, ref currentKeyBase, ref ans);
                            ParseLyricString(parts[0].Trim(), ref currentNoteListIndex, ref ans); // ParseMMLString()のあとに呼ぶ
                            break;

                        default:
                            throw new Exception(string.Format(@"Incorrect definition."));
                    }

                    currentNoteListIndex = ans.Count;
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format(@"measure:[{0}], {1}", measure.Trim(), e.Message));
                }
            }

            return ans;
        }

        public string ExportNotes(List<MyNoteInfo> mynotes)
        {
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();

            settings.UseSimpleDictionaryFormat = true;

            var Score = new VoiceVoxNotes();

            Score.Notes = new List<VoiceVoxNote>();
            foreach (var note in mynotes)
            {
                var noteobj = new VoiceVoxNote();
                noteobj.Lyric = note.Lyric;
                noteobj.Key = note.Key;
                noteobj.Frame_Length = Convert.ToInt32(note.FrameLength);

                if ((note.Lyric == "") && (note.Note == "R"))
                {
                    noteobj.Key = null;
                }
                Score.Notes.Add(noteobj);
            }

            var jsonNotes = new DataContractJsonSerializer(typeof(List<VoiceVoxNotes>));
            MemoryStream ms = new MemoryStream();

            jsonNotes.WriteObject(ms, Score);

            var ans = Encoding.UTF8.GetString(ms.ToArray());

            return ans;
        }

        private void ParseMMLString(string mml, ref int currentnotelen, ref int currentkeybase, ref List<MyNoteInfo> mynotes)
        {
            string localMML = mml.ToUpper();
            int localNumPart;

            int addpos;
            for (var pos = 0; pos < localMML.Length; pos += addpos)
            {
                // MMLマクロの書式に合致しないならエラー
                if (!Regex.IsMatch(localMML.Substring(pos), MacroMatchReg ))
                {
                    throw new Exception(string.Format(@"column {0}, '{1}' is unknown.", pos + 1, localMML.Substring(pos, 1)));
                }

                // 処理対象部分の切り出し
                string token = Regex.Match(localMML.Substring(pos), MacroMatchReg).Value;
                addpos = token.Length;

                // 数値の指定があるか否か
                var num = Regex.IsMatch(token, MacroNumMatchReg);

                // "."の指定があるか否か
                var dot = Regex.IsMatch(token, MacroDotMatchReg);

                // キー修飾(#,+,-)があるか否か
                var keyModify = Regex.IsMatch(token, MacroKeyModifierMatchReg);

                // 各マクロ処理
                string macro = token.Substring(0, 1);
                switch (macro)
                {
                    case "T":
                        // テンポの変更
                        if(dot || keyModify || !num)
                        {
                            throw new Exception(string.Format(@"column {0}, {1} is syntax error.", pos + 1, token));
                        }

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out localNumPart);
                        Bpm(localNumPart);
                        break;

                    case "O":
                        // オクターブの変更（基準キー位置の変更）
                        if (dot || keyModify || !num)
                        {
                            throw new Exception(string.Format(@"column {0}, {1} is syntax error.", pos + 1, token));
                        }

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out localNumPart);
                        if (!OctaveToKeyMap.ContainsKey(localNumPart))
                        {
                            throw new Exception(string.Format(@"column {0}, {1} is out of range.", pos + 2, localNumPart));
                        }
                        Octave = localNumPart;
                        currentkeybase = OctaveToKeyMap[Octave];
                        break;

                    case "L":
                        // 音符・休符のデフォルト長変更
                        if (keyModify || !num)
                        {
                            throw new Exception(string.Format(@"column {0}, {1} is syntax error.", pos + 1, token));
                        }

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out localNumPart);
                        if (!NoteLengthToFrameLengthMap.ContainsKey(localNumPart))
                        {
                            throw new Exception(string.Format(@"column {0}, {1} is out of range.", pos + 2, localNumPart));
                        }
                        currentnotelen = localNumPart;
                        break;

                    case "R":
                    case "C":
                    case "D":
                    case "E":
                    case "F":
                    case "G":
                    case "A":
                    case "B":
                        localNumPart = currentnotelen;
                        if (num) int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out localNumPart);

                        if (!NoteLengthToFrameLengthMap.ContainsKey(localNumPart))
                        {
                            throw new Exception(string.Format(@"column {0}, {1} is out of range.", pos + 2, localNumPart));
                        }

                        var noteItem = new MyNoteInfo();
                        mynotes.Add(noteItem);
                        noteItem.Note = token;
                        noteItem.Lyric = macro == "R" ? "" : NoteToSampleLyricMap[macro];  // 歌詞無しの場合の歌唱用

                        var note = Regex.Replace(token, MacroNoteLenMatchReg, "");

                        if ((macro != "R") &&(!NoteToKeyDispMap.ContainsKey(note)))
                        {
                            throw new Exception(string.Format(@"column {0}, {1} is unknown key.", pos + 1, note));
                        }

                        noteItem.Key = macro == "R" ? currentkeybase : currentkeybase + NoteToKeyDispMap[note];

                        noteItem.FrameLength = NoteLengthToFrameLengthMap[localNumPart];

                        if (dot) noteItem.FrameLength *= 1.5; // 付点音符・付点休符の補正

                        break;
                }
            }

        }

        private void ParseLyricString(string lyric, ref int noteindex, ref List<MyNoteInfo> mynotes)
        {
            // 歌詞分解実施
            List<string> lyricList = new List<string>();
            int lyricIndex = 0;
            for (var idx = 0; idx < lyric.Length; idx++)
            {
                string lyricChar = lyric[idx].ToString();
                switch (lyricChar)
                {
                    case "ー":
                    case "っ":
                    case "ッ":
                        // 促音、長音記号は割り当てない
                        break;

                    case "ゃ":
                    case "ャ":
                    case "ゅ":
                    case "ュ":
                    case "ょ":
                    case "ョ":
                        // 拗音文字は直前の音符の歌詞に加える
                        if (idx >= 1)
                        {
                            lyricList[lyricIndex - 1] += lyricChar;
                        }
                        else
                        {
                            lyricList[lyricIndex] += lyricChar;
                            lyricIndex++;
                        }
                        break;

                    default:
                        lyricList.Add(lyricChar);
                        lyricIndex++;
                        break;
                }
            }

            foreach (var item in lyricList)
            {
                // 休符をスキップする
                while (mynotes[noteindex].Note == "R")
                {
                    noteindex++;
                    if (noteindex >= mynotes.Count) break;
                }

                // 音符に歌詞を割り当てる
                mynotes[noteindex].Lyric = item;
                noteindex++;
            };

        }
    }
}
