using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hourglass.Terminal.Input
{
    public delegate void OnTextInputChanged(ref string textInput);

    public delegate void OnHistoryPrevious();
    public delegate void OnHistoryNext();

    public delegate void OnAutoComplete();

    public delegate void OnExecuteInput();

    public abstract class TerminalInputProvider
    {
        public static TerminalInputProvider Default { get; set; }

        public event OnTextInputChanged TextChanged;
        public event OnHistoryPrevious HistoryPrevious;
        public event OnHistoryNext HistoryNext;
        public event OnAutoComplete AutoComplete;
        public event OnExecuteInput ExecuteInput;

        public abstract bool Enabled { get; set; }
        
        public abstract int GetCaretPosition();


        protected string _input;
        protected Word[] _wordCache;
        protected bool _inputChanged;

        protected TerminalInputProvider()
        {
            _input = "";
            _wordCache = new Word[0];
            _inputChanged = false;
        }

        public virtual void SetTextInput(string newInput)
        {
            _input = newInput;
            _inputChanged = true;
            RaiseTextChanged(ref _input);
        }

        public virtual void ResetTextInput()
        {
            SetTextInput("");
        }

        public virtual Word GetFirstWord()
        {
            var words = EnumerateWords();
            if (words == null || words.Length == 0)
            {
                return Word.Empty;
            }
            return EnumerateWords()[0];
        }

        public virtual Word GetCurrentWord()
        {
            return FindCurrentWord(GetCaretPosition());
        }

        public virtual int GetCurrentWordPosition()
        {
            return FindCurrentWord(GetCaretPosition()).WordIndex;
        }

        protected virtual Word FindCurrentWord(int caretPosition)
        {
            var words = EnumerateWords();
            if (caretPosition >= _input.Length - 1 && words.Length > 0)
            {
                //  return words.Last();
            }

            var leftNeighbor = Word.Empty;
            for (int i = 0; i < words.Length; i++)
            {
                var curWord = words[i];
                if (curWord.ContainsIndex(caretPosition))
                {
                    return curWord;
                }
                else if (curWord.StartIndex > caretPosition)
                {
                    //return curWord;
                }
                if (curWord.EndIndex < caretPosition)
                {
                    leftNeighbor = curWord;
                }
            }

            if (_input.LastOrDefault() == ' ')
            {
                return new Word(caretPosition, caretPosition, words.Length + 1, "");
            }
            return leftNeighbor;
        }


        public virtual void SetCurrentWord(string newWord)
        {
            var word = FindCurrentWord(GetCaretPosition());

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
        }

        private string CutString(string input, int startIndex, int endIndex)
        {
            if (startIndex >= input.Length)
            {
                return input;
            }
            if (endIndex >= input.Length - 2)
            {
                return input.Remove(startIndex);
            }
            return input.Remove(startIndex) + input.Substring(endIndex + 1);
        }

        protected virtual Word[] EnumerateWords()
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

        protected void RaiseTextChanged(ref string textInput)
        {
            TextChanged?.Invoke(ref textInput);
        }

        protected void RaiseHistoryPrevious()
        {
            HistoryPrevious?.Invoke();
        }

        protected void RaiseHistoryNext()
        {
            HistoryNext?.Invoke();
        }

        protected void RaiseAutoComplete()
        {
            AutoComplete?.Invoke();
        }

        protected void RaiseExecuteInput()
        {
            ExecuteInput?.Invoke();
        }

    }
}
