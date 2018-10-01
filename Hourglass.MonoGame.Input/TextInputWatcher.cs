using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hourglass.Input
{
    public delegate void OnTextChanged(string newText, TextChangeType changeType);

    public enum TextChangeType
    {
        Addition,
        Deletion,
    }

    [Flags]
    public enum CapsType
    {
        None,
        CapsLock,
        Shift,
        CapsLockShift,
    }

    public class TextInputWatcher
    {
        public static Dictionary<Keys, TextInputKey> InputKeys { get; private set; }
        
        public OnTextChanged TextChanged;

        public bool Enabled { get; set; }
        public int CaretPosition => _caretPosition;

        private string _input;
        private int _caretPosition;
        private Dictionary<Keys, int> _keyDelays;

        static TextInputWatcher()
        {
            InputKeys = new Dictionary<Keys, TextInputKey>();

            AddLetterInputKey(Keys.Space, ' ');
            AddLetterInputKey(Keys.A, 'a');
            AddLetterInputKey(Keys.B, 'b');
            AddLetterInputKey(Keys.C, 'c');
            AddLetterInputKey(Keys.D, 'd');
            AddLetterInputKey(Keys.E, 'e');
            AddLetterInputKey(Keys.F, 'f');
            AddLetterInputKey(Keys.G, 'g');
            AddLetterInputKey(Keys.H, 'h');
            AddLetterInputKey(Keys.I, 'i');
            AddLetterInputKey(Keys.J, 'j');
            AddLetterInputKey(Keys.K, 'k');
            AddLetterInputKey(Keys.L, 'l');
            AddLetterInputKey(Keys.M, 'm');
            AddLetterInputKey(Keys.N, 'n');
            AddLetterInputKey(Keys.O, 'o');
            AddLetterInputKey(Keys.P, 'p');
            AddLetterInputKey(Keys.Q, 'q');
            AddLetterInputKey(Keys.R, 'r');
            AddLetterInputKey(Keys.S, 's');
            AddLetterInputKey(Keys.T, 't');
            AddLetterInputKey(Keys.U, 'u');
            AddLetterInputKey(Keys.V, 'v');
            AddLetterInputKey(Keys.W, 'w');
            AddLetterInputKey(Keys.X, 'x');
            AddLetterInputKey(Keys.Y, 'y');
            AddLetterInputKey(Keys.Z, 'z');

            AddSymbolInputKey(Keys.D1, '1', '!');
            AddSymbolInputKey(Keys.D2, '2', '@');
            AddSymbolInputKey(Keys.D3, '3', '#');
            AddSymbolInputKey(Keys.D4, '4', '$');
            AddSymbolInputKey(Keys.D5, '5', '%');
            AddSymbolInputKey(Keys.D6, '6', '^');
            AddSymbolInputKey(Keys.D7, '7', '&');
            AddSymbolInputKey(Keys.D8, '8', '*');
            AddSymbolInputKey(Keys.D9, '9', '(');
            AddSymbolInputKey(Keys.D0, '0', ')');

            AddSymbolInputKey(Keys.OemTilde, '`', '~');
            AddSymbolInputKey(Keys.OemMinus, '-', '_');
            AddSymbolInputKey(Keys.OemPlus, '=', '+');
            AddSymbolInputKey(Keys.OemOpenBrackets, '[', '{');
            AddSymbolInputKey(Keys.OemCloseBrackets, ']', '}');
            AddSymbolInputKey(Keys.OemPipe, '\\', '|');
            AddSymbolInputKey(Keys.OemBackslash, '\\', '|');
            AddSymbolInputKey(Keys.OemSemicolon, ';', ':');
            AddSymbolInputKey(Keys.OemQuotes, '\'', '"');
            AddSymbolInputKey(Keys.OemComma, ',', '<');
            AddSymbolInputKey(Keys.OemPeriod, '.', '>');
            AddSymbolInputKey(Keys.OemQuestion, '/', '?');

            AddSymbolInputKey(Keys.OemTilde, '`', '~');

            AddSymbolInputKey(Keys.NumPad1, '1', '1');
            AddSymbolInputKey(Keys.NumPad2, '2', '2');
            AddSymbolInputKey(Keys.NumPad3, '3', '3');
            AddSymbolInputKey(Keys.NumPad4, '4', '4');
            AddSymbolInputKey(Keys.NumPad5, '5', '5');
            AddSymbolInputKey(Keys.NumPad6, '6', '6');
            AddSymbolInputKey(Keys.NumPad7, '7', '7');
            AddSymbolInputKey(Keys.NumPad8, '8', '8');
            AddSymbolInputKey(Keys.NumPad9, '9', '9');
            AddSymbolInputKey(Keys.NumPad0, '0', '0');
        }

        public TextInputWatcher()
        {
            Enabled = true;
            _input = "";
            _caretPosition = 0;
            _keyDelays = new Dictionary<Keys, int>();
        }

        public static void AddLetterInputKey(Keys key, char lower)
        {
            AddInputKey(TextInputKey.CreateLetter(key, lower, char.ToUpper(lower)));
        }

        public static void AddSymbolInputKey(Keys key, char lower, char shift)
        {
            AddInputKey(TextInputKey.CreateSymbol(key, lower, shift));
        }

        public static void AddInputKey(TextInputKey inputKey)
        {
            if(InputKeys.ContainsKey(inputKey.Key))
            {
                InputKeys[inputKey.Key] = inputKey;
            }
            else
            {
                InputKeys.Add(inputKey.Key, inputKey);
            }
        }

        public void ClearInput()
        {
            _input = "";
            _caretPosition = 0;
            _keyDelays.Clear();
        }

        public void SetInput(string input)
        {
            int position = _caretPosition;
            if (position >= input.Length || position >= _input.Length)
            {
                position = input.Length;
            }
            _input = input;
            _caretPosition = position;
            _keyDelays.Clear();
        }

        public void Update(int delta, InputManager inputManager)
        {
            if(!Enabled)
            {
                return;
            }
            CapsType caps = CapsType.None;
            if(inputManager.CurrentKeyboardState.CapsLock)
            {
                caps = CapsType.CapsLock;
            }
            var previousPressedKeys = inputManager.PreviousKeyboardState.GetPressedKeys();
            var pressedKeys = inputManager.CurrentKeyboardState.GetPressedKeys();

            if(pressedKeys.Contains(Keys.LeftShift) || pressedKeys.Contains(Keys.RightShift))
            {
                caps |= CapsType.Shift;
            }

            foreach (var item in pressedKeys)
            {
                if(item == Keys.LeftShift || item == Keys.RightShift)
                {
                    continue;
                }
                
                if(_keyDelays.ContainsKey(item))
                {
                    if(_keyDelays[item] <= 0)
                    {
                        UpdateKey(item, caps);
                        _keyDelays[item] = 5;
                    }
                    else
                    {
                        _keyDelays[item] -= delta;
                    }
                }
                else
                {
                    UpdateKey(item, caps);
                    _keyDelays.Add(item, 500);
                }
            }

            foreach (var item in previousPressedKeys)
            {
                if(!pressedKeys.Contains(item) && _keyDelays.ContainsKey(item))
                {
                    _keyDelays.Remove(item);
                }
            }
        }

        private void UpdateKey(Keys key, CapsType caps)
        {
            if(key == Keys.Left && _caretPosition > 0)
            {
                _caretPosition--;
            }
            if(key == Keys.Right && _caretPosition < _input.Length)
            {
                _caretPosition++;
            }
            if(key == Keys.Back && _caretPosition > 0)
            {
                _input = _input.Remove(_caretPosition - 1, 1);
                _caretPosition--;
                RaiseTextChanged(TextChangeType.Deletion);
                return;
            }
            else if(key == Keys.Delete && _caretPosition < _input.Length)
            {
                _input = _input.Remove(_caretPosition, 1);
                RaiseTextChanged(TextChangeType.Deletion);
                return;
            }
            if(!InputKeys.ContainsKey(key))
            {
                return;
            }
            _input = _input.Insert(_caretPosition, FindChar(key, caps));
            _caretPosition++;
            RaiseTextChanged(TextChangeType.Addition);
        }

        private string FindChar(Keys key, CapsType caps)
        {
            if(InputKeys.ContainsKey(key))
            {
                return InputKeys[key].GetChar(caps).ToString();
            }
            else
            {
                return "";
            }
        }

        private void RaiseTextChanged(TextChangeType changeType)
        {
            TextChanged?.Invoke(_input, changeType);
        }
    }
}
