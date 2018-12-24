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
        private TextInputWatcher _inputWatcher;

        public override bool Enabled { get => _inputWatcher.Enabled; set => _inputWatcher.Enabled = value; }


        public MonoGameInputProvider(InputManager inputManager)
        {
            _inputWatcher = inputManager.AddTextWatcher("terminal", WatcherTextChanged);
            inputManager.KeyPressed += KeyPressed;
        }
        
        public override void ResetTextInput()
        {
            base.ResetTextInput();
            _inputWatcher.ClearInput();
        }

        public override void SetTextInput(string newInput)
        {
            base.SetTextInput(newInput);
            _inputWatcher.SetInput(newInput);
        }

        private void WatcherTextChanged(string newText, TextChangeType changeType)
        {
            base.SetTextInput(newText);
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
