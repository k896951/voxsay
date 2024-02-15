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
        private const double QuarterNoteFrameLength = 50.0;

        private int tempo = 120;
        private int octave =4;

        private int currentNoteLen;
        private int currentKeyBase;
        private int currentNoteListIndex;

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

        public int Bpm
        {
            get
            {
                return tempo;
            }

            set
            {
                tempo = value;

                double magnification = Convert.ToDouble(60.0 / tempo) / FrameDurationParTick;

                NoteLengthToFrameLengthMap[1] = magnification * 4;  // 　　全音符フレーム数
                NoteLengthToFrameLengthMap[2] = magnification * 2;  // 　２分音符フレーム数
                NoteLengthToFrameLengthMap[4] = magnification;      // 　４分音符フレーム数
                NoteLengthToFrameLengthMap[8] = magnification / 2;  // 　８分音符フレーム数
                NoteLengthToFrameLengthMap[16] = magnification / 4; // １６分音符フレーム数
                NoteLengthToFrameLengthMap[32] = magnification / 8; // ３２分音符フレーム数
            }
        }

        public int Octave
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
            Bpm = 120;
            currentNoteLen = 4;
            currentKeyBase = OctaveToKeyMap[octave];
            currentNoteListIndex = 0;
        }

        public List<MyNoteInfo> ParseSingString(string singtext)
        {
            List<MyNoteInfo> ans = new List<MyNoteInfo>();

            ans.Add(new MyNoteInfo());
            ans[0].Lyric = "";
            ans[0].Note = "R";
            ans[0].Key = OctaveToKeyMap[octave];
            ans[0].FrameLength = 2;

            currentNoteListIndex = 0;

            foreach (var measure in Regex.Split(singtext, @"[,]"))
            {
                try
                {
                    var parts = Regex.Split(measure.Trim(), @"[:]+");
                    switch (parts.Length)
                    {
                        case 1:
                            ParseMMLString(parts[0].Trim(), ref ans);
                            break;

                        case 2:
                            ParseMMLString(parts[1].Trim(), ref ans);
                            ParseLyricString(parts[0].Trim(), ref ans); // ParseMMLString()のあとに呼ぶ
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

        private void ParseMMLString(string mml, ref List<MyNoteInfo> mynotes)
        {
            string lenMatchReg0 = @"^[TOLRCDEFGAB]\d{0,}\.{0,1}[#+-]{0,1}";
            string lenMatchReg1 = @"\d+";
            string lenMatchReg2 = @"\.";
            string lenMatchReg3 = @"\d*\.*";
            string localMML = mml.ToUpper();
            int localNumPart = currentNoteLen;

            int addpos;
            for (var pos = 0; pos < localMML.Length; pos += addpos)
            {
                if (!Regex.IsMatch(localMML.Substring(pos), lenMatchReg0 ))
                {
                    throw new Exception(string.Format(@"column {0}, '{1}' is unknown.", pos + 1, localMML.Substring(pos, 1)));
                }

                string token = Regex.Match(localMML.Substring(pos), lenMatchReg0).Value;
                addpos = token.Length;

                // 数値指定があれば抜き出す
                if (Regex.IsMatch(token, lenMatchReg1))
                {
                    int.TryParse(Regex.Match(token, lenMatchReg1).Value, out localNumPart);
                }

                // "."の指定があるか否か
                var dot = Regex.IsMatch(token, lenMatchReg2);

                // 各マクロ処理
                string macro = token.Substring(0, 1);
                switch (macro)
                {
                    case "T":
                        Bpm = localNumPart;
                        break;

                    case "O":
                        if (!OctaveToKeyMap.ContainsKey(localNumPart))
                        {
                            throw new Exception(string.Format(@"column {0}, {1} is out of range.", pos + 2, localNumPart));
                        }
                        Octave = localNumPart;
                        currentKeyBase = OctaveToKeyMap[localNumPart];
                        break;

                    case "L":
                        if (!NoteLengthToFrameLengthMap.ContainsKey(localNumPart))
                        {
                            throw new Exception(string.Format(@"column {0}, {1} is out of range.", pos + 2, localNumPart));
                        }
                        currentNoteLen = localNumPart;
                        break;

                    case "R":
                    case "C":
                    case "D":
                    case "E":
                    case "F":
                    case "G":
                    case "A":
                    case "B":
                        if (!NoteLengthToFrameLengthMap.ContainsKey(localNumPart))
                        {
                            throw new Exception(string.Format(@"column {0}, {1} is out of range.", pos + 2, localNumPart));
                        }

                        var noteItem = new MyNoteInfo();
                        mynotes.Add(noteItem);
                        noteItem.Note = token;
                        noteItem.Lyric = macro == "R" ? "" : NoteToSampleLyricMap[macro];  // 歌詞無しの場合の歌唱用

                        var note = Regex.Replace(token, lenMatchReg3, "");

                        if ((macro != "R") &&(!NoteToKeyDispMap.ContainsKey(note)))
                        {
                            throw new Exception(string.Format(@"column {0}, {1} is unknown key.", pos + 1, note));
                        }

                        noteItem.Key = macro == "R" ? currentKeyBase : currentKeyBase + NoteToKeyDispMap[note];
                        noteItem.FrameLength = NoteLengthToFrameLengthMap[localNumPart];

                        if (dot) noteItem.FrameLength *= 1.5; // 付点音符・付点休符の補正

                        break;
                }
            }

        }

        private void ParseLyricString(string lyric, ref List<MyNoteInfo> mynotes)
        {
            int myNoteInfoIndex = currentNoteListIndex;

            while (mynotes[myNoteInfoIndex].Note == "R")
            {
                myNoteInfoIndex++;
                if (myNoteInfoIndex > mynotes.Count)
                {
                    return ;
                }
            }

            for (var idx = 0; idx < lyric.Length; idx++)
            {

                while (mynotes[myNoteInfoIndex].Note == "R")
                {
                    myNoteInfoIndex++;
                    if (myNoteInfoIndex > mynotes.Count) break;
                }

                string lyricChar = lyric[idx].ToString();
                switch (lyricChar)
                {
                    case "ー":
                    case "っ":
                    case "ッ":
                        myNoteInfoIndex--;
                        break;

                    case "ゃ":
                    case "ャ":
                    case "ゅ":
                    case "ュ":
                    case "ょ":
                    case "ョ":
                        if (idx >= 1)
                        {
                            mynotes[myNoteInfoIndex - 1].Lyric += lyricChar;
                            myNoteInfoIndex--;
                        }
                        else
                        {
                            mynotes[myNoteInfoIndex].Lyric = lyricChar;
                        }
                        break;

                    default:
                        mynotes[myNoteInfoIndex].Lyric = lyricChar;
                        break;

                }

                myNoteInfoIndex++;
                if (myNoteInfoIndex >= mynotes.Count) break;

            };

        }
    }
}
