using Chroma.Graphics.TextRendering.Bitmap;
using Chroma.Graphics.TextRendering.TrueType;

namespace Chroma.SabreVGA
{
    public class ConsoleFont
    {
        public TrueTypeFont TrueTypeFont { get; }
        public BitmapFont BitmapFont { get; }

        public bool IsBitmapFont => TrueTypeFont == null && BitmapFont != null;
        public bool IsTrueTypeFont => TrueTypeFont != null && BitmapFont == null;

        public ConsoleFont(BitmapFont bitmapFont)
            => BitmapFont = bitmapFont;

        public ConsoleFont(TrueTypeFont trueTypeFont)
            => TrueTypeFont = trueTypeFont;
    }
}