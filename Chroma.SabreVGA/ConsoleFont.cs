using Chroma.Graphics.TextRendering.Bitmap;
using Chroma.Graphics.TextRendering.TrueType;

namespace Chroma.SabreVGA
{
    public class ConsoleFont
    {
        public TrueTypeFont TrueTypeFont { get; private set; }
        public BitmapFont BitmapFont { get; private set; }

        public bool IsBitmapFont => TrueTypeFont == null && BitmapFont != null;
        public bool IsTrueTypeFont => TrueTypeFont != null && BitmapFont == null;

        internal ConsoleFont(BitmapFont bitmapFont)
            => BitmapFont = bitmapFont;

        internal ConsoleFont(TrueTypeFont trueTypeFont)
            => TrueTypeFont = trueTypeFont;
    }
}