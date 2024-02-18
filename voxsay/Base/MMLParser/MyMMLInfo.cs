using System;

namespace voxsay
{
    public class MyMMLInfo
    {
        public string MacroName { get; set; }

        public string SampleLyric { get; set; } = "";

        public int NoteLen { get; set; }

        public Boolean WithDot { get; set; } = false;

        public int Tempo { get; set; }

        public int Octave { get; set; }

    }
}
