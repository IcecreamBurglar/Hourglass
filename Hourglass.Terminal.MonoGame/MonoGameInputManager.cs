using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Hourglass.Input;
using Microsoft.Xna.Framework.Input;

namespace Hourglass.Terminal.Input
{
    //NOTE:
    /*
     * If I ever decide to implement caret changing,
     * it'll mess-up the current auto-completion implementation.
     * This can be redone using a 'Word' struct, containing
     * the following data:
     *   + startindex
     *       The index in the input this word starts at.
     *   + endindex
     *       The index in the input this word ends at.
     *   + wordposition
     *       The index of this word.
     *   + word
     *       The actual text of this word.
     *
     */
    public class MonoGameInputProvider : TerminalInputProvider
    {
        private string _input;
        private bool _inputChanged;
        private TextInputWatcher _inputWatcher;
        private Word[] _wordCache;

        public override bool Enabled { get => _inputWatcher.Enabled; set => _inputWatcher.Enabled = value; }


        public MonoGameInputProvider(InputManager inputManager)
        {
            _input = "";
            _inputWatcher = inputManager.AddTextWatcher("terminal", WatcherTextChanged);
            inputManager.KeyPressed += KeyPressed;
        }

        public override string GetFirstWord()
        {
            var words = EnumerateWords();
            if (words == null || words.Length == 0)
            {
                return "";
            }
            return EnumerateWords()[0].Text;
        }

        public override string GetLastWord()
        {
            return FindCurrentWord().Text;
            var words = EnumerateWords();

            for (int i = 0; i < words.Length; i++)
            {
                var curWord = words[i];
                if (curWord.ContainsIndex(_inputWatcher.CaretPosition))
                {

                }
            }

            var word = "";
            for (int i = 0; i < _input.Length; i++)
            {
                if(_input[i] == ' ')
                {
                    word = "";
                    continue;
                }
                word += _input[i];
            }
            return word;
        }

        public override int GetLastWordPosition()
        {
            return FindCurrentWord().WordIndex;
            int wordPosition = 0;

            for (int i = 0; i < _input.Length; i++)
            {
                if (_input[i] == ' ')
                {
                    wordPosition++;
                }
            }

            return wordPosition;
        }

        public override void ResetTextInput()
        {
            _input = "";
            _inputChanged = true;
            _inputWatcher.ClearInput();
        }

        public override void SetLastWord(string newWord)
        {
            var word = FindCurrentWord();
            
            _input = CutString(_input, word.StartIndex, word.EndIndex);
            if (word.StartIndex >= _input.Length - 1)
            {
                _input += newWord;
            }
            else
            {
                _input = _input.Insert(word.StartIndex, newWord);
            }
            SetTextInput(_input);

            /*
            int lastWordIndex = 0;
            if(_input.Contains(' '))
            {
                lastWordIndex = _input.LastIndexOf(' ') + 1;
            }
            if (lastWordIndex >= _input.Length)
            {
                _input += newWord;
            }
            else
            {
                _input = _input.Remove(lastWordIndex) + newWord;
            }
            SetTextInput(_input);
            */
        }

        public override void SetTextInput(string newInput)
        {
            _input = newInput;
            _inputChanged = true;
            _inputWatcher.SetInput(newInput);
            RaiseTextChanged(ref _input);
        }

        private Word FindCurrentWord()
        {
            var words = EnumerateWords();
            if (_inputWatcher.CaretPosition >= _input.Length - 1 && words.Length > 0)
            {
                return words.Last();
            }

            for (int i = 0; i < words.Length; i++)
            {
                var curWord = words[i];
                if (curWord.ContainsIndex(_inputWatcher.CaretPosition))
                {
                    return curWord;
                }
            }

            return Word.Empty;
        }

        private Word[] EnumerateWords()
        {
            if (_inputChanged == false && _wordCache != null)
            {
                return _wordCache;
            }

            List<Word> words = new List<Word>();
            int wordCount = 0;
            int wordStart = -1;
            bool quotes = false;
            bool lastWasSpace = true;
            string curWord = "";

            void AddWord(int curIndex, bool addEmpty = false)
            {
                if (!string.IsNullOrWhiteSpace(curWord) || addEmpty)
                {
                    wordCount++;
                    words.Add(new Word(wordStart, curIndex, wordCount, curWord));
                    wordStart = -1;
                    curWord = "";
                }
            }

            var input = _input + " ";

            for (int i = 0; i < input.Length; i++)
            {
                var curChar = input[i];
                if (curChar == '"')
                {
                    quotes = !quotes;
                    continue;
                }

                if (curChar == '\\')
                {
                    if (i + i < input.Length && input[i + 1] == '\"')
                    {
                        if (input[i + 1] == '\"')
                        {
                            curWord += "\"";
                            i++;
                            continue;
                        }
                    }

                    curWord += "\\";
                }

                if (curChar == ' ' && !quotes && !lastWasSpace)
                {
                    lastWasSpace = true;
                    AddWord(i - 1);
                    continue;
                }
                else if (curChar == ' ' && !quotes && lastWasSpace)
                {
                    //Skip repeated spaces
                    continue;
                }
                else if (curChar != ' ' && lastWasSpace)
                {
                    lastWasSpace = false;
                    wordStart = i;
                }

                curWord += curChar;
            }

            //TODO: Add edge-case where the desired arg has no text and the cursor is at the end of the string.
            //like in
            //"Greet "
            //       ^
            AddWord(input.Length - 1, false);
            _inputChanged = false;
            _wordCache = words.ToArray();
            return _wordCache;
        }

        private void WatcherTextChanged(string newText, TextChangeType changeType)
        {
            _input = newText;
            RaiseTextChanged(ref _input);
        }

        private void KeyPressed(Keys key, KeyStatus status)
        {
            if(key == Keys.Enter)
            {
                RaiseExecuteInput();
            }
            else if(key == Keys.Up)
            {
                RaiseHistoryNext();
            }
            else if (key == Keys.Down)
            {
                RaiseHistoryPrevious();
            }
            else if (key == Keys.Tab)
            {
                RaiseAutoComplete();
            }
        }

        public override int GetCaretPosition()
        {
            return _inputWatcher.CaretPosition;
        }

        private string CutString(string input, int startIndex, int endIndex)
        {
            if (startIndex >= input.Length)
            {
                return input;
            }
            if (endIndex >= input.Length - 1)
            {
                return input.Remove(startIndex);
            }
            return input.Remove(startIndex) + input.Substring(endIndex);
        }

        private struct Word
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
                return StartIndex == -1 && EndIndex == -1 && WordIndex == -1 && Text == null;
            }
        }
    }
}
