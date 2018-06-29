using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Hourglass.Input
{
    public class KeyboardTransposer
    {
        public Dictionary<Keys, Keys> Map { get; private set; }

        public static KeyboardTransposer FromScript(string source)
        {
            var transposer = new KeyboardTransposer();

            var parser = new KeyTransposerScriptParser();
            parser.Parse(source);

            foreach (var item in parser.Mapping)
            {
                transposer.Map.Add(item.Key, item.Value);
            }

            return transposer;
        }

        public KeyboardTransposer()
        {
            Map = new Dictionary<Keys, Keys>();
        }

        public KeyboardState TransposeState(KeyboardState input)
        {
            return new KeyboardState(TransposeKeys(input.GetPressedKeys()), input.CapsLock, input.NumLock);
        }

        public Keys[] TransposeKeys(params Keys[] input)
        {
            var output = new Keys[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = TransposeKey(input[i]);
            }
            return output;
        }

        public Keys TransposeKey(Keys input)
        {
            if(Map.ContainsKey(input))
            {
                return Map[input];
            }
            return input;
        }
    }
}
