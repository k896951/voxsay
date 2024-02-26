using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MMLParser
{
    /// <summary>
    /// MML解析クラス
    /// </summary>
    internal class MMLParser
    {
        private const string MacroMatchReg0 = @"^[><STONRLCDEFGAB]";
        private const string MacroMatchReg1 = @"^([><]|S\d|[TON]\d{1,}|L\d{1,}\.{0,1}|R\d{0,}\.{0,1}|[CDEFGAB]\d{0,}\.{0,1}[#+\-]{0,1})";
        private const string MacroMatchReg2 = @"^([><]|S\d|[TON]\d{1,}|L\d{1,}\.{0,1}|R\d{0,}\.{0,1}|[CDEFGAB][#+\-]{0,1}\d{0,}\.{0,1})";
        private const string MacroNumMatchReg = @"\d{1,}";
        private const string MacroDotMatchReg = @"\.";
        private const string MacroNoteLenMatchReg = @"\d{0,}\.{0,1}";

        private const int MinBPM = 40;
        private const int MaxBPM = 800;

        private int currentTempo;
        private int currentOctave;
        private string currentNotelen;
        private int mmlStyleMode = 1;

        /// <summary>
        /// ノートとサンプル歌詞のマップ
        /// </summary>
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

        /// <summary>
        /// MMLパーサ
        /// </summary>
        internal MMLParser()
        {
            Bpm = 120;            // T120
            Octave = 4;           // O4
            DefaultNoteLen = "4"; // L4
        }

        /// <summary>
        /// MMLパーサ
        /// </summary>
        /// <param name="defaultTempo">解析開始時テンポ</param>
        /// <param name="defaultOctave">解析開始時オクターブ</param>
        /// <param name="defaultNoteLen">解析開始時音符・休符長さ</param>
        internal MMLParser(int defaultTempo, int defaultOctave, string defaultNoteLen)
        {

            Bpm = 120;            // T120
            Octave = 4;           // O4
            DefaultNoteLen = "4"; // L4

            Bpm = defaultTempo;
            Octave = defaultOctave;
            DefaultNoteLen = defaultNoteLen;
        }

        /// <summary>
        /// テンポ
        /// </summary>
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

        /// <summary>
        /// オクターブ
        /// </summary>
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

        /// <summary>
        /// 音符・休符長さ
        /// </summary>
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

        /// <summary>
        /// マクロスタイル
        /// </summary>
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

        /// <summary>
        /// オクターブの範囲確認
        /// </summary>
        /// <param name="octave">オクターブ。0～9</param>
        /// <returns>範囲にあるならTRUE</returns>
        private bool OctaveCheck(int octave)
        {
            return ((-1 < octave) && (octave < 10));
        }

        /// <summary>
        /// 長さ指定の範囲判定
        /// </summary>
        /// <param name="notelen">長さ。1,2,4,8,16,32,64,128 の8種類。付点が必要なら"."を最後に付与</param>
        /// <returns>範囲にあるならTRUE</returns>
        private bool NoteLenCheck(string notelen)
        {
            string[] notelengthArray = { "1", "1.", "2", "2.", "4", "4.", "8", "8.", "16", "16.", "32", "32.", "64", "64.", "128", "128." };

            return notelengthArray.Contains(notelen);
        }

        /// <summary>
        /// 音符指定の判定
        /// </summary>
        /// <param name="note">音符。C,D,E,F,G,A,B の7種類。半音上げ下げが必要なら "#","-" を最後に付与</param>
        /// <returns>範囲にあるならTRUE</returns>
        private bool NoteCheck(string note)
        {
            return NoteToDefaultLyricMap.ContainsKey(note);
        }

        /// <summary>
        /// 音符に紐づけるサンプル歌詞を得る
        /// </summary>
        /// <param name="note">音符。C,D,E,F,G,A,B の7種類。半音上げ下げが必要なら "#","-" を最後に付与</param>
        /// <returns>対応するサンプル歌詞。対応しない場合は長さゼロの文字列</returns>
        private string NoteToSampleLyric(string note)
        {
            if (NoteCheck(note)) return NoteToDefaultLyricMap[note];

            return "";
        }

        /// <summary>
        /// キー範囲の判定
        /// </summary>
        /// <param name="key">キー。</param>
        /// <returns></returns>
        private bool KeyCheck(int key)
        {
            return ((-1 < key) && (key < 128));
        }

        /// <summary>
        /// キーから対応する音符の文字列を得る
        /// </summary>
        /// <param name="key">キー。0～127</param>
        /// <returns>キーから算出した音符の文字列</returns>
        private string KeyToNote(int key)
        {
            Dictionary<int, string> map = new Dictionary<int, string>()
            {
                { 0,"C"}, { 1,"C#"},{ 2,"D"}, { 3,"D#"},{ 4,"E"}, { 5,"F"}, { 6,"F#"},{ 7,"G"}, { 8,"G#"},{ 9,"A"}, {10,"A#"},{11,"B"}
            };

            return map[key % 12];
        }

        /// <summary>
        /// マクロスタイルの確認
        /// </summary>
        /// <param name="style">スタイル</param>
        /// <returns>指定が正しければTRUE</returns>
        private bool MmlStyleCheck(int style)
        {
            return ((0 < style) && (style < 3));
        }

        /// <summary>
        /// MML文字列を解析する
        /// </summary>
        /// <param name="mmlstr">MML文字列</param>
        /// <returns>解析結果のリスト</returns>
        /// <exception cref="Exception">書式不正を検出した時</exception>
        internal List<MMLInfo> ParseMMLString(string mmlstr)
        {
            List<MMLInfo> mmlInfo = new List<MMLInfo>();
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
                var mml = new MMLInfo();
                mmlInfo.Add(mml);

                // マクロ名（先頭１文字） 
                string macro = token.Substring(0, 1);

                // 数値の指定があるか否か
                var num = Regex.IsMatch(token, MacroNumMatchReg);

                // 付点(".")の指定があるか否か
                var dot = Regex.IsMatch(token, MacroDotMatchReg);

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
