using System;
using System.Collections.Generic;
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

        public abstract void ResetTextInput();

        public abstract void SetTextInput(string newInput);
        public abstract void SetLastWord(string newWord);
        public abstract string GetLastWord();
        public abstract int GetCaretPosition();

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
