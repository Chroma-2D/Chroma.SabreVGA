using Chroma.Graphics;

namespace Chroma.SabreVGA
{
    public static class VgaScreenExtensions
    {
        public static void PutCharAt(this VgaScreen screen,
            int x, int y, char character)
        {
            screen[x, y].Character = character;
        }

        public static void PutCharAt(this VgaScreen screen,
            int x, int y, char character, bool blink)
        {
            screen[x, y].Character = character;
            screen[x, y].Blink = blink;
        }

        public static void PutCharAt(this VgaScreen screen, 
            int x, int y, char character, Color foreground, Color background, bool blink)
        {
            screen.SetColorAt(x, y, foreground, background);
            screen.PutCharAt(x, y, character, blink);
        }

        public static void SetColorAt(this VgaScreen screen, 
            int x, int y, Color foreground, Color background)
        {
            screen[x, y].Foreground = foreground;
            screen[x, y].Background = background;
        }

        public static void ClearToColor(this VgaScreen screen, 
            Color foreground, Color background)
        {
            screen.ActiveForegroundColor = foreground;
            screen.ActiveBackgroundColor = background;

            for (var y = 0; y < screen.TotalRows; y++)
            {
                for (var x = 0; x < screen.TotalColumns; x++)
                {
                    screen[x, y].Character = ' ';
                    screen[x, y].Blink = false;
                    screen.SetColorAt(x, y, foreground, background);
                }
            }
        }

        public static void Scroll(this VgaScreen screen)
        {
            for (var y = screen.Margins.Top + 1; y < screen.TotalRows - screen.Margins.Bottom; y++)
            {
                for (var x = screen.Margins.Left; x < screen.TotalColumns - screen.Margins.Right; x++)
                {
                    screen[x, y - 1].Character = screen[x, y].Character;
                    screen[x, y - 1].Foreground = screen[x, y].Foreground;
                    screen[x, y - 1].Background = screen[x, y].Background;
                    screen[x, y - 1].Blink = screen[x, y].Blink;
                }
            }

            for (var x = screen.Margins.Left; x < screen.TotalColumns - screen.Margins.Right; x++)
            {
                var lastLineCoord = screen.TotalRows - screen.Margins.Bottom - 1;

                screen[x, lastLineCoord].Character = ' ';
                screen[x, lastLineCoord].Foreground = VgaScreen.DefaultForegroundColor;
                screen[x, lastLineCoord].Background = VgaScreen.DefaultBackgroundColor;
                screen[x, lastLineCoord].Blink = false;
            }
        }
    }
}