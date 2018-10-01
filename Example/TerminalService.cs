
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hourglass.Input;
using Hourglass.Terminal;
using Hourglass.Terminal.Input;
using Hourglass.Terminal.Interpreting;
using Hourglass.Terminal.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Example
{
    class TerminalService
    {
        public static Terminal Terminal { get; private set; }
        public static MonoGameRenderer TerminalRenderer { get; private set; }
        public static CommandInterpreter Interpreter { get; private set; }
        public static TerminalEnvironment Environment { get; private set; }

        private static InputManager _inputManager;
        
        public static void Init(GraphicsDevice graphicsDevice)
        {
            _inputManager = new InputManager();

            Environment = new TerminalEnvironment();
            Interpreter = new CommandInterpreter();
            TerminalRenderer = new MonoGameRenderer(Assets.TerminalFont, Assets.PixelTexture, new SpriteBatch(graphicsDevice));

            Terminal = new Terminal(graphicsDevice.PresentationParameters.BackBufferWidth,
                new MonoGameInputProvider(_inputManager), TerminalRenderer, Interpreter);
            
            Interpreter.SetEnvironment(Environment);
        }

        public static void Update(GameTime gameTime)
        {
            _inputManager.Update(gameTime);
            if (_inputManager.EvaluateKeyStatus(Keys.OemTilde).HasFlag(KeyStatus.Pressed))
            {
                Terminal.Enabled = !Terminal.Enabled;
            }
            if (Terminal.Enabled)
            {
                TerminalRenderer.Index += _inputManager.ScrollChange;
            }
        }

        public static void Draw(GameTime gameTime)
        {
            TerminalRenderer.DrawTerminal(Terminal, gameTime);
        }
    }
}
