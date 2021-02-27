using Chroma.Graphics;

namespace Chroma.SabreVGA
{
    public struct VgaCell
    {
        internal static VgaCell Dummy = new(); // used for out-of-bounds buffer misses
        
        public static readonly VgaCell Empty = new();
        
        public char Character;
        public Color Foreground;
        public Color Background;
        public bool Blink;

        public VgaCell(char character, Color fg, Color bg, bool blink = false)
        {
            Character = character;
            Foreground = fg;
            Background = bg;
            Blink = blink;
        }
    }
}