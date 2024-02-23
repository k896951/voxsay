using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace voxsay
{
    public class MMLParser
    {
        private const string MacroMatchReg = @"^[TOLRCDEFGABN<>]\d{0,}\.{0,1}[#+\-]{0,1}";
        private const string MacroNumMatchReg = @"\d{1,}";
        private const string MacroDotMatchReg = @"\.";
        private const string MacroKeyModifierMatchReg = @"[#+\-]";
        private const string MacroNoteLenMatchReg = @"\d{0,}\.{0,1}";

        private const int MinBPM = 40;
        private const int MaxBPM = 800;

        private int currentTempo;
        private int currentOctave;
        private string currentNotelen;

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

        public MMLParser()
        {
            Bpm = 120;            // T120
            Octave = 4;           // O4
            DefaultNoteLen = "4"; // L4
        }

        public MMLParser(int defaultTempo, int defaultOctave, string defaultNoteLen)
        {

            Bpm = 120;            // T120
            Octave = 4;           // O4
            DefaultNoteLen = "4"; // L4

            Bpm = defaultTempo;
            Octave = defaultOctave;
            DefaultNoteLen = defaultNoteLen;
        }

        public int Bpm
        {
            get
            {
                return currentTempo;
            }

            set
            {
                if ((value >= MinBPM) && (value <= MaxBPM)) currentTempo = value;
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

        public string DefaultNoteLen
        {
            get
            {
                return currentNotelen;
            }

            set
            {
                if (NoteLenCheck(value)) currentNotelen = value;
            }
        }

        private bool OctaveCheck(int octave)
        {
            return ((-1 < octave) && (octave < 10));
        }

        private bool NoteLenCheck(string notelen)
        {
            string[] notelengthArray = { "1", "1.", "2", "2.", "4", "4.", "8", "8.", "16", "16.", "32", "32.", "64", "64.", "128", "128." };

            return notelengthArray.Contains(notelen);
        }

        private bool NoteCheck(string note)
        {
            return NoteToDefaultLyricMap.ContainsKey(note);
        }

        private string NoteToSampleLyric(string note)
        {
            if (NoteCheck(note)) return NoteToDefaultLyricMap[note];

            return "";
        }

        private bool KeyCheck(int key)
        {
            return ((-1 < key) && (key < 128));
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
                    throw new Exception(string.Format(@"mml Part column {0}, '{1}' はサポートしていないマクロです", pos + 1, localMML.Substring(pos, 1)));
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

                // 付点(".")の指定があるか否か
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
                        if (dot || keyModify || !num) throw new Exception(string.Format(@"mml Part column {0}, テンポの指定 '{1}' は誤りです", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localTempo);

                        if ((MinBPM > localTempo) || (MaxBPM < localTempo)) throw new Exception(string.Format(@"mml Part column {0}, テンポに '{1}' が指定されましたが範囲は {2}～{3} です", pos + 1, localTempo, MinBPM, MaxBPM));

                        Bpm = localTempo;
                        mml.Tempo = localTempo;
                        break;

                    case "O":
                        // オクターブの変更（基準キー位置の変更）
                        if (dot || keyModify || !num) throw new Exception(string.Format(@"mml Part column {0}, オクターブの指定 '{1}' は誤りです", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localOctave);

                        if (!OctaveCheck(localOctave)) throw new Exception(string.Format(@"mml Part column {0}, オクターブに '{1}' が指定されましたが範囲は 0～9 です", pos + 2, localOctave));

                        Octave = localOctave;
                        mml.Octave = localOctave;
                        break;

                    case ">":
                        // １オクターブ上げる（基準キー位置を上位へ変更）
                        if (dot || keyModify || num) throw new Exception(string.Format(@"mml Part column {0}, オクターブの指定 '{1}' は誤りです", pos + 1, token));
                        if (!OctaveCheck(Octave+1)) throw new Exception(string.Format(@"mml Part column {0}, オクターブの指定 '{1}' は上限以上になります", pos + 1, token));

                        Octave++;
                        mml.Octave = Octave;
                        break;

                    case "<":
                        // １オクターブ下げる（基準キー位置を下位へ変更）
                        if (dot || keyModify || num) throw new Exception(string.Format(@"mml Part column {0}, オクターブの指定 '{1}' は誤りです", pos + 1, token));
                        if (!OctaveCheck(Octave - 1)) throw new Exception(string.Format(@"mml Part column {0}, オクターブの指定 '{1}' は下限以下になります", pos + 1, token));

                        Octave--;
                        mml.Octave = Octave;
                        break;

                    case "L":
                        // 音符・休符のデフォルト長変更
                        if (keyModify || !num) throw new Exception(string.Format(@"mml Part column {0}, 長さの指定 '{1}' は誤りです", pos + 1, token));

                        var localDefaultNoteLen = Regex.Match(token, MacroNumMatchReg).Value;
                        if (dot) localDefaultNoteLen += ".";

                        if (!NoteLenCheck(localDefaultNoteLen)) throw new Exception(string.Format(@"mml Part column {0},  長さに '{1}' が指定されましたが設定可能な値は 1,2,4,8,16,32,64,128 かそれらに"".""を付与したものです", pos + 2, localDefaultNoteLen));

                        DefaultNoteLen = localDefaultNoteLen;
                        mml.WithDot = dot;
                        break;

                    case "N":
                        // キー直接指定
                        if (dot || keyModify || !num) throw new Exception(string.Format(@"mml Part column {0}, キーの指定 '{1}' は誤りです", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localKey);

                        if (!KeyCheck(localKey)) throw new Exception(string.Format(@"mml Part column {0},  キーに '{1}' が指定されましたが設定可能な値は 0～127 です", pos + 2, localKey));

                        mml.MacroName = macro;
                        mml.Key = localKey;
                        mml.NoteLen = DefaultNoteLen;
                        mml.SampleLyric = "ラ"; // とりあえずのサンプル歌詞
                        mml.WithDot = false;

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
                        if ( ((macro == "R") && keyModify) || ((macro != "R") && !NoteCheck(note)) ) throw new Exception(string.Format(@"mml Part column {0}, {1}にキー修飾指定 '{2}' はできません", pos + 1, macro, note));

                        var localNoteLen = DefaultNoteLen;
                        if (num) localNoteLen = Regex.Match(token, MacroNumMatchReg).Value;
                        if (dot) localNoteLen += ".";
                        if (localNoteLen.EndsWith("..")) throw new Exception(string.Format(@"mml Part column {0}, L{1}が指定されているのに{2}へ付点(""."")を指定しました", pos + 2, DefaultNoteLen, macro));
                        if (!NoteLenCheck(localNoteLen)) throw new Exception(string.Format(@"mml Part column {0}, 長さに '{1}' が指定されましたが設定可能な値は 1,2,4,8,16,32,64,128 かそれらに"".""を付与したものです", pos + 2, localNoteLen));

                        mml.MacroName = note;
                        mml.NoteLen = localNoteLen;
                        mml.SampleLyric = macro == "R" ? "" : NoteToSampleLyric(macro);
                        mml.WithDot = dot;
                        break;
                }
            }

            return mmlInfo;
        }

    }
}
