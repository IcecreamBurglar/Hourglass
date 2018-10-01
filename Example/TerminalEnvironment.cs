using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hourglass.Terminal.Interpreting;
using Microsoft.Xna.Framework;

namespace Example
{
    class TerminalEnvironment
    {
        [EnvironmentContextualCompletionResolver]
        public string[] Resolve(string command, int argIndex)
        {
            if (command == "Greet")
            {
                if (argIndex == 2)
                {
                    return new[] {"Bob", "John", "Geraldo", "Anne"};
                }
            }
            else if (command == "SetColor")
            {

                if (argIndex == 2)
                {
                    return new[] {"Red", "Green", "Blue"};
                }
            }

            return new []  {Interpreter.DEFER_RESOLUTION};
        }

        [EnvironmentItem]
        private void Greet(string who)
        {
            TerminalService.Terminal.Write($"Hello, {who}!");
        }

        [EnvironmentItem]
        private void SetColor(string colorName)
        {
            Color color = Color.CornflowerBlue;
            if (colorName == "Red")
            {
                color = Color.Red;
            }
            else if (colorName == "Green")
            {
                color = Color.Green;
            }
            else if (colorName == "Blue")
            {
                color = Color.Blue;
            }
            Game1.Instance.ClearColor = color;
        }
    }
}
