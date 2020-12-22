using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Chroma.Diagnostics.Logging;
using Chroma.Graphics;
using Chroma.Graphics.TextRendering;
using Chroma.MemoryManagement;
using Chroma.Windowing;
using Color = Chroma.Graphics.Color;

namespace Chroma.SabreVGA
{
    public class VgaScreen : DisposableResource
    {
        public static readonly Color DefaultForegroundColor = Color.Gray;
        public static readonly Color DefaultBackgroundColor = Color.Transparent;

        private Size _size;

        private Log Log { get; } = LogManager.GetForCurrentAssembly();

        private int BlinkTimer { get; set; }
        private bool BlinkingVisible { get; set; } = true;
        
        private RenderTarget BackgroundRenderTarget { get; set; }
        private RenderTarget ForegroundRenderTarget { get; set; }

        public TrueTypeFont Font { get; private set; }

        public Cursor Cursor { get; private set; }

        public VgaCell[] Buffer { get; private set; }
        public int CellBlinkInterval { get; set; } = 500;

        public Vector2 Position { get; set; }

        public Size Size
        {
            get => _size;
            set
            {
                _size = value;
                
                BackgroundRenderTarget.Dispose();
                ForegroundRenderTarget.Dispose();
                
                FinishInitialization();
            }
        }

        public int TotalColumns { get; private set; }
        public int TotalRows { get; private set; }

        public int WindowColumns => TotalColumns - Margins.Left - Margins.Right;
        public int WindowRows => TotalRows - Margins.Top - Margins.Bottom;

        public int CellWidth { get; private set; }
        public int CellHeight { get; private set; }

        public VgaMargins Margins { get; set; }

        public Color ActiveForegroundColor { get; set; } = DefaultForegroundColor;
        public Color ActiveBackgroundColor { get; set; } = DefaultBackgroundColor;

        public Dictionary<char, Vector2> InCellCharacterOffsets { get; }

        public VgaScreen(Vector2 position, Size size, TrueTypeFont font, int cellWidth, int cellHeight)
        {
            Position = position;
            _size = size;

            Font = font;

            CellWidth = cellWidth;
            CellHeight = cellHeight;

            Cursor = new Cursor(this);
            Margins = new VgaMargins(1, 1, 1, 1);

            InCellCharacterOffsets = new Dictionary<char, Vector2>();
            
            FinishInitialization();
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
                    $"Tried to set color FG:'{foreground.Packed.RGBA:X6}' at ({x},{y}) which are out of bounds.");

                return;
            }

            Buffer[y * TotalColumns + x].Foreground = foreground;
            Buffer[y * TotalColumns + x].Background = background;
        }

        public void Clear()
        {
            for (var y = Margins.Top; y < TotalRows - Margins.Bottom; y++)
            {
                for (var x = Margins.Left; x < TotalColumns - Margins.Right; x++)
                    Buffer[y * TotalRows + x].Character = ' ';
            }
        }

        public void Clear(Color foreground, Color background, bool setActiveColors = false)
        {
            for (var y = Margins.Top; y < TotalRows - Margins.Bottom; y++)
            {
                for (var x = Margins.Left; x < TotalColumns - Margins.Right; x++)
                {
                    Buffer[y * TotalColumns + x].Character = ' ';
                    Buffer[y * TotalColumns + x].Foreground = foreground;
                    Buffer[y * TotalColumns + x].Background = background;
                }

                if (setActiveColors)
                {
                    ActiveForegroundColor = foreground;
                    ActiveBackgroundColor = background;
                }
            }
        }

        public void ClearToDefaults()
            => Clear(DefaultForegroundColor, DefaultBackgroundColor, true);

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

            if (BlinkTimer >= CellBlinkInterval)
            {
                BlinkingVisible = !BlinkingVisible;
                BlinkTimer = 0;
            }

            Cursor.Color = ActiveForegroundColor;
            Cursor.Update(delta);
        }

        public void Draw(RenderContext context)
        {
            context.RenderTo(BackgroundRenderTarget, () =>
            {
                context.Clear(Color.Transparent);
                DrawBackgroundBuffer(context);
            });
            
            context.RenderTo(ForegroundRenderTarget, () =>
            {
                context.Clear(Color.Transparent);
                DrawDisplayBuffer(context);
            });
            
            context.DrawTexture(BackgroundRenderTarget, Position, Vector2.One, Vector2.Zero, 0f);
            Cursor.Draw(context);

            context.DrawTexture(ForegroundRenderTarget, Position, Vector2.One, Vector2.Zero, 0f);            
        }

        public void RecalculateDimensions()
        {
            try
            {
                Cursor.X = Margins.Left;
                Cursor.Y = Margins.Top;

                TotalColumns = Size.Width / CellWidth;
                TotalRows = Size.Height / CellHeight;

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

        public VgaCell GetCell(int x, int y)
        {
            if (y < 0 || y >= TotalRows || x < 0 || x >= TotalColumns)
            {
                Log.Error($"Trying to get out-of-bounds cell info.");
                return new VgaCell('\0', Color.Black, Color.Black);
            }

            return Buffer[y * TotalColumns + x];
        }

        protected override void FreeManagedResources()
        {
            BackgroundRenderTarget.Dispose();
            ForegroundRenderTarget.Dispose();
        }

        private void FinishInitialization()
        {
            RecalculateDimensions();
            
            ForegroundRenderTarget = new RenderTarget(Size);
            BackgroundRenderTarget = new RenderTarget(Size);
        }

        private void FailsafeReset()
        {
            Margins = new VgaMargins(1, 1, 1, 1);
            RecalculateDimensions();
        }

        private void DrawBackgroundBuffer(RenderContext context)
        {
            context.ShapeBlendingEnabled = false;
            for (var y = 0; y < TotalRows; y++)
            {
                for (var x = 0; x < TotalColumns; x++)
                {
                    context.Rectangle(
                        ShapeMode.Fill,
                        new Vector2(x * CellWidth, y * CellHeight),
                        CellWidth, CellHeight,
                        Buffer[y * TotalColumns + x].Background
                    );
                }
            }
            context.ShapeBlendingEnabled = true;
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