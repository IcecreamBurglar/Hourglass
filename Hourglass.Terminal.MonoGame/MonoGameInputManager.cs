using System.Linq;
using Hourglass.Input;
using Microsoft.Xna.Framework.Input;

namespace Hourglass.Terminal.Input
{
    class MonoGameInputProvider : TerminalInputProvider
    {
        private string _input;
        private TextInputWatcher _inputWatcher;

        public override bool Enabled { get => _inputWatcher.Enabled; set => _inputWatcher.Enabled = value; }


        public MonoGameInputProvider(InputManager inputManager)
        {
            _input = "";
            _inputWatcher = inputManager.AddTextWatcher("terminal", WatcherTextChanged);
            inputManager.KeyPressed += KeyPressed;
        }

        public override string GetLastWord()
        {
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

        public override void ResetTextInput()
        {
            _input = "";
            _inputWatcher.ClearInput();
        }

        public override void SetLastWord(string newWord)
        {
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
        }

        public override void SetTextInput(string newInput)
        {
            _input = newInput;
            _inputWatcher.SetInput(newInput);
            RaiseTextChanged(ref _input);
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
    }
}
