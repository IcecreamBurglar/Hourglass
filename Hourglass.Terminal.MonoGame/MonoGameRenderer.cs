using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Hourglass.Terminal.Rendering
{
    internal class MonoGameRenderer : RenderManager
    {
        public override bool Enabled { get; set; }
        public int OutputLines { get; set; }
        public Color BackgroundColor { get; set; }
        public SpriteFont Font { get; private set; }
        public Texture2D Background { get; private set; }
        public SpriteBatch SpriteBatch { get; private set; }

        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                if(_index < 0)
                {
                    _index = 0;
                }
            }
        }

        private float _lineHeight;
        private bool _showCaret;
        private int _caretTime;
        private int _index;

        public MonoGameRenderer(SpriteFont font, Texture2D background, SpriteBatch spriteBatch)
        {
            Font = font;
            Background = background;
            SpriteBatch = spriteBatch;

            OutputLines = 20;
            BackgroundColor = Color.Gray;

            _lineHeight = Font.MeasureString(Environment.NewLine).Y;
            _showCaret = true;
            _caretTime = 0;
        }

        public override int MeasureTextWidth(string text)
        {
            return (int)Font.MeasureString(text).X;
        }

        public void DrawTerminal(Terminal hostTerminal, GameTime gameTime)
        {
            if(!Enabled)
            {
                return;
            }

            _caretTime += gameTime.ElapsedGameTime.Milliseconds;
            if(_caretTime >= 500)
            {
                _showCaret = !_showCaret;
                _caretTime = 0;
            }

            var output = hostTerminal.GetBottomUpOutput(ref _index, OutputLines);
            SpriteBatch.Begin();

            var bounds = new Rectangle(0, 0, hostTerminal.MaxWidth, (int)(_lineHeight / 2 * (output.Length + 1)));
            SpriteBatch.Draw(Background, bounds, BackgroundColor);

            bounds = new Rectangle(0, (int)(_lineHeight / 2 * (output.Length)), hostTerminal.MaxWidth, 3);
            SpriteBatch.Draw(Background, bounds, Color.DarkGoldenrod);


            var nextPos = Vector2.Zero;
            foreach (var item in output)
            {
                var textSize = Font.MeasureString($" {item.Text} ");
                bounds = new Rectangle((int)nextPos.X, (int)nextPos.Y, (int)textSize.X, (int)textSize.Y);

                var backColor = new Color(item.BackColor.R, item.BackColor.G, item.BackColor.B, item.BackColor.A);
                var textcolor = new Color(item.TextColor.R, item.TextColor.G, item.TextColor.B, item.TextColor.A);
                SpriteBatch.Draw(Background, bounds, backColor);
                SpriteBatch.DrawString(Font, item.Text, nextPos, textcolor);

                nextPos += new Vector2(0.0F, _lineHeight / 2);
            }
            nextPos += new Vector2(0.0F, 3.0F);
            
            var inputText = hostTerminal.GetInputText();
            SpriteBatch.DrawString(Font, $"> {inputText}", nextPos, Color.Black);

            
            if(_showCaret)
            {
                if(inputText.Length == 0)
                {
                    nextPos.X = MeasureTextWidth("> ");
                }
                else if(hostTerminal.CaretPosition == inputText.Length)
                {
                    nextPos.X = MeasureTextWidth($"> {inputText.Remove(hostTerminal.CaretPosition - 1)}_");
                }
                else
                {
                    nextPos.X = MeasureTextWidth($"> {inputText.Remove(hostTerminal.CaretPosition)}");
                }
                SpriteBatch.DrawString(Font, "_", nextPos, Color.Black);
            }
            

            SpriteBatch.End();
        }
    }
}