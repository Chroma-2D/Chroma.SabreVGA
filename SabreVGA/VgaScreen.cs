using System;
using System.Collections.Generic;
using System.Numerics;
using Chroma.Diagnostics.Logging;
using Chroma.Graphics;
using Chroma.Graphics.TextRendering;
using Chroma.Windowing;

namespace SabreVGA
{
    public class VgaScreen
    {
        private Log Log { get; } = LogManager.GetForCurrentAssembly();
        private Window Window { get; }

        public TrueTypeFont Font { get; private set; }

        public readonly Color DefaultForegroundColor = Color.Gray;

        public Cursor Cursor { get; private set; }

        public char[] CharacterBuffer { get; private set; }
        public Color[] ForegroundColorBuffer { get; private set; }

        public int TotalColumns { get; private set; }
        public int TotalRows { get; private set; }

        public int WindowColumns => TotalColumns - Margins.Left - Margins.Right;
        public int WindowRows => TotalRows - Margins.Top - Margins.Bottom;

        public int CellWidth { get; private set; }
        public int CellHeight { get; private set; }

        public VgaMargins Margins { get; private set; }

        public Color ActiveForegroundColor { get; set; }

        public Dictionary<char, Vector2> InCellCharacterOffsets { get; set; }

        public VgaScreen(Window window, TrueTypeFont font)
            : this(window, font, font.Measure("X").Width, font.Size)
        {
        }

        public VgaScreen(Window window, TrueTypeFont font, int cellWidth, int cellHeight)
        {
            ActiveForegroundColor = DefaultForegroundColor;

            Window = window;
            Font = font;

            CellWidth = cellWidth;
            CellHeight = cellHeight;

            Cursor = new Cursor(this);
            Margins = new VgaMargins(1, 1, 1, 1);

            InCellCharacterOffsets = new Dictionary<char, Vector2>();

            RecalculateDimensions();
        }

        public void PutCharAt(char character, int x, int y) =>
            PutCharAt(character, ActiveForegroundColor, x, y);

        public void PutCharAt(char character, Color foreground, int x, int y)
        {
            if (y < 0 || y >= TotalRows || x < 0 || x >= TotalColumns)
            {
                Log.Error($"Tried to put char '{(int)character}' at ({x},{y}) which are out of bounds.");
                return;
            }

            CharacterBuffer[y * TotalColumns + x] = character;
            ForegroundColorBuffer[y * TotalColumns + x] = foreground;
        }

        public void SetColorAt(Color foreground, int x, int y, bool flushBackdrop = true)
        {
            if (y < 0 || y >= TotalRows || x < 0 || x >= TotalColumns)
            {
                Log.Error(
                    $"Tried to set color FG:'{foreground.PackedValue:X6}' at ({x},{y}) which are out of bounds.");

                return;
            }

            ForegroundColorBuffer[y * TotalColumns + x] = foreground;
        }

        public void ClearScreen(bool preserveColors)
        {
            for (var y = Margins.Top; y < TotalRows - Margins.Bottom; y++)
            {
                if (preserveColors)
                {
                    for (var x = Margins.Left; x < TotalColumns - Margins.Right; x++)
                        CharacterBuffer[y * TotalRows + x] = ' ';
                }
                else
                {
                    for (var x = Margins.Left; x < TotalColumns - Margins.Right; x++)
                    {
                        CharacterBuffer[y * TotalColumns + x] = ' ';
                        ForegroundColorBuffer[y * TotalColumns + x] = DefaultForegroundColor;
                    }

                    ActiveForegroundColor = DefaultForegroundColor;
                }
            }
        }

        public void ScrollUp()
        {
            for (var y = Margins.Top + 1; y < TotalRows - Margins.Bottom; y++)
            {
                for (var x = Margins.Left; x < TotalColumns - Margins.Right; x++)
                {
                    CharacterBuffer[(y - 1) * TotalColumns + x] = CharacterBuffer[y * TotalColumns + x];
                    ForegroundColorBuffer[(y - 1) * TotalColumns + x] = ForegroundColorBuffer[y * TotalColumns + x];
                }
            }

            for (var x = Margins.Left; x < TotalColumns - Margins.Right; x++)
            {
                CharacterBuffer[(TotalRows - Margins.Bottom - 1) * TotalColumns + x] = ' ';
                ForegroundColorBuffer[(TotalRows - Margins.Bottom - 1) * TotalColumns + x] = DefaultForegroundColor;
            }
        }

        public void Update(float delta)
        {
            Cursor.Color = ActiveForegroundColor;
            Cursor.Update(delta);
        }

        public void Draw(RenderContext context)
        {
            DrawDisplayBuffer(context);
            Cursor.Draw(context);
        }

        public void RecalculateDimensions()
        {
            try
            {
                Cursor.X = Margins.Left;
                Cursor.Y = Margins.Top;

                TotalColumns = Window.Size.Width / CellWidth;
                TotalRows = Window.Size.Height / CellHeight;

                var bufferSize = TotalColumns * TotalRows;
                CharacterBuffer = new char[bufferSize];
                ForegroundColorBuffer = new Color[bufferSize];

                for (var y = 0; y < TotalRows; y++)
                {
                    for (var x = 0; x < TotalColumns; x++)
                    {
                        var pos = y * TotalColumns + x;

                        CharacterBuffer[pos] = ' ';
                        ForegroundColorBuffer[pos] = DefaultForegroundColor;
                    }
                }
            }
            catch (OverflowException)
            {
                FailsafeReset();
            }
        }
        
        private void FailsafeReset()
        {
            Margins = new VgaMargins(1, 1, 1, 1);
            RecalculateDimensions();
        }

        private void DrawDisplayBuffer(RenderContext context)
        {
            for (var y = 0; y < TotalRows; y++)
            {
                var start = y * TotalColumns;
                var end = start + TotalColumns;

                var str = new string(CharacterBuffer[start..end]);

                try
                {
                    var y1 = y;
                    var pos = new Vector2(0, (y1 - Margins.Top) * CellHeight);

                    context.DrawString(
                        Font,
                        str,
                        pos,
                        (c, i, p, g) =>
                        {
                            var offset = Vector2.Zero;

                            if (InCellCharacterOffsets.ContainsKey(c))
                                offset = InCellCharacterOffsets[c];

                            return new GlyphTransformData(
                                new Vector2(
                                    (i * CellWidth) + (CellWidth / 2) - (int)(g.BitmapSize.X / 2),
                                    (Margins.Top * CellHeight) + p.Y
                                ) + offset
                            )
                            {
                                Color = ForegroundColorBuffer[y1 * TotalColumns + i]
                            };
                        });
                }
                catch
                {
                    /* Ignored */
                }
            }
        }
    }
}