using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hourglass.Terminal
{
    public struct TerminalOutput
    {
        public string Text { get; private set; }
        public (byte R, byte G, byte B, byte A) TextColor { get; private set; }
        public (byte R, byte G, byte B, byte A) BackColor { get; private set; }

        public TerminalOutput(string text, (byte R, byte G, byte B, byte A) textColor, (byte R, byte G, byte B, byte A) backColor)
        {
            Text = text;
            TextColor = textColor;
            BackColor = backColor;
        }

        public static explicit operator string(TerminalOutput output)
        {
            return output.Text;
        }
    }
}
