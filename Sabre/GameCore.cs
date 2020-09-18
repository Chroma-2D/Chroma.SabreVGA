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

namespace Sabre
{
    internal class GameCore : Game
    {
        private TrueTypeFont _ttf;
        private VgaScreen _vga;

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
        }

        protected override void LoadContent()
        {
            _ttf = Content.Load<TrueTypeFont>("c64style.ttf", 16);
            _ttf.HintingMode = HintingMode.Normal;
            _vga = new VgaScreen(Window, _ttf, 16, 16);

            _vga.Cursor.Offset = new Vector2(0, -1);
            _vga.Cursor.Padding = new Size(0, 1);
            _vga.Cursor.Shape = CursorShape.Block;

            for (var y = 0; y < _vga.TotalRows; y++)
            {
                for (var x = 0; x < _vga.TotalColumns; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        _vga.PutCharAt('+', x, y);
                    }
                    else if (x == _vga.TotalColumns - 1 && y == 0)
                    {
                        _vga.PutCharAt('+', x, y);
                    }
                    else if (x == 0 && y == _vga.TotalRows - 1)
                    {
                        _vga.PutCharAt('+', x, y);
                    }
                    else if (x == _vga.TotalColumns - 1 && y == _vga.TotalRows - 1)
                    {
                        _vga.PutCharAt('+', x, y);
                    }
                    else if (x == 0 || x == _vga.TotalColumns - 1)
                    {
                        _vga.PutCharAt('|', x, y);
                    }
                    else if (y == 0 || y == _vga.TotalRows - 1)
                    {
                        _vga.PutCharAt('-', x, y);
                    }
                }
            }
        }

        protected override void TextInput(TextInputEventArgs e)
        {
        }

        protected override void KeyPressed(KeyEventArgs e)
        {
            if (e.KeyCode == KeyCode.Up)
                _vga.Cursor.Y--;
            else if (e.KeyCode == KeyCode.Down)
                _vga.Cursor.Y++;
            else if (e.KeyCode == KeyCode.Right)
                _vga.Cursor.X++;
            else if (e.KeyCode == KeyCode.Left)
                _vga.Cursor.X--;
            else if (e.KeyCode == KeyCode.Home)
                _vga.Cursor.X = 0;
            else if (e.KeyCode == KeyCode.End)
                _vga.Cursor.X = _vga.TotalColumns;
            else if (e.KeyCode == KeyCode.F1)
                _vga.ClearScreen(false);
            else if (e.KeyCode == KeyCode.F2)
                _vga.ScrollUp();
            else if (e.KeyCode == KeyCode.Return)
            {
                _vga.Cursor.X = 0;
                _vga.Cursor.Y++;
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
            
            _vga.PutCharAt(c, _vga.Cursor.X, _vga.Cursor.Y, fgColor, Color.Green, _rnd.Next() % 5 == 0);

            if (_vga.Cursor.X++ >= _vga.WindowColumns)
            {
                _vga.Cursor.X = 0;

                if (_vga.Cursor.Y + 1 > _vga.WindowRows)
                {
                    _vga.ScrollUp();
                }

                else
                    _vga.Cursor.Y++;
            }
            
            _vga.Update(delta);
        }

        protected override void FixedUpdate(float fixedDelta)
        {
        }

        protected override void Draw(RenderContext context)
        {
            _vga.Draw(context);
        }
    }
}