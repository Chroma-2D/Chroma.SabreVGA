namespace Chroma.SabreVGA
{
    public struct VgaMargins
    {
        public byte Left;
        public byte Top;
        public byte Right;
        public byte Bottom;

        public VgaMargins(byte uniform)
            : this(uniform, uniform, uniform, uniform)
        {
        }

        public VgaMargins(byte left, byte top, byte right, byte bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }
}