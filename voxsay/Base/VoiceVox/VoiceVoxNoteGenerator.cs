using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace voxsay
{
    public class VoiceVoxNoteGenerator
    {
        private double FrameDurationParTick = 0.01;
        private double QuarterNoteFrameLength = 50.0;
        private int Tempo = 120;
        private string currentOctave;
        private int currentNoteLen;
        private int currentNoteBase;
        private int currentNoteListIndex;

        private Dictionary<int, double> NoteFrameLengthMap = new Dictionary<int, double>()
        {
            {  1, 200.0 },
            {  2, 100.0 },
            {  4,  50.0 },
            {  8,  25.0 },
            { 16,  12.5 },
            { 32,   6.25}
        };

        private Dictionary<string, int> OctaveMap = new Dictionary<string, int>()
        {
            { "O0",  12},
            { "O1",  24},
            { "O2",  36},
            { "O3",  48},
            { "O4",  60},
            { "O5",  72},
            { "O6",  84},
            { "O7",  96},
            { "O8", 108},
            { "O9", 120},
        };

        private Dictionary<string, string> NoteSampleLyricMap = new Dictionary<string, string>()
        {
            { "C", "ど"  },
            { "D", "れ"  },
            { "E", "み"  },
            { "F", "ふぁ"},
            { "G", "そ"  },
            { "A", "ら"  },
            { "B", "し"  }
        };

        private Dictionary<string, int> NoteNumDispMap = new Dictionary<string, int>()
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

        private int Bpm
        {
            get
            {
                return Tempo;
            }

            set
            {
                Tempo = value;

                double magnification = Convert.ToDouble(60.0 / Tempo) / FrameDurationParTick;

                NoteFrameLengthMap[1] = magnification * 4;  // 　　全音符フレーム数
                NoteFrameLengthMap[2] = magnification * 2;  // 　２分音符フレーム数
                NoteFrameLengthMap[4] = magnification;      // 　４分音符フレーム数
                NoteFrameLengthMap[8] = magnification / 2;  // 　８分音符フレーム数
                NoteFrameLengthMap[16] = magnification / 4; // １６分音符フレーム数
                NoteFrameLengthMap[32] = magnification / 8; // ３２分音符フレーム数
            }
        }

        public VoiceVoxNoteGenerator()
        {
            Bpm = 120;
            currentOctave = "O4";
            currentNoteLen = 4;
            currentNoteBase = OctaveMap[currentOctave];
            currentNoteListIndex = 0;
        }

        public List<VoiceVoxMyNoteInfo> ParseSingString(string singtext)
        {
            List<VoiceVoxMyNoteInfo> ans = new List<VoiceVoxMyNoteInfo>();

            ans.Add(new VoiceVoxMyNoteInfo());
            ans[0].Lyric = "";
            ans[0].Note = "R";
            ans[0].Key = OctaveMap[currentOctave];
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

        private void ParseMMLString(string mml, ref List<VoiceVoxMyNoteInfo> mynotes)
        {
            string lenMatchReg = @"\d+";
            Boolean parseFail = false;

            var tokens = Regex.Matches(mml.ToUpper(), @"[TOLRCDEFGAB]\d*[#+-]*");

            for (var idx = 0; idx <  tokens.Count; idx++ )
            {
                string token = tokens[idx].Value.Trim();
                string macro = token.Substring(0, 1);
                switch (macro)
                {
                    case "T":
                        if (int.TryParse(token.Substring(1), out var tempo))
                        {
                            Bpm = tempo;
                        }
                        else
                        {
                            parseFail = true;
                        }
                        break;

                    case "O":
                        if (OctaveMap.ContainsKey(token))
                        {
                            currentOctave = token;
                            currentNoteBase = OctaveMap[currentOctave];
                        }
                        else
                        {
                            parseFail = true;
                        }
                        break;

                    case "L":
                        if (int.TryParse(token.Substring(1), out var noteLen))
                        {
                            if (NoteFrameLengthMap.ContainsKey(noteLen))
                            {
                                currentNoteLen = noteLen;
                            }
                            else
                            {
                                parseFail = true;
                            }
                        }
                        else
                        {
                            parseFail = true;
                        }
                        break;

                    case "R":
                    case "C":
                    case "D":
                    case "E":
                    case "F":
                    case "G":
                    case "A":
                    case "B":
                        var localNoteLen = currentNoteLen;
                        var localToken = token;

                        if (Regex.IsMatch(localToken, lenMatchReg))
                        {
                            var tokenLenPartStr = Regex.Match(localToken, lenMatchReg).Value;
                            if (int.TryParse(tokenLenPartStr, out var tokenLenPart))
                            {
                                if (NoteFrameLengthMap.ContainsKey(tokenLenPart))
                                {
                                    localNoteLen = tokenLenPart;
                                }
                                else
                                {
                                    parseFail = true;
                                }
                            }
                            else
                            {
                                parseFail = true;
                            }

                            localToken = Regex.Replace(localToken, lenMatchReg, "");
                        }

                        var noteItem = new VoiceVoxMyNoteInfo();
                        mynotes.Add(noteItem);

                        noteItem.Lyric = macro == "R" ? "" : NoteSampleLyricMap[macro];
                        noteItem.Note = localToken;
                        noteItem.Key = macro == "R" ? currentNoteBase : currentNoteBase + NoteNumDispMap[localToken];
                        noteItem.FrameLength = NoteFrameLengthMap[localNoteLen];

                        break;

                    default:
                        parseFail = false;
                        break;
                }

                if(parseFail)
                {
                    throw new Exception(string.Format(@"MML[{0}], {1} is invalid.", idx+1, token));
                }
            };

        }

        private void ParseLyricString(string lyric, ref List<VoiceVoxMyNoteInfo> mynotes)
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
