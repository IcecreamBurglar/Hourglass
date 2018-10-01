using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace Example
{
    static class Assets
    {
        public static SpriteFont TerminalFont => _contentManager.Load<SpriteFont>("font");
        public static Texture2D PixelTexture { get; private set; }

        private static ContentManager _contentManager;

        public static void Init(ContentManager contentManager, GraphicsDevice graphicsDevice)
        {
            _contentManager = contentManager;

            PixelTexture = new Texture2D(graphicsDevice, 1, 1);
            PixelTexture.SetData(new [] { Color.White });
        }
    }
}
