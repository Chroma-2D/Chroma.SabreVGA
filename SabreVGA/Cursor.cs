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

        public CursorShape Shape { get; set; } = CursorShape.Block;
        public Vector2 Offset { get; set; }
        public Size Padding { get; set; }

        public Cursor(VgaScreen vgaScreen)
        {
            _x = 0;
            _y = 0;

            VgaScreen = vgaScreen;
        }

        public void Draw(RenderContext context)
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
                            ) + Offset,
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
                            ) + Offset,
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
                            ) + Offset,
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

        public void Reset()
        {
            _timer = 0;
            _visible = true;
        }

        public void Update(float delta)
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
            if (_x >= VgaScreen.TotalColumns - VgaScreen.Margins.Right - 1)
                _x = VgaScreen.TotalColumns - VgaScreen.Margins.Right - 1;
            else if (_x < VgaScreen.Margins.Left)
                _x = VgaScreen.Margins.Left;

            if (_y >= VgaScreen.TotalRows - VgaScreen.Margins.Bottom - 1)
                _y = VgaScreen.TotalRows - VgaScreen.Margins.Bottom - 1;
            else if (_y < VgaScreen.Margins.Top)
                _y = VgaScreen.Margins.Top;
        }
    }
}