using Chroma.Graphics;

namespace SabreVGA
{
    public struct VgaCell
    {
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