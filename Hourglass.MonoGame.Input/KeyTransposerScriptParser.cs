using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hourglass.Input
{
    public class KeyTransposerScriptParser
    {
        public KeyValuePair<Keys, Keys>[] Mapping => _mapping.ToArray();

        private Dictionary<Keys, Keys> _mapping;

        public KeyTransposerScriptParser()
        {
            _mapping = new Dictionary<Keys, Keys>();
        }

        public void Load(string file)
        {
            Parse(System.IO.File.ReadAllText(file));
        }

        public void Parse(string code)
        {
            var mapping = new Dictionary<Keys, Keys>();

            string[] lines = code.Split('\n');
            
            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    ParseLine(lines[i]);
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void ParseLine(string line)
        {
            if(line.StartsWith("##"))
            {
                return;
            }

            bool parsingSecond = false;
            string firstKeyName = "";
            string secondKeyName = "";

            for (int i = 0; i < line.Length; i++)
            {
                char curChar = line[i];
                
                if(curChar == ' ')
                {
                    continue;
                }
                else if(curChar == '-')
                {
                    if(i + 1 < line.Length)
                    {
                        if(line[i + 1] == '>')
                        {
                            if (parsingSecond)
                            {
                                throw new InvalidOperationException("Invalid key mapping format. Key mapping must be in the form '[key]->[key]'.");
                            }
                            parsingSecond = true;
                            i++;
                            continue;
                        }
                    }
                }
                else if(!parsingSecond)
                {
                    firstKeyName += curChar;
                }
                else if(parsingSecond)
                {
                    secondKeyName += curChar;
                }
            }

            ParseKeys(firstKeyName, secondKeyName);
        }

        private void ParseKeys(string first, string second)
        {
            if(!Enum.TryParse(first, out Keys firstKey))
            {
                throw new InvalidOperationException($"The string '{first}' is not a valid key.");
            }
            if (!Enum.TryParse(second, out Keys secondKey))
            {
                throw new InvalidOperationException($"The string '{second}' is not a valid key.");
            }

            _mapping.Add(firstKey, secondKey);
        }
    }
}
