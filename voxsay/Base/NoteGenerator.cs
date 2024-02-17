﻿using NAudio.CoreAudioApi;
using NAudio.Gui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private const string MacroMatchReg = @"^[TOLRCDEFGAB]\d{0,}\.{0,1}[#+\-]{0,1}";
        private const string MacroNumMatchReg = @"\d{1,}";
        private const string MacroDotMatchReg = @"\.";
        private const string MacroKeyModifierMatchReg = @"[#+\-]";
        private const string MacroNoteLenMatchReg = @"\d{0,}\.{0,1}";

        private int tempo = 120;
        private int octave = 4;
        private int notelen = 4;

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

        public int DefaultNoteLen
        {
            get
            {
                return notelen;
            }

            set
            {
                if (NoteLengthToFrameLengthMap.ContainsKey(value))
                {
                    notelen = value;
                }
            }
        }

        public NoteGenerator()
        {
                                 // default
            Bpm = 120;           // T120
            Octave = 4;          // O4
            DefaultNoteLen = 4;  // L4
        }

        public List<MyNoteInfo> ParseSingString(string singtext)
        {
            List<MyNoteInfo> mynoteinfo = new List<MyNoteInfo>();
            List<MyNoteInfo> notelist;
            List<MyMMLInfo> mmllist;
            List<List<string>> lyriclist;

            foreach (var measure in Regex.Split(singtext, @"[,]"))
            {
                try
                {
                    var parts = Regex.Split(measure.Trim(), @"[:]+");
                    switch (parts.Length)
                    {
                        case 1: // MMLだけある場合
                            mmllist = ParseMMLString(parts[0].Trim());
                            notelist = GenMyNoteInfoFromMyMMLInfo(ref mmllist);
                            break;

                        case 2:  // 歌詞とMMLがある場合
                            lyriclist = ParseLyricString(parts[0].Trim());
                            mmllist = ParseMMLString(parts[1].Trim());
                            notelist = GenMyNoteInfoFromMyMMLInfo(ref mmllist);
                            AssignLyricToMyNoteInfo(ref lyriclist, ref notelist);
                            break;

                        default:
                            throw new Exception(string.Format(@"Incorrect definition."));
                    }

                    // 最初に休符を入れておく
                    if (mynoteinfo.Count == 0)
                    {
                        mynoteinfo.Add(new MyNoteInfo());
                        mynoteinfo[0].Lyric = "";
                        mynoteinfo[0].Note = "R";
                        mynoteinfo[0].Key = OctaveToKeyMap[octave];
                        mynoteinfo[0].FrameLength = NoteLengthToFrameLengthMap[DefaultNoteLen];
                    }

                    // 解析結果追加

                    mynoteinfo.AddRange(notelist);

                }
                catch (Exception e)
                {
                    throw new Exception(string.Format(@"measure:[{0}], {1}", measure.Trim(), e.Message), e);
                }
            }

            return mynoteinfo;
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

        private List<MyMMLInfo> ParseMMLString(string mmlstr)
        {
            List<MyMMLInfo> mmlInfo = new List<MyMMLInfo>();

            string localMML = mmlstr.ToUpper();
            MyMMLInfo mml;

            int addpos;
            for (var pos = 0; pos < localMML.Length; pos += addpos)
            {
                // MMLマクロの書式に合致しないならエラー
                if (!Regex.IsMatch(localMML.Substring(pos), MacroMatchReg ))
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
                        if(dot || keyModify || !num) throw new Exception(string.Format(@"mml Part column {0}, {1} is syntax error.", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localTempo);

                        Bpm = localTempo;
                        mml.Tempo = localTempo;
                        break;

                    case "O":
                        // オクターブの変更（基準キー位置の変更）
                        if (dot || keyModify || !num) throw new Exception(string.Format(@"mml Part column {0}, {1} is syntax error.", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localOctave);

                        if (!OctaveToKeyMap.ContainsKey(localOctave)) throw new Exception(string.Format(@"mml Part column {0}, {1} is out of range.", pos + 2, localOctave));

                        Octave = localOctave;
                        mml.Tempo = localOctave;
                        break;

                    case "L":
                        // 音符・休符のデフォルト長変更
                        if (keyModify || !num) throw new Exception(string.Format(@"mml Part column {0}, {1} is syntax error.", pos + 1, token));

                        int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out var localDefaultNoteLen);

                        if (!NoteLengthToFrameLengthMap.ContainsKey(localDefaultNoteLen)) throw new Exception(string.Format(@"mml Part column {0}, {1} is out of range.", pos + 2, localDefaultNoteLen));

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
                        if ((macro != "R") && (!NoteToKeyDispMap.ContainsKey(note))) throw new Exception(string.Format(@"mml Part column {0}, {1} is unknown note.", pos + 1, note));

                        var localNoteLen = DefaultNoteLen;
                        if (num) int.TryParse(Regex.Match(token, MacroNumMatchReg).Value, out localNoteLen);
                        if (!NoteLengthToFrameLengthMap.ContainsKey(localNoteLen)) throw new Exception(string.Format(@"mml Part column {0}, {1} is out of range.", pos + 2, localNoteLen));

                        mml.MacroName = note;
                        mml.NoteLen = localNoteLen;
                        mml.SampleLyric = macro == "R" ? "" : NoteToSampleLyricMap[macro];
                        mml.WithDot = dot;
                        break;
                }
            }

            return mmlInfo;
        }

        
        
        private List<List<string>> AggregationLyrics(List<MyLyricInfo> list)
        {
            List<List<string>> parsedlist = new List<List<string>>();

            string lyricGroupingStart = "（(";
            string lyricGroupingClose = ")）";
            int index = 0;

            for(index = 0; index < list.Count; index++)
            {
                parsedlist.Add(new List<string>());

                if (!lyricGroupingStart.Contains(list[index].Lyric) && !lyricGroupingClose.Contains(list[index].Lyric))
                {
                    // 開始カッコ、閉じカッコではない
                    parsedlist[parsedlist.Count - 1].Add(list[index].Lyric);
                }
                else if (lyricGroupingClose.Contains(list[index].Lyric))
                {
                    // 開始カッコの前に閉じカッコが来た
                    throw new Exception(string.Format(@"lyric Part column {0}, {1} is syntax error.", list[index].Column + 1, list[index].Lyric));
                }
                else
                {
                    // 開始カッコが来た

                    int spos = index;
                    int epos = index;

                    while (epos < list.Count)
                    {
                        if (!lyricGroupingClose.Contains(list[epos].Lyric))
                        {
                            //  ( ... ) を処理
                            var fff = list.Where((v, idx) => idx > spos).ToList();

                            foreach (var item in list.Where((v, idx) => idx > spos).ToList())
                            {
                                epos++;

                                if (lyricGroupingClose.Contains(item.Lyric)) break;

                                if (lyricGroupingStart.Contains(item.Lyric))
                                {
                                    // 閉じカッコに来る前にまた開始カッコが来た
                                    throw new Exception(string.Format(@"lyric Part column {0}, {1} is syntax error.", item.Column + 1, item.Lyric));
                                }

                                parsedlist[parsedlist.Count - 1].Add(item.Lyric);
                            }

                            index = epos;

                            break;
                        }
                        else
                        {
                            epos++;
                        }
                    }
                }
            }

            return parsedlist;
        }


        private List<List<string>> ParseLyricString(string lyric)
        {
            string lyricParentChars2 = @"[きキぎギ][ゃャゅュょョ]ー{0,1}|" +
                                       @"[しじシジ][ゃャゅュょョ]ー{0,1}|" +
                                       @"[ちチぢヂ][ゃャゅュょョ]ー{0,1}|" +
                                       @"[にニ][ゃャゅュょョ]ー{0,1}|" +
                                       @"[ひヒびビぴピ][ゃャゅュょョ]ー{0,1}|" +
                                       @"[みミ][ゃャゅュょョ]ー{0,1}|" +
                                       @".";
            string lyricParentChars3 = @"[ーっッ]";

            int lyricposition = 0;
            var planelist = new List<MyLyricInfo>();

            var lp = Regex.Matches(lyric, lyricParentChars2).GetEnumerator();
            while (lp.MoveNext())
            {
                string lyricChar = Regex.Replace(lp.Current.ToString(), lyricParentChars3, "");

                if (lyricChar != "")
                {
                    MyLyricInfo mylyric = new MyLyricInfo
                    {
                        Column = lyricposition,
                        Lyric = lyricChar
                    };
                    lyricposition += lp.Current.ToString().Length;

                    planelist.Add(mylyric);
                }
            }

            var lyricList = AggregationLyrics(planelist);

            return lyricList;
        }

        private List<MyNoteInfo> GenMyNoteInfoFromMyMMLInfo(ref List<MyMMLInfo> mmllist)
        {
            List<MyNoteInfo> mynotes = new List<MyNoteInfo>();

            foreach (var item in mmllist)
            {
                string macro = item.MacroName.Substring(0, 1);
                switch (macro)
                {
                    case "T":
                    case "L":
                    case "O":
                        break;

                    default:
                        // 音符・休符の生成
                        var noteItem = new MyNoteInfo();
                        mynotes.Add(noteItem);

                        noteItem.Note = item.MacroName;
                        noteItem.Lyric = item.SampleLyric;

                        noteItem.Key = OctaveToKeyMap[item.Octave] + (macro == "R" ? 0 : NoteToKeyDispMap[item.MacroName]);
                        noteItem.FrameLength = NoteLengthToFrameLengthMap[item.NoteLen];

                        if (item.WithDot) noteItem.FrameLength *= 1.5; // 付点音符・付点休符の時の補正
                        break;
                }
            }

            return mynotes;
        }

        private void AssignLyricToMyNoteInfo(ref List<List<string>> lyriclist, ref List<MyNoteInfo> mynotes)
        {
            int lyricindex = 0;
            int noteIndex = 0;

            if (lyriclist.Count == 0) return;

            while (noteIndex < mynotes.Count)
            {
                string macro = mynotes[noteIndex].Note.Substring(0, 1);
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
                                mynotes[noteIndex].Lyric = lyriclist[lyricindex++][0];
                                noteIndex++;
                            }
                            else
                            {
                                // 音符が分割される場合
                                var targetNoteFrameLength = mynotes[noteIndex].FrameLength;
                                int dividedNoteFrameLength = Convert.ToInt32(mynotes[noteIndex].FrameLength / lyriclist[lyricindex].Count);
                                int difflen = Convert.ToInt32( targetNoteFrameLength - (lyriclist[lyricindex].Count * dividedNoteFrameLength ));

                                // clone()を実装しないので
                                var FrameLength = mynotes[noteIndex].FrameLength;
                                var Key = mynotes[noteIndex].Key;
                                var Lyric = mynotes[noteIndex].Lyric;
                                var Note = mynotes[noteIndex].Note;

                                // 音符追加
                                for (int cnt = 0; cnt < (lyriclist[lyricindex].Count - 1); cnt++)
                                {
                                    var newNote = new MyNoteInfo();
                                    newNote.FrameLength = FrameLength;
                                    newNote.Key = Key;
                                    newNote.Lyric = Lyric;
                                    newNote.Note = Note;

                                    mynotes.Insert(noteIndex, newNote);
                                }

                                // フレーム長と歌詞の再割り当て
                                for (int cnt = 0; cnt < lyriclist[lyricindex].Count; cnt++)
                                {
                                    mynotes[noteIndex].Lyric = lyriclist[lyricindex][cnt];
                                    mynotes[noteIndex].FrameLength = dividedNoteFrameLength;

                                    noteIndex++;
                                }
                                mynotes[noteIndex - 1].FrameLength += difflen;

                                lyricindex++;
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
