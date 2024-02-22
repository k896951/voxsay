using System;

namespace voxsay
{
    public class MyMMLInfo
    {
        public string MacroName { get; set; }

        public string SampleLyric { get; set; } = "";

        public string NoteLen { get; set; }

        public Boolean WithDot { get; set; } = false;

        public int Tempo { get; set; }

        public int Octave { get; set; }

        public int Key { get; set; }
    }
}
