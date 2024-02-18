using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace voxsay
{
    public class MMLParser
    {
        private const double FrameDurationParTick = 0.01;
        private const double QuarterNoteFrameLength = 50.0;  // BPM=120

        private const string MacroMatchReg = @"^[TOLRCDEFGAB]\d{0,}\.{0,1}[#+\-]{0,1}";
        private const string MacroNumMatchReg = @"\d{1,}";
        private const string MacroDotMatchReg = @"\.";
        private const string MacroKeyModifierMatchReg = @"[#+\-]";
        private const string MacroNoteLenMatchReg = @"\d{0,}\.{0,1}";

        private int tempo;
        private int octave;
        private int notelen;

        private Dictionary<int, bool> OctaveCheckMap = new Dictionary<int, bool>()
        {
            { 0,  true},
            { 1,  true},
            { 2,  true},
            { 3,  true},
            { 4,  true},
            { 5,  true},
            { 6,  true},
            { 7,  true},
            { 8,  true},
            { 9,  true},
        };

        private Dictionary<int, Boolean> NoteLengthCheckMap = new Dictionary<int, Boolean>()
        {
            {  1,  true },
            {  2,  true },
            {  4,  true },
            {  8,  true },
            { 16,  true },
            { 32,  true }
        };

        private Dictionary<string, string> NoteCheckMap = new Dictionary<string, string>()
        {
            { "C",  "ど"},
            { "C#", "ど"},
            { "C+", "ど"},
            { "D-", "れ"},
            { "D",  "れ"},
            { "D#", "れ"},
            { "D+", "れ"},
            { "E-", "み"},
            { "E",  "み"},
            { "F",  "ふぁ"},
            { "F#", "ふぁ"},
            { "F+", "ふぁ"},
            { "G-", "そ"},
            { "G",  "そ"},
            { "G#", "そ"},
            { "G+", "そ"},
            { "A-", "ら"},
            { "A",  "ら"},
            { "A#", "ら"},
            { "A+", "ら"},
            { "B-", "し"},
            { "B",  "し"}
        };

        public MMLParser(int defaultTempo, int defaultOctave, int defaultNoteLen)
        {
            this.tempo = defaultTempo;
            this.octave = defaultOctave;
            this.notelen = defaultNoteLen;
        }

        public int Bpm
        {
            get
            {
                return tempo;
            }

            set
            {
                tempo = value;
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
                if (OctaveCheckMap.ContainsKey(value)) octave = value;
            }
        }

        public int DefaultNoteLen
        {
            get
            {
                return notelen;
            }

            set
            {
                if (NoteLengthCheckMap.ContainsKey(value))
                {
                    notelen = value;
                }
            }
        }

        public List<MyMMLInfo> ParseMMLString(string mmlstr)
        {
            List<MyMMLInfo> mmlInfo = new List<MyMMLInfo>();

            string localMML = mmlstr.ToUpper();
            MyMMLInfo mml;

            int addpos;
            for (var pos = 0; pos < localMML.Length; pos += addpos)
            {
                // MMLマクロの書式に合致しないならエラー
                if (!Regex.IsMatch(localMML.Substring(pos), MacroMatchReg))
                {
                    throw new Exception(string.Format(@"mml Part column {0}, '{1}' is unknown.", pos + 1, localMML.Substring(pos, 1)));
                }

                // MML要素生成
                mml = new MyMMLInfo();
                mmlInfo.Add(mml);

                // 音符・休符部分の切り出し
                string token = Regex.Match(localMML.Substring(pos), MacroMatchReg).Value;
                addpos = token.Length;

                // マクロ名（先頭１文字） 
                string macro = token.Substring(0, 1);

                // 数値の指定があるか否か
                var num = Regex.IsMatch(token, MacroNumMatchReg);

                // "."の指定があるか否か
                var dot = Regex.IsMatch(token, MacroDotMatchReg);

                // キー修飾(#,+,-)があるか否か
                var keyModify = Regex.IsMatch(token, MacroKeyModifierMatchReg);

                mml.MacroName = macro;
                mml.NoteLen = DefaultNoteLen;
                mml.Tempo = Bpm;
                mml.Octave = Octave;

                // 各マクロ処理
                switch (macro)
                {
                    case "T":
                        // テンポの変更
                        if (dot || keyModify || !num) throw new Exception(string.Format(@"mml Part column {0}, {1} is syntax error.", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localTempo);

                        Bpm = localTempo;
                        mml.Tempo = localTempo;
                        break;

                    case "O":
                        // オクターブの変更（基準キー位置の変更）
                        if (dot || keyModify || !num) throw new Exception(string.Format(@"mml Part column {0}, {1} is syntax error.", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localOctave);

                        if (!OctaveCheckMap.ContainsKey(localOctave)) throw new Exception(string.Format(@"mml Part column {0}, {1} is out of range.", pos + 2, localOctave));

                        Octave = localOctave;
                        mml.Octave = localOctave;
                        break;

                    case "L":
                        // 音符・休符のデフォルト長変更
                        if (keyModify || !num) throw new Exception(string.Format(@"mml Part column {0}, {1} is syntax error.", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localDefaultNoteLen);

                        if (!NoteLengthCheckMap.ContainsKey(localDefaultNoteLen)) throw new Exception(string.Format(@"mml Part column {0}, {1} is out of range.", pos + 2, localDefaultNoteLen));

                        DefaultNoteLen = localDefaultNoteLen;
                        break;

                    case "R":
                    case "C":
                    case "D":
                    case "E":
                    case "F":
                    case "G":
                    case "A":
                    case "B":
                        var note = Regex.Replace(token, MacroNoteLenMatchReg, ""); // 長さ指定を消した物
                        if ((macro == "R") && keyModify) throw new Exception(string.Format(@"mml Part column {0}, {1} is syntax error.", pos + 1, note));
                        if ((macro != "R") && (!NoteCheckMap.ContainsKey(note))) throw new Exception(string.Format(@"mml Part column {0}, {1} is unknown note.", pos + 1, note));

                        var localNoteLen = DefaultNoteLen;
                        if (num) int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out localNoteLen);
                        if (!NoteLengthCheckMap.ContainsKey(localNoteLen)) throw new Exception(string.Format(@"mml Part column {0}, {1} is out of range.", pos + 2, localNoteLen));

                        mml.MacroName = note;
                        mml.NoteLen = localNoteLen;
                        mml.SampleLyric = macro == "R" ? "" : NoteCheckMap[macro];
                        mml.WithDot = dot;
                        break;
                }
            }

            return mmlInfo;
        }

    }
}
