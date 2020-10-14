using System.Drawing;
using System.Numerics;
using Chroma.Graphics;
using Color = Chroma.Graphics.Color;

namespace SabreVGA
{
    public class Cursor
    {
        private bool _visible;
        private int _timer;

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

        public int BlinkInterval { get; set; } = 250;
        public Color Color { get; set; } = Color.White;
        public bool ForceVisible { get; set; }
        public bool ForceHidden { get; set; }
        public bool AllowMovementOutOfWindow { get; set; }

        public CursorShape Shape { get; set; } = CursorShape.Block;
        public Vector2 Offset { get; set; }
        public Size Padding { get; set; }
        
        public void ResetState()
        {
            _timer = 0;
            _visible = true;
        }

        internal Cursor(VgaScreen vgaScreen)
        {
            _x = 0;
            _y = 0;

            VgaScreen = vgaScreen;
        }

        internal void Draw(RenderContext context)
        {
            if (_visible)
            {
                switch (Shape)
                {
                    case CursorShape.Block:
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
                        break;

                    case CursorShape.Pipe:
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
                        break;

                    case CursorShape.Underscore:
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
                        break;
                }
            }
        }

        internal void Update(float delta)
        {
            if (ForceHidden)
            {
                _visible = false;
                return;
            }

            if (ForceVisible)
            {
                _visible = true;
                return;
            }

            if (_timer >= BlinkInterval)
            {
                _visible = !_visible;
                _timer = 0;

                return;
            }

            _timer += (int)(1000 * delta);
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