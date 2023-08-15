using System.Drawing;
using System.Numerics;
using Chroma.Graphics;
using Color = Chroma.Graphics.Color;

namespace Chroma.SabreVGA
{
    public class Cursor
    {
        private int _timer;
        private bool _show;

        private int _x;
        private int _y;

        private VgaScreen VgaScreen { get; }

        public int X
        {
            get
            {
                EnsureInBounds();
                return _x;
            }

            set
            {
                _x = value;
                EnsureInBounds();
            }
        }

        public int Y
        {
            get
            {
                EnsureInBounds();
                return _y;
            }

            set
            {
                _y = value;
                EnsureInBounds();
            }
        }

        public int BlinkInterval { get; set; } = 225;

        public bool Blink { get; set; } = true;
        public bool IsVisible { get; set; } = true;

        public Color Color { get; set; } = Color.White;
        public bool AllowMovementOutOfWindow { get; set; }

        public CursorShape Shape { get; set; } = CursorShape.Block;
        public Vector2 Offset { get; set; }
        public Size Padding { get; set; }

        public void Reset()
        {
            _timer = 0;
            _x = 0;
            _y = 0;
        }

        internal Cursor(VgaScreen vgaScreen)
        {
            VgaScreen = vgaScreen;
        }

        internal void Draw(RenderContext context)
        {
            if (!IsVisible)
                return;
            
            if (_show)
            {
                RenderSettings.ShapeBlendingEnabled = true;
                RenderSettings.SetShapeBlendingFunctions(
                    BlendingFunction.OneMinusDestinationColor,
                    BlendingFunction.OneMinusDestinationAlpha,
                    BlendingFunction.Zero,
                    BlendingFunction.One
                );
                
                switch (Shape)
                {
                    case CursorShape.Block:
                    {
                        DrawBlock(context);
                        break;
                    }

                    case CursorShape.Pipe:
                    {
                        DrawPipe(context);
                        break;
                    }

                    case CursorShape.Underscore:
                    {
                        DrawUnderscore(context);
                        break;
                    }
                }
            }
        }

        internal void Update(float delta)
        {
            if (Blink)
            {
                if (_timer >= BlinkInterval)
                {
                    _show = !_show;
                    _timer = 0;

                    return;
                }

                _timer += (int)(1000 * delta);
            }
        }

        private void DrawBlock(RenderContext context)
        {           
            context.Rectangle(
                ShapeMode.Fill,
                new Vector2(
                    X * VgaScreen.CellWidth,
                    Y * VgaScreen.CellHeight
                ) + Offset + VgaScreen.Position,
                new Size(
                    VgaScreen.CellWidth,
                    VgaScreen.CellHeight
                ) + Padding,
                Color
            );
        }

        private void DrawPipe(RenderContext context)
        {
            context.Rectangle(
                ShapeMode.Fill,
                new Vector2(
                    X * VgaScreen.CellWidth,
                    Y * VgaScreen.CellHeight
                ) + Offset + VgaScreen.Position,
                new Size(
                    1,
                    VgaScreen.CellHeight
                ) + Padding,
                Color
            );
        }

        private void DrawUnderscore(RenderContext context)
        {
            context.Rectangle(
                ShapeMode.Fill,
                new Vector2(
                    X * VgaScreen.CellWidth,
                    Y * VgaScreen.CellHeight + VgaScreen.CellHeight - 2
                ) + Offset + VgaScreen.Position,
                new Size(
                    VgaScreen.CellWidth,
                    2
                ) + Padding,
                Color
            );
        }

        private void EnsureInBounds()
        {
            var leftMostBounds = 0;
            var topMostBounds = 0;
            var rightMostBounds = VgaScreen.TotalColumns - 1;
            var bottomMostBounds = VgaScreen.TotalRows - 1;

            if (!AllowMovementOutOfWindow)
            {
                leftMostBounds = VgaScreen.Margins.Left;
                rightMostBounds -= VgaScreen.Margins.Right;
                topMostBounds = VgaScreen.Margins.Top;
                bottomMostBounds -= VgaScreen.Margins.Bottom;
            }

            if (_x > rightMostBounds)
                _x = rightMostBounds;
            else if (_x < leftMostBounds)
                _x = leftMostBounds;

            if (_y > bottomMostBounds)
                _y = bottomMostBounds;
            else if (_y < topMostBounds)
                _y = topMostBounds;
        }
    }
}