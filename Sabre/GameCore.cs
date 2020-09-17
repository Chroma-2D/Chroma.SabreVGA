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
        private int _direction = 1;
        private bool _initComplete;

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
            Window.GoWindowed(new Size(1024, 640), true);
            FixedUpdateFrequency = 90;
        }

        protected override void LoadContent()
        {
            _ttf = Content.Load<TrueTypeFont>("Nouveau_IBM.ttf", 16);
            _ttf.HintingMode = HintingMode.Normal;
            _vga = new VgaScreen(Window, _ttf, 9, 16);

            _vga.Cursor.Offset = new Vector2(0, -1);
            _vga.Cursor.Padding = new Size(0, 1);
            _vga.Cursor.Shape = CursorShape.Block;

            _initComplete = true;
        }

        protected override void TextInput(TextInputEventArgs e)
        {
            var fgColor = _colors[_rnd.Next() % _colors.Length];
            _vga.PutCharAt(e.Text[0], fgColor, _vga.Cursor.X, _vga.Cursor.Y);

            if (_vga.Cursor.X >= _vga.WindowColumns || _vga.Cursor.X == _vga.Margins.Left)
                _direction = -_direction;

            _vga.Cursor.X += _direction;
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

            _vga.Update(delta);
        }

        protected override void FixedUpdate(float fixedDelta)
        {
            _vga.ScrollUp();
        }

        protected override void Draw(RenderContext context)
        {
            _vga.Draw(context);
        }
    }
}