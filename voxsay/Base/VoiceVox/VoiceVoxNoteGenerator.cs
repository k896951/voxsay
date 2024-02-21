using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using voxsay.Base.VoiceVox;

namespace voxsay
{
    public class VoiceVoxNoteGenerator
    {
        private int currentTempo;
        private int currentOctave;
        private int currentNotelen;
        private LyricParser lyricParser;
        private MMLParser mmlParser;

        private const double FrameDuration = 0.010752688; // BPM=120時、1フレームの時間

        private Dictionary<string, double> NoteLengthToFrameLengthMap = new Dictionary<string, double>()
        {
            {   "1.", 300.0    },
            {   "1" , 200.0    },
            {   "2.", 150.0    },
            {   "2" , 100.0    },
            {   "4.",  75.0    },
            {   "4" ,  50.0    },
            {   "8.",  37.5    },
            {   "8" ,  25.0    },
            {  "16.",  18.75   },
            {  "16" ,  12.5    },
            {  "32.",   9.375  },
            {  "32" ,   6.25   },
            {  "64.",   4.6875 },
            {  "64",    3.125  },
            { "128.",   2.34375},
            { "128" ,   1.5625 }
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
                return currentTempo;
            }

            set
            {
                currentTempo = value;

                var framelength = (60.0 / currentTempo) / FrameDuration;

                NoteLengthToFrameLengthMap["1."]   = framelength * 4 * 1.5;  // 　　　付点全音符フレーム数
                NoteLengthToFrameLengthMap["1"]    = framelength * 4;        // 　　　　　全音符フレーム数
                NoteLengthToFrameLengthMap["2."]   = framelength * 2 * 1.5;  // 　　付点２分音符フレーム数
                NoteLengthToFrameLengthMap["2"]    = framelength * 2;        // 　　　　２分音符フレーム数
                NoteLengthToFrameLengthMap["4."]   = framelength * 1.5;      // 　　付点４分音符フレーム数
                NoteLengthToFrameLengthMap["4"]    = framelength;            // 　　　　４分音符フレーム数
                NoteLengthToFrameLengthMap["8."]   = framelength / 2 * 1.5;  // 　　付点８分音符フレーム数
                NoteLengthToFrameLengthMap["8"]    = framelength / 2;        // 　　　　８分音符フレーム数
                NoteLengthToFrameLengthMap["16."]  = framelength / 4 * 1.5;  // 　付点１６分音符フレーム数
                NoteLengthToFrameLengthMap["16"]   = framelength / 4;        // 　　　１６分音符フレーム数
                NoteLengthToFrameLengthMap["32."]  = framelength / 8 * 1.5;  // 　付点３２分音符フレーム数
                NoteLengthToFrameLengthMap["32"]   = framelength / 8;        //　　 　３２分音符フレーム数
                NoteLengthToFrameLengthMap["64."]  = framelength / 16 * 1.5; // 　付点６４分音符フレーム数
                NoteLengthToFrameLengthMap["64"]   = framelength / 16;       // 　　　６４分音符フレーム数
                NoteLengthToFrameLengthMap["128."] = framelength / 32 * 1.5; // 付点１２８分音符フレーム数
                NoteLengthToFrameLengthMap["128"]  = framelength / 32;       // 　　１２８分音符フレーム数
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
                if (OctaveToKeyMap.ContainsKey(value)) currentOctave = value;
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
                if (NoteLengthToFrameLengthMap.ContainsKey(value.ToString()))
                {
                    currentNotelen = value;
                }
            }
        }


        public VoiceVoxNoteGenerator()
        {
            // デフォルト T120, O4, L4 に設定

            Bpm = 120;
            Octave = 4;
            DefaultNoteLen = 4;
            mmlParser = new MMLParser(Bpm, Octave, DefaultNoteLen);
            lyricParser = new LyricParser();
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
                    var parts = Regex.Split(measure.Trim(), @"[:：]+");
                    switch (parts.Length)
                    {
                        case 1: // MMLだけある場合
                            mmllist = mmlParser.ParseMMLString(parts[0].Trim());
                            notelist = GenMyNoteInfoFromMyMMLInfo(ref mmllist);
                            break;

                        case 2:  // 歌詞とMMLがある場合
                            lyriclist = lyricParser.ParseLyricString(parts[0].Trim());
                            mmllist = mmlParser.ParseMMLString(parts[1].Trim());
                            notelist = GenMyNoteInfoFromMyMMLInfo(ref mmllist);
                            AssignLyricToMyNoteInfo(ref lyriclist, ref notelist);
                            break;

                        default:
                            throw new Exception(string.Format(@"Incorrect definition."));
                    }

                    // 最初に休符を入れておく
                    if (mynoteinfo.Count == 0)
                    {
                        var noteRinfo = new MyNoteInfo();
                        noteRinfo.Lyric = "";
                        noteRinfo.Note = "R";
                        noteRinfo.Key = OctaveToKeyMap[currentOctave];
                        noteRinfo.FrameLength = 2;
                        mynoteinfo.Add(noteRinfo);
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

        public void PrintAssignInfo(List<MyNoteInfo> mynotes)
        {
            int noteindex = 0;
            foreach (var note in mynotes)
            {
                var notelenStr = NoteLengthToFrameLengthMap.FirstOrDefault(v => v.Value == note.FrameLength).Key;
                if (notelenStr == "0")
                {
                    notelenStr = NoteLengthToFrameLengthMap.FirstOrDefault(v => v.Value == (note.FrameLength / 1.5)).Key.ToString();
                    notelenStr = notelenStr != "0" ? notelenStr + "." : "";
                }

                switch(note.Note)
                {
                    case "R":
                        Console.WriteLine(@"{0,4:D} {1}{2,-4:G} (---,{3,6:D})", noteindex, note.Note, notelenStr, Convert.ToInt32(note.FrameLength));
                        break;

                    default:
                        Console.WriteLine(@"{0,4:D} {1}{2,-4:G} ({3,3:D},{4,6:D}), {5}", noteindex, note.Note, notelenStr, note.Key, Convert.ToInt32(note.FrameLength), note.Lyric + (note.defaultLyric ? "(default)" : "") );
                        break;
                }

                noteindex++;
            }

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
                        Bpm = item.Tempo;
                        break;

                    case "O":
                        Octave = item.Octave;
                        break;

                    case "L":
                        DefaultNoteLen = item.NoteLen;
                        break;

                    default:
                        // 音符・休符の生成
                        var noteItem = new MyNoteInfo();
                        mynotes.Add(noteItem);

                        noteItem.Note = item.MacroName;
                        noteItem.Lyric = item.SampleLyric;
                        noteItem.defaultLyric = true;

                        noteItem.Key = OctaveToKeyMap[item.Octave] + (macro == "R" ? 0 : NoteToKeyDispMap[item.MacroName]);

                        var notelenStr = item.NoteLen.ToString() + (item.WithDot ? "." : "");
                        noteItem.FrameLength = NoteLengthToFrameLengthMap[notelenStr];

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
                                mynotes[noteIndex].defaultLyric = false;
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
                                var DefaultLyric = mynotes[noteIndex].defaultLyric;

                                // 音符追加
                                for (int cnt = 0; cnt < (lyriclist[lyricindex].Count - 1); cnt++)
                                {
                                    var newNote = new MyNoteInfo();
                                    newNote.FrameLength = FrameLength;
                                    newNote.Key = Key;
                                    newNote.Lyric = Lyric;
                                    newNote.Note = Note;
                                    newNote.defaultLyric = DefaultLyric;

                                    mynotes.Insert(noteIndex, newNote);
                                }

                                // フレーム長と歌詞の再割り当て
                                for (int cnt = 0; cnt < lyriclist[lyricindex].Count; cnt++)
                                {
                                    mynotes[noteIndex].Lyric = lyriclist[lyricindex][cnt];
                                    mynotes[noteIndex].FrameLength = dividedNoteFrameLength;
                                    mynotes[noteIndex].defaultLyric = false;

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
