using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace voxsay
{
    public class MMLParser
    {
        private const string MacroMatchReg = @"^[TOLRCDEFGAB]\d{0,}\.{0,1}[#+\-]{0,1}";
        private const string MacroNumMatchReg = @"\d{1,}";
        private const string MacroDotMatchReg = @"\.";
        private const string MacroKeyModifierMatchReg = @"[#+\-]";
        private const string MacroNoteLenMatchReg = @"\d{0,}\.{0,1}";

        private int currentTempo;
        private int currentOctave;
        private int currentNotelen;

        private Dictionary<string, string> NoteToDefaultLyricMap = new Dictionary<string, string>()
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
            this.currentTempo = defaultTempo;
            this.currentOctave = defaultOctave;
            this.currentNotelen = defaultNoteLen;
        }

        public int Bpm
        {
            get
            {
                return currentTempo;
            }

            set
            {
                currentTempo = value;
            }
        }

        public int Octave
        {
            get
            {
                return currentOctave;
            }

            set
            {
                if (OctaveCheck(value)) currentOctave = value;
            }
        }

        public int DefaultNoteLen
        {
            get
            {
                return currentNotelen;
            }

            set
            {
                if (NoteLengthCheck(value)) currentNotelen = value;
            }
        }

        private bool OctaveCheck(int octave)
        {
            return ((-1 < octave) && (octave < 10));
        }

        private bool NoteLengthCheck(int notelen)
        {
            int[] notelengthArray = { 1, 2, 4, 8, 16, 32, 64, 128 };

            return notelengthArray.Contains(notelen);
        }

        private bool NoteCheck(string note)
        {
            return NoteToDefaultLyricMap.ContainsKey(note);
        }

        private string NoteToDefaultLyric(string note)
        {
            if (NoteCheck(note)) return NoteToDefaultLyricMap[note];

            return "";
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

                        if (!OctaveCheck(localOctave)) throw new Exception(string.Format(@"mml Part column {0}, {1} is out of range.", pos + 2, localOctave));

                        Octave = localOctave;
                        mml.Octave = localOctave;
                        break;

                    case "L":
                        // 音符・休符のデフォルト長変更
                        if (keyModify || !num) throw new Exception(string.Format(@"mml Part column {0}, {1} is syntax error.", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localDefaultNoteLen);

                        if (!NoteLengthCheck(localDefaultNoteLen)) throw new Exception(string.Format(@"mml Part column {0}, {1} is out of range.", pos + 2, localDefaultNoteLen));

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
                        if ((macro != "R") && (!NoteCheck(note))) throw new Exception(string.Format(@"mml Part column {0}, {1} is unknown note.", pos + 1, note));

                        var localNoteLen = DefaultNoteLen;
                        if (num) int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out localNoteLen);
                        if (!NoteLengthCheck(localNoteLen)) throw new Exception(string.Format(@"mml Part column {0}, {1} is out of range.", pos + 2, localNoteLen));

                        mml.MacroName = note;
                        mml.NoteLen = localNoteLen;
                        mml.SampleLyric = macro == "R" ? "" : NoteToDefaultLyric(macro);
                        mml.WithDot = dot;
                        break;
                }
            }

            return mmlInfo;
        }

    }
}
