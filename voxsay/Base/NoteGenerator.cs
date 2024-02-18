using NAudio.CoreAudioApi;
using NAudio.Gui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using voxsay.Base.VoiceVox;

namespace voxsay
{
    public class NoteGenerator
    {
        private int tempo;
        private int octave;
        private int notelen;
        private LyricParser lyricParser;
        private MMLParser mmlParser;

        private const double FrameDurationParTick = 0.01;
        private const double QuarterNoteFrameLength = 50.0;  // BPM=120

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
            lyricParser = new LyricParser();
            mmlParser = new MMLParser(120, 4, 4); // T120, O4, L4
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
                        noteRinfo.Key = OctaveToKeyMap[octave];
                        noteRinfo.FrameLength = 2; // NoteLengthToFrameLengthMap[DefaultNoteLen];
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
