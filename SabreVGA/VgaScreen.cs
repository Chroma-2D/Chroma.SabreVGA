using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
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

        private bool Running { get; set; } = true;

        private int BlinkTimer { get; set; }
        private bool BlinkingVisible { get; set; } = true;

        private Texture BackgroundRender { get; }
        private Thread BackgroundRenderThread { get; }

        public TrueTypeFont Font { get; private set; }

        public static readonly Color DefaultForegroundColor = Color.Gray;
        public static readonly Color DefaultBackgroundColor = Color.Transparent;

        public Cursor Cursor { get; private set; }

        public VgaCell[] Buffer { get; private set; }
        public int BlinkInterval { get; set; } = 500;

        public int TotalColumns { get; private set; }
        public int TotalRows { get; private set; }

        public int WindowColumns => TotalColumns - Margins.Left - Margins.Right;
        public int WindowRows => TotalRows - Margins.Top - Margins.Bottom;

        public int CellWidth { get; private set; }
        public int CellHeight { get; private set; }

        public VgaMargins Margins { get; private set; }

        public Color ActiveForegroundColor { get; set; } = DefaultForegroundColor;
        public Color ActiveBackgroundColor { get; set; } = DefaultBackgroundColor;

        public Dictionary<char, Vector2> InCellCharacterOffsets { get; set; }

        public VgaScreen(Window window, TrueTypeFont font)
            : this(window, font, font.Measure("X").Width, font.Size)
        {
        }

        public VgaScreen(Window window, TrueTypeFont font, int cellWidth, int cellHeight)
        {
            Window = window;
            Font = font;

            CellWidth = cellWidth;
            CellHeight = cellHeight;

            Cursor = new Cursor(this);
            Margins = new VgaMargins(1, 1, 1, 1);

            InCellCharacterOffsets = new Dictionary<char, Vector2>();

            BackgroundRender = new Texture(Window.Size.Width, Window.Size.Height);
            BackgroundRenderThread = new Thread(() =>
            {
                while (Running)
                {
                    UpdateBackground();
                    Thread.Sleep(TimeSpan.FromTicks(100));
                }
            });

            Window.QuitRequested += (sender, args) => { Running = false; };

            RecalculateDimensions();
            BackgroundRenderThread.Start();
        }

        public void PutCharAt(char character, int x, int y) =>
            PutCharAt(character, x, y, ActiveForegroundColor, ActiveBackgroundColor, false);

        public void PutCharAt(char character, int x, int y, Color foreground, Color background, bool blink)
        {
            if (y < 0 || y >= TotalRows || x < 0 || x >= TotalColumns)
            {
                Log.Error($"Tried to put char '{(int)character}' at ({x},{y}) which are out of bounds.");
                return;
            }

            Buffer[y * TotalColumns + x].Character = character;
            Buffer[y * TotalColumns + x].Foreground = foreground;
            Buffer[y * TotalColumns + x].Background = background;
            Buffer[y * TotalColumns + x].Blink = blink;
        }

        public void SetColorAt(Color foreground, Color background, int x, int y)
        {
            if (y < 0 || y >= TotalRows || x < 0 || x >= TotalColumns)
            {
                Log.Error(
                    $"Tried to set color FG:'{foreground.PackedValue:X6}' at ({x},{y}) which are out of bounds.");

                return;
            }

            Buffer[y * TotalColumns + x].Foreground = foreground;
            Buffer[y * TotalColumns + x].Background = background;
        }

        public void ClearScreen(bool preserveColors)
        {
            for (var y = Margins.Top; y < TotalRows - Margins.Bottom; y++)
            {
                if (preserveColors)
                {
                    for (var x = Margins.Left; x < TotalColumns - Margins.Right; x++)
                        Buffer[y * TotalRows + x].Character = ' ';
                }
                else
                {
                    for (var x = Margins.Left; x < TotalColumns - Margins.Right; x++)
                    {
                        Buffer[y * TotalColumns + x].Character = ' ';
                        Buffer[y * TotalColumns + x].Foreground = DefaultForegroundColor;
                        Buffer[y * TotalColumns + x].Background = DefaultBackgroundColor;
                    }

                    ActiveForegroundColor = DefaultForegroundColor;
                    ActiveBackgroundColor = DefaultBackgroundColor;
                }
            }
        }

        public void ScrollUp()
        {
            for (var y = Margins.Top + 1; y < TotalRows - Margins.Bottom; y++)
            {
                for (var x = Margins.Left; x < TotalColumns - Margins.Right; x++)
                {
                    Buffer[(y - 1) * TotalColumns + x].Character = Buffer[y * TotalColumns + x].Character;
                    Buffer[(y - 1) * TotalColumns + x].Foreground = Buffer[y * TotalColumns + x].Foreground;
                    Buffer[(y - 1) * TotalColumns + x].Background = Buffer[y * TotalColumns + x].Background;
                    Buffer[(y - 1) * TotalColumns + x].Blink = Buffer[y * TotalColumns + x].Blink;
                }
            }

            for (var x = Margins.Left; x < TotalColumns - Margins.Right; x++)
            {
                Buffer[(TotalRows - Margins.Bottom - 1) * TotalColumns + x].Character = ' ';
                Buffer[(TotalRows - Margins.Bottom - 1) * TotalColumns + x].Foreground = DefaultForegroundColor;
                Buffer[(TotalRows - Margins.Bottom - 1) * TotalColumns + x].Background = DefaultBackgroundColor;
                Buffer[(TotalRows - Margins.Bottom - 1) * TotalColumns + x].Blink = false;
            }
        }

        public void Update(float delta)
        {
            BlinkTimer += (int)(1000 * delta);

            if (BlinkTimer >= BlinkInterval)
            {
                BlinkingVisible = !BlinkingVisible;
                BlinkTimer = 0;
            }

            Cursor.Color = ActiveForegroundColor;
            Cursor.Update(delta);
        }

        public void Draw(RenderContext context)
        {
            BackgroundRender.Flush();
            context.DrawTexture(BackgroundRender, Vector2.Zero, Vector2.One, Vector2.Zero, 0f);

            Cursor.Draw(context);
            DrawDisplayBuffer(context);
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
                Buffer = new VgaCell[bufferSize];

                for (var y = 0; y < TotalRows; y++)
                {
                    for (var x = 0; x < TotalColumns; x++)
                    {
                        var pos = y * TotalColumns + x;

                        Buffer[pos] = new VgaCell(
                            ' ',
                            DefaultForegroundColor,
                            DefaultBackgroundColor
                        );
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

        private void UpdateBackground()
        {
            for (var y = 0; y < TotalRows; y++)
            {
                for (var x = 0; x < TotalColumns; x++)
                {
                    var cell = Buffer[y * TotalColumns + x];

                    var actualColor = new Color(
                        cell.Background.A,
                        cell.Background.R,
                        cell.Background.G,
                        cell.Background.B
                    );

                    for (var ty = y * CellHeight; ty < y * CellHeight + CellHeight; ty++)
                    {
                        for (var tx = x * CellWidth; tx < x * CellWidth + CellWidth; tx++)
                        {
                            BackgroundRender.SetPixel(tx, ty, actualColor);
                        }
                    }
                }
            }
        }

        private void DrawDisplayBuffer(RenderContext context)
        {
            for (var y = 0; y < TotalRows; y++)
            {
                var start = y * TotalColumns;
                var end = start + TotalColumns;

                var str = new string(Buffer[start..end].Select(c => c.Character).ToArray());

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

                        var cell = Buffer[y1 * TotalColumns + i];

                        return new GlyphTransformData(
                            new Vector2(
                                (i * CellWidth) + (CellWidth / 2) - (int)(g.BitmapSize.X / 2),
                                (Margins.Top * CellHeight) + p.Y
                            ) + offset
                        )
                        {
                            Color = (cell.Blink && !BlinkingVisible) ? Color.Transparent : cell.Foreground
                        };
                    }
                );
            }
        }
    }
}