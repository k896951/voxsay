using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text.RegularExpressions;

namespace voxsay
{
    public class MMLParser
    {
        private const string MacroMatchReg0 = @"^[><STONRLCDEFGAB]";
        private const string MacroMatchReg1 = @"^([><]|S\d|[TON]\d{1,}|L\d{1,}\.{0,1}|R\d{0,}\.{0,1}|[CDEFGAB]\d{0,}\.{0,1}[#+\-]{0,1})";
        private const string MacroMatchReg2 = @"^([><]|S\d|[TON]\d{1,}|L\d{1,}\.{0,1}|R\d{0,}\.{0,1}|[CDEFGAB][#+\-]{0,1}\d{0,}\.{0,1})";
        private const string MacroNumMatchReg = @"\d{1,}";
        private const string MacroDotMatchReg = @"\.";
        private const string MacroKeyModifierMatchReg = @"[#+\-]";
        private const string MacroNoteLenMatchReg = @"\d{0,}\.{0,1}";

        private const int MinBPM = 40;
        private const int MaxBPM = 800;

        private int currentTempo;
        private int currentOctave;
        private string currentNotelen;
        private int mmlStyleMode = 1;

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

        public int MmlStyle
        {
            get
            {
                return mmlStyleMode;
            }

            set
            {
                if ((0 < value) && (value < 3)) mmlStyleMode = value;
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

        private bool MmlStyleCheck(int style)
        {
            return ((0 < style) && (style < 3));
        }

        public List<MyMMLInfo> ParseMMLString(string mmlstr)
        {
            List<MyMMLInfo> mmlInfo = new List<MyMMLInfo>();
            string MacroMatchReg = "";
            string localMML = mmlstr.ToUpper();

            switch(MmlStyle)
            {
                case 1:
                    // スタイル1書式
                    MacroMatchReg = MacroMatchReg1;
                    break;
                case 2:
                    // スタイル2書式
                    MacroMatchReg = MacroMatchReg2;
                    break;
            }

            int addpos;
            for (var pos = 0; pos < localMML.Length; pos += addpos)
            {
                // MMLマクロの書式に合致しないならエラー
                if (!Regex.IsMatch(localMML.Substring(pos), MacroMatchReg0))
                {
                    throw new Exception(string.Format(@"mml Part column {0}, '{1}' はサポートしていないマクロです", pos + 1, localMML.Substring(pos, 1)));
                }

                // 音符・休符部分の切り出し
                string token = Regex.Match(localMML.Substring(pos), MacroMatchReg).Value;
                addpos = token.Length;

                if (addpos == 0)
                {
                    throw new Exception(string.Format(@"mml Part column {0}, マクロ '{1}' は書式が正しくありません", pos + 1, localMML.Substring(pos, 1)));
                }

                // MML要素生成
                var mml = new MyMMLInfo();
                mmlInfo.Add(mml);

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
                    case "S":
                        // 音符書式（スタイル）変更
                        if (!num) throw new Exception(string.Format(@"mml Part column {0}, '{1}' にスタイルの指定がありません", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localStyle);

                        if(!MmlStyleCheck(localStyle)) throw new Exception(string.Format(@"mml Part column {0}, スタイルの指定 '{1}' は誤りです", pos + 1, localStyle));

                        MmlStyle = localStyle;
                        break;

                    case "T":
                        // テンポの変更
                        if (!num) throw new Exception(string.Format(@"mml Part column {0}, '{1}' にテンポの指定数値がありません", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localTempo);

                        if ((MinBPM > localTempo) || (MaxBPM < localTempo)) throw new Exception(string.Format(@"mml Part column {0}, テンポに '{1}' が指定されましたが範囲は {2}～{3} です", pos + 1, localTempo, MinBPM, MaxBPM));

                        Bpm = localTempo;
                        mml.Tempo = localTempo;
                        break;

                    case "O":
                        // オクターブの変更（基準キー位置の変更）
                        if (!num) throw new Exception(string.Format(@"mml Part column {0}, '{1}' にオクターブの指定数値がありません", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localOctave);

                        if (!OctaveCheck(localOctave)) throw new Exception(string.Format(@"mml Part column {0}, オクターブに '{1}' が指定されましたが範囲は 0～9 です", pos + 2, localOctave));

                        Octave = localOctave;
                        mml.Octave = localOctave;
                        break;

                    case ">":
                        // １オクターブ上げる（基準キー位置を上位へ変更）
                        if (!OctaveCheck(Octave + 1)) throw new Exception(string.Format(@"mml Part column {0}, オクターブの指定 '{1}' は上限以上になります", pos + 1, token));

                        Octave++;
                        mml.Octave = Octave;
                        break;

                    case "<":
                        // １オクターブ下げる（基準キー位置を下位へ変更）
                        if (!OctaveCheck(Octave - 1)) throw new Exception(string.Format(@"mml Part column {0}, オクターブの指定 '{1}' は下限以下になります", pos + 1, token));

                        Octave--;
                        mml.Octave = Octave;
                        break;

                    case "R":
                        // 休符指定
                        var localDefaultRestLen = DefaultNoteLen;
                        if (num) localDefaultRestLen = Regex.Match(token, MacroNumMatchReg).Value;
                        if (dot) localDefaultRestLen += ".";
                        if (localDefaultRestLen.EndsWith("..")) throw new Exception(string.Format(@"mml Part column {0}, L{1}が指定されているのに{2}へ付点(""."")を指定しました", pos + 2, DefaultNoteLen, macro));

                        if (!NoteLenCheck(localDefaultRestLen)) throw new Exception(string.Format(@"mml Part column {0},  長さに '{1}' が指定されましたが設定可能な値は 1,2,4,8,16,32,64,128 かそれらに"".""を付与したものです", pos + 2, DefaultNoteLen));

                        DefaultNoteLen = localDefaultRestLen;
                        mml.NoteLen = DefaultNoteLen;
                        mml.WithDot = dot;
                        break;

                    case "L":
                        // 音符・休符のデフォルト長変更
                        if (!num) throw new Exception(string.Format(@"mml Part column {0}, '{1}' に長さの指定がありません", pos + 1, token));

                        var localDefaultNoteLen = Regex.Match(token, MacroNumMatchReg).Value;
                        if (dot) localDefaultNoteLen += ".";

                        if (!NoteLenCheck(localDefaultNoteLen)) throw new Exception(string.Format(@"mml Part column {0},  長さに '{1}' が指定されましたが設定可能な値は 1,2,4,8,16,32,64,128 かそれらに"".""を付与したものです", pos + 2, localDefaultNoteLen));

                        DefaultNoteLen = localDefaultNoteLen;
                        mml.WithDot = dot;
                        break;

                    case "N":
                        // キー直接指定
                        if (!num) throw new Exception(string.Format(@"mml Part column {0}, '{1}' にキーの指定がありません", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localKey);

                        if (!KeyCheck(localKey)) throw new Exception(string.Format(@"mml Part column {0},  キーに '{1}' が指定されましたが設定可能な値は 0～127 です", pos + 2, localKey));

                        mml.MacroName = macro;
                        mml.Key = localKey;
                        mml.NoteLen = DefaultNoteLen;
                        mml.SampleLyric = "ラ"; // とりあえずのサンプル歌詞
                        mml.WithDot = false;
                        break;

                    case "C":
                    case "D":
                    case "E":
                    case "F":
                    case "G":
                    case "A":
                    case "B":
                        // 長さ指定を消した物
                        var note = Regex.Replace(token, MacroNoteLenMatchReg, "");
                        if (!NoteCheck(note)) throw new Exception(string.Format(@"mml Part column {0}, {1}にキー修飾指定 '{2}' はできません", pos + 1, macro, note));

                        switch (MmlStyle)
                        {
                            case 1:
                                var localNoteNum = DefaultNoteLen;
                                if (num) localNoteNum = Regex.Match(token, MacroNumMatchReg).Value;
                                if (dot) localNoteNum += ".";
                                if (localNoteNum.EndsWith("..")) throw new Exception(string.Format(@"mml Part column {0}, L{1}が指定されているのに{2}へ付点(""."")を指定しました", pos + 2, DefaultNoteLen, macro));
                                if (!NoteLenCheck(localNoteNum)) throw new Exception(string.Format(@"mml Part column {0}, 長さに '{1}' が指定されましたが設定可能な値は 1,2,4,8,16,32,64,128 かそれらに"".""を付与したものです", pos + 2, localNoteNum));

                                mml.MacroName = note;
                                mml.NoteLen = localNoteNum;
                                mml.SampleLyric = NoteToSampleLyric(macro);
                                mml.WithDot = dot;
                                break;

                            case 2:
                                var localBaseOctave = Octave;
                                var localNoteNum2 = DefaultNoteLen;
                                if (num) int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out localBaseOctave);
                                if (!OctaveCheck(localBaseOctave)) throw new Exception(string.Format(@"mml Part column {0}, オクターブに '{1}' が指定されましたが範囲は 0～9 です", pos + 2, localBaseOctave));
                                if (dot) localNoteNum2 += ".";
                                if (localNoteNum2.EndsWith("..")) throw new Exception(string.Format(@"mml Part column {0}, L{1}が指定されているのに{2}へ付点(""."")を指定しました", pos + 2, DefaultNoteLen, macro));

                                mml.MacroName = note;
                                mml.NoteLen = localNoteNum2;
                                mml.Octave = localBaseOctave;
                                mml.SampleLyric = NoteToSampleLyric(macro);
                                mml.WithDot = dot;
                                break;
                        }

                        break;
                }
            }

            return mmlInfo;
        }

    }
}
