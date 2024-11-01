using System;
using System.Drawing;
using System.Numerics;
using Chroma.Diagnostics;
using Chroma.Diagnostics.Logging;
using Chroma.Graphics;
using Chroma.Graphics.TextRendering.Bitmap;
using Chroma.Graphics.TextRendering.TrueType;
using Chroma.Input;
using Color = Chroma.Graphics.Color;

namespace Chroma.SabreVGA.TestGame
{
    internal class GameCore : Game
    {
        private TrueTypeFont _ttf;
        private TrueTypeFont _ttf2;
        private BitmapFont _bmf;
        private VgaScreen _vga1;
        private VgaScreen _vga2;

        private Log Log { get; } = LogManager.GetForCurrentAssembly();

        private Color[] _colors = new[]
        {
            Color.Yellow,
            Color.Blue,
            Color.White,
            Color.Aqua,
            Color.HotPink,
            Color.Lime,
            Color.CornflowerBlue
        };

        private Random _rnd = new();

        internal GameCore() : base(new(false, false))
        {
            Window.Mode.SetWindowed(1024, 600, true);
            Graphics.VerticalSyncMode = VerticalSyncMode.None;

            Window.QuitRequested += (_, _) =>
            {
                _vga1.Dispose();
                _vga2.Dispose();
            };
        }

        protected override void LoadContent()
        {
            _ttf = Content.Load<TrueTypeFont>("c64style.ttf", 16);
            _vga1 = new VgaScreen(Vector2.Zero, Window.Size / 4, _ttf, 16, 16);
            _vga1.Cursor.Padding = new Size(0, 1);
            _vga1.Cursor.Shape = CursorShape.Block;
            _vga1.Cursor.Blink = false;
            _vga1.Cursor.Visible = true;
            
            _bmf = Content.Load<BitmapFont>("vga437.fnt");
            _ttf2 = Content.Load<TrueTypeFont>("MODENINE.TTF", 16);
            
            _vga2 = new VgaScreen(new Vector2(
                _vga1.Size.Width + 2,
                0
            ), Window.Size / 4, _ttf2, 8, 16);
            _vga2.ClearToColor(Color.Cyan, Color.Black);
            _vga2.Cursor.Shape = CursorShape.Underscore;
            _vga2.Cursor.AllowMovementOutOfWindow = false;

            DrawFrame(_vga1);
            DrawFrame(_vga2);
        }

        protected override void KeyPressed(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case KeyCode.Up:
                    _vga2.Cursor.Y--;
                    break;
                case KeyCode.Down:
                    _vga2.Cursor.Y++;
                    break;
                case KeyCode.Right:
                    _vga2.Cursor.X++;
                    break;
                case KeyCode.Left:
                    _vga2.Cursor.X--;
                    break;
                case KeyCode.Home:
                    _vga2.Cursor.X = 0;
                    break;
                case KeyCode.End:
                    _vga2.Cursor.X = _vga2.TotalColumns;
                    break;
                case KeyCode.F2:
                    _vga2.Scroll();
                    break;
                case KeyCode.Return:
                    _vga2.Cursor.X = 0;
                    _vga2.Cursor.Y++;
                    break;
            }
        }

        protected override void Update(float delta)
        {
            Window.Title = $"{PerformanceCounter.FPS}";

            var fgColor = _colors[_rnd.Next() % _colors.Length];
            var bgColor = Color.Black;

            var c = '/';

            if (_rnd.Next() % 2 == 0)
                c = '\\';

            _vga1.PutCharAt(_vga1.Cursor.X, _vga1.Cursor.Y, c, fgColor, bgColor, false);

            if (_vga1.Cursor.X++ >= _vga1.WindowColumns)
            {
                _vga1.Cursor.X = 0;

                if (_vga1.Cursor.Y + 1 > _vga1.WindowRows)
                {
                    _vga1.Scroll();
                }
                else
                {
                    _vga1.Cursor.Y++;
                }
            }

            _vga1.Update(delta);
            _vga2.Update(delta);
        }

        protected override void TextInput(TextInputEventArgs e)
        {
            _vga2.PutCharAt(_vga2.Cursor.X++, _vga2.Cursor.Y, e.Text[0]);
        }

        protected override void Draw(RenderContext context)
        {
            _vga1.Draw(context);
            _vga2.Draw(context);
        }

        private void DrawFrame(VgaScreen vga)
        {
            for (var y = 0; y < vga.TotalRows; y++)
            {
                for (var x = 0; x < vga.TotalColumns; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        vga.PutCharAt(x, y, '+');
                    }
                    else if (x == vga.TotalColumns - 1 && y == 0)
                    {
                        vga.PutCharAt(x, y, '+');
                    }
                    else if (x == 0 && y == vga.TotalRows - 1)
                    {
                        vga.PutCharAt(x, y, '+');
                    }
                    else if (x == vga.TotalColumns - 1 && y == vga.TotalRows - 1)
                    {
                        vga.PutCharAt(x, y, '+');
                    }
                    else if (x == 0 || x == vga.TotalColumns - 1)
                    {
                        vga.PutCharAt(x, y, '|');
                    }
                    else if (y == 0 || y == vga.TotalRows - 1)
                    {
                        vga.PutCharAt(x, y, '-');
                    }
                }
            }
        }
    }
}