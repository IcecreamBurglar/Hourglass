using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hourglass.Terminal.Input
{
    public struct Word
    {
        public static Word Empty => new Word(0, 0, 0, "");

        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }
        public int WordIndex { get; private set; }
        public string Text { get; private set; }

        public int Length => (EndIndex - StartIndex) + 1;

        public Word(int startIndex, int endIndex, int wordIndex, string text)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            WordIndex = wordIndex;
            Text = text;
        }

        public bool ContainsIndex(int index)
        {
            return index >= StartIndex && index <= EndIndex;
        }

        public bool IsEmpty()
        {
            return StartIndex == 0 && EndIndex == 0 && WordIndex == 0 && Text == "";
        }
    }
}
