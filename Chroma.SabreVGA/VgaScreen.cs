﻿using System.Drawing;
using System.Linq;
using System.Numerics;
using Chroma.Diagnostics.Logging;
using Chroma.Graphics;
using Chroma.Graphics.TextRendering;
using Chroma.MemoryManagement;
using Color = Chroma.Graphics.Color;

namespace Chroma.SabreVGA
{
    public class VgaScreen : DisposableResource
    {
        public static readonly Color DefaultBackgroundColor = Color.Black;
        public static readonly Color DefaultForegroundColor = Color.Gray;

        private static readonly Log _log = LogManager.GetForCurrentAssembly();

        private VgaCell[] _buffer = null!;
        private Size _size;
        private int _blinkTimer;
        private bool _blinkingCellsVisible = true;

        private RenderTarget? BackgroundRenderTarget { get; set; }
        private RenderTarget? ForegroundRenderTarget { get; set; }

        public int CellBlinkInterval { get; set; } = 500;

        public IFontProvider Font { get; set; }
        public Cursor Cursor { get; private set; }

        public Vector2 Position { get; set; }

        public Size Size
        {
            get => _size;
            set
            {
                _size = value;

                BackgroundRenderTarget?.Dispose();
                ForegroundRenderTarget?.Dispose();

                FinishInitialization();
            }
        }

        public int TotalColumns { get; private set; }
        public int TotalRows { get; private set; }

        public int CellWidth { get; private set; }
        public int CellHeight { get; private set; }

        public VgaMargins Margins { get; set; } = new(1, 1, 1, 1);
        public int WindowColumns => TotalColumns - Margins.Left - Margins.Right;
        public int WindowRows => TotalRows - Margins.Top - Margins.Bottom;

        public Color ActiveForegroundColor { get; set; } = DefaultForegroundColor;
        public Color ActiveBackgroundColor { get; set; } = DefaultBackgroundColor;

        public ref VgaCell this[int linearAddress]
            => ref _buffer[linearAddress];

        public ref VgaCell this[int x, int y]
        {
            get
            {
                if (y < 0 || y >= TotalRows || x < 0 || x >= TotalColumns)
                {
                    _log.Error($"Tried to retrieve an out-of-bounds cell ({x}, {y}).");
                    return ref VgaCell.Dummy;
                }

                return ref _buffer[y * TotalColumns + x];
            }
        }

        public VgaScreen(Vector2 position, Size size, IFontProvider font, int cellWidth, int cellHeight)
        {
            Position = position;
            _size = size;

            Font = font;

            CellWidth = cellWidth;
            CellHeight = cellHeight;

            Cursor = new Cursor(this);

            FinishInitialization();
        }

        public void Update(float delta)
        {
            _blinkTimer += (int)(1000 * delta);

            if (_blinkTimer >= CellBlinkInterval)
            {
                _blinkingCellsVisible = !_blinkingCellsVisible;
                _blinkTimer = 0;
            }

            Cursor.Color = this[Cursor.X, Cursor.Y].Foreground;
            Cursor.Update(delta);
        }

        public void Draw(RenderContext context)
        {
            if (BackgroundRenderTarget != null)
            {
                context.RenderTo(BackgroundRenderTarget, FlushBackgroundBuffer);
                context.DrawTexture(BackgroundRenderTarget, Position, Vector2.One, Vector2.Zero, 0f);
            }

            if (ForegroundRenderTarget != null)
            {
                context.RenderTo(ForegroundRenderTarget, FlushForegroundBuffer);
                context.DrawTexture(ForegroundRenderTarget, Position, Vector2.One, Vector2.Zero, 0f);
            }

            Cursor.Draw(context);
        }

        private void FlushBackgroundBuffer(RenderContext context, RenderTarget _)
        {
            context.Clear(Color.Transparent);
            DrawBackgroundBuffer(context);
        }

        private void FlushForegroundBuffer(RenderContext context, RenderTarget _)
        {
            context.Clear(Color.Transparent);
            DrawForegroundBuffer(context);
        }

        public void SetCellSizes(int cellWidth, int cellHeight)
        {
            CellWidth = cellWidth;
            CellHeight = cellHeight;

            BackgroundRenderTarget?.Dispose();
            ForegroundRenderTarget?.Dispose();

            FinishInitialization();
        }

        public void RecalculateDimensions()
        {
            Cursor.X = Margins.Left;
            Cursor.Y = Margins.Top;

            TotalColumns = Size.Width / CellWidth;
            TotalRows = Size.Height / CellHeight;

            _buffer = new VgaCell[TotalColumns * TotalRows];

            for (var y = 0; y < TotalRows; y++)
            {
                for (var x = 0; x < TotalColumns; x++)
                {
                    var pos = y * TotalColumns + x;

                    _buffer[pos] = new VgaCell(
                        ' ',
                        DefaultForegroundColor,
                        DefaultBackgroundColor
                    );
                }
            }
        }

        protected override void FreeManagedResources()
        {
            BackgroundRenderTarget?.Dispose();
            BackgroundRenderTarget = null;

            ForegroundRenderTarget?.Dispose();
            ForegroundRenderTarget = null;
        }

        private void FinishInitialization()
        {
            RecalculateDimensions();

            ForegroundRenderTarget = new RenderTarget(Size);
            BackgroundRenderTarget = new RenderTarget(Size);
        }

        private void DrawBackgroundBuffer(RenderContext context)
        {
            RenderSettings.ShapeBlendingEnabled = false;

            for (var y = 0; y < TotalRows; y++)
            {
                for (var x = 0; x < TotalColumns; x++)
                {
                    context.Rectangle(
                        ShapeMode.Fill,
                        new Vector2(x * CellWidth, y * CellHeight),
                        CellWidth, CellHeight,
                        _buffer[y * TotalColumns + x].Background
                    );
                }
            }
        }

        private void DrawForegroundBuffer(RenderContext context)
        {
            for (var y = 0; y < TotalRows; y++)
            {
                var start = y * TotalColumns;
                var end = start + TotalColumns;

                var bufferLine = new string(_buffer[start..end].Select(c => c.Character).ToArray());

                var y1 = y;
                var pos = new Vector2(0, (y1 - Margins.Top) * CellHeight);

                context.DrawString(
                    Font,
                    bufferLine,
                    pos,
                    (d, _, i, p) =>
                    {
                        var cell = _buffer[y1 * TotalColumns + i];
                        d.Position = new Vector2(
                            p.X,
                            Margins.Top * CellHeight + p.Y
                        );

                        d.Color = (cell.Blink && !_blinkingCellsVisible)
                            ? Color.Transparent
                            : cell.Foreground;
                    }
                );
            }
        }
    }
}