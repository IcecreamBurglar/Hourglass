using System;
using System.Collections.Generic;
using System.Text;

namespace Hourglass.Terminal.Rendering
{
    public abstract class RenderManager
    {
        public static RenderManager Default { get; set; }

        public abstract bool Enabled { get; set; }
        public abstract int MeasureTextWidth(string text);
    }
}
