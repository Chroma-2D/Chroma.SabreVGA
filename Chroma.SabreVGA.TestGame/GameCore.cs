using System;
using System.Drawing;
using System.Numerics;
using Chroma;
using Chroma.Diagnostics.Logging;
using Chroma.Graphics;
using Chroma.Graphics.TextRendering;
using Chroma.Input;
using Chroma.Input.EventArgs;
using SabreVGA;
using Color = Chroma.Graphics.Color;

namespace Chroma.SabreVGA.TestGame
{
    internal class GameCore : Game
    {
        private TrueTypeFont _ttf;
        private TrueTypeFont _ttf2;
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

        private Random _rnd = new Random();

        internal GameCore()
        {
            Window.GoWindowed(new Size(1640, 800), true);
            Graphics.VerticalSyncMode = VerticalSyncMode.None;
            
            Window.QuitRequested += (sender, args) =>
            {
                _vga1.Dispose();
                _vga2.Dispose();
            };
        }

        protected override void LoadContent()
        {
            _ttf = Content.Load<TrueTypeFont>("c64style.ttf", 16);            
            _vga1 = new VgaScreen(Vector2.Zero, Window.Size / 4, _ttf, 16, 16);
            _vga1.Cursor.Offset = new Vector2(0, -1);
            _vga1.Cursor.Padding = new Size(0, 1);
            _vga1.Cursor.Shape = CursorShape.Block;
            
            _ttf2 = Content.Load<TrueTypeFont>("Nouveau_IBM.ttf", 16);
            _vga2 = new VgaScreen(new Vector2(
                _vga1.Size.Width + 2,
                0
            ), Window.Size / 4, _ttf2, 9, 16);
            _vga2.Cursor.Offset = new Vector2(0, -1);
            _vga2.Cursor.Shape = CursorShape.Underscore;
            _vga2.Cursor.AllowMovementOutOfWindow = true;
            
            DrawFrame(_vga1);
            DrawFrame(_vga2);
        }

        protected override void KeyPressed(KeyEventArgs e)
        {
            if (e.KeyCode == KeyCode.Up)
                _vga2.Cursor.Y--;
            else if (e.KeyCode == KeyCode.Down)
                _vga2.Cursor.Y++;
            else if (e.KeyCode == KeyCode.Right)
                _vga2.Cursor.X++;
            else if (e.KeyCode == KeyCode.Left)
                _vga2.Cursor.X--;
            else if (e.KeyCode == KeyCode.Home)
                _vga2.Cursor.X = 0;
            else if (e.KeyCode == KeyCode.End)
                _vga2.Cursor.X = _vga2.TotalColumns;
            else if (e.KeyCode == KeyCode.F1)
                _vga2.Clear();
            else if (e.KeyCode == KeyCode.F2)
                _vga2.ScrollUp();
            else if (e.KeyCode == KeyCode.Return)
            {
                _vga2.Cursor.X = 0;
                _vga2.Cursor.Y++;
            }
        }

        protected override void Update(float delta)
        {
            Window.Title = $"{Window.FPS}";

            var fgColor = _colors[_rnd.Next() % _colors.Length];
            var bgColor = _colors[_rnd.Next() % _colors.Length];

            var c = '/';

            if (_rnd.Next() % 4 == 0)
                c = '\\';
            
            _vga1.PutCharAt(c, _vga1.Cursor.X, _vga1.Cursor.Y, fgColor, bgColor, _rnd.Next() % 5 == 0);

            if (_vga1.Cursor.X++ >= _vga1.WindowColumns)
            {
                _vga1.Cursor.X = 0;

                if (_vga1.Cursor.Y + 1 > _vga1.WindowRows)
                {
                    _vga1.ScrollUp();
                }

                else
                    _vga1.Cursor.Y++;
            }
            
            _vga1.Update(delta);
            _vga2.Update(delta);
        }

        protected override void TextInput(TextInputEventArgs e)
        {
            _vga2.PutCharAt(e.Text[0], _vga2.Cursor.X++, _vga2.Cursor.Y);
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
                        vga.PutCharAt('+', x, y);
                    }
                    else if (x == vga.TotalColumns - 1 && y == 0)
                    {
                        vga.PutCharAt('+', x, y);
                    }
                    else if (x == 0 && y == vga.TotalRows - 1)
                    {
                        vga.PutCharAt('+', x, y);
                    }
                    else if (x == vga.TotalColumns - 1 && y == vga.TotalRows - 1)
                    {
                        vga.PutCharAt('+', x, y);
                    }
                    else if (x == 0 || x == vga.TotalColumns - 1)
                    {
                        vga.PutCharAt('|', x, y);
                    }
                    else if (y == 0 || y == vga.TotalRows - 1)
                    {
                        vga.PutCharAt('-', x, y);
                    }
                }
            }
        }
    }
}