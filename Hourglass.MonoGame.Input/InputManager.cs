using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hourglass.Input
{
    [Flags]
    public enum KeyStatus
    {
        Up = 0,
        Down = 1,
        Pressed = 2,
        PressedDown = Down | Pressed,
        Released = 4,
        ReleasedDown = Up | Released
    }

    public delegate void OnKeyEvent(Keys key, KeyStatus status);

    public class InputManager
    {
        public event OnKeyEvent KeyPressed;
        public event OnKeyEvent KeyReleased;

        public Dictionary<string, TextInputWatcher> TextWatchers { get; }

        public Dictionary<InputActions, IInputController> InputMap { get; }

        public KeyboardTransposer KeyboardTransposer { get; set; }
        public KeyboardState PreviousKeyboardState { get; private set; }
        public KeyboardState CurrentKeyboardState { get; private set; }

        public MouseState PreviousMouseState { get; private set; }
        public MouseState CurrentMouseState { get; private set; }
        public int ScrollDelta => CurrentMouseState.ScrollWheelValue - PreviousMouseState.ScrollWheelValue;
        public int ScrollChange => Comparer<int>.Default.Compare(CurrentMouseState.ScrollWheelValue, PreviousMouseState.ScrollWheelValue);

        public GamePadState PreviousGamePad { get; private set; }
        public GamePadState CurrentGamePad { get; private set; }

        public InputManager()
        {
            TextWatchers = new Dictionary<string, TextInputWatcher>();
            KeyboardTransposer = new KeyboardTransposer();
            InputMap = new Dictionary<InputActions, IInputController>();

            TextWatchers.Add("glob", new TextInputWatcher());
        }

        public TextInputWatcher AddTextWatcher(string name, OnTextChanged textChangedCallback)
        {
            if(TextWatchers.ContainsKey(name))
            {
                //TODO: Error
            }
            var inputWatcher = new TextInputWatcher();
            inputWatcher.TextChanged += textChangedCallback;

            TextWatchers.Add(name, inputWatcher);
            
            return inputWatcher;
        }

        public void DestroyTextWatcher(string name)
        {
            if (!TextWatchers.ContainsKey(name))
            {
                //TODO: Error
            }
            var watcher = TextWatchers[name];
            TextWatchers.Remove(name);

            Delegate[] clientList = watcher.TextChanged.GetInvocationList();
            foreach (var callback in clientList)
            {
                watcher.TextChanged -= (OnTextChanged)callback;
            }
        }

        public void SubscribeToGlobalTextWatcher(OnTextChanged callback)
        {
            SubscribeToTextWatcher("glob", callback);
        }

        public void SubscribeToTextWatcher(string textWatcher, OnTextChanged callback)
        {
            if (!TextWatchers.ContainsKey(textWatcher))
            {
                //TODO: Error
            }
            TextWatchers[textWatcher].TextChanged += callback;
        }

        public void UnsubscribeFromGlobalTextWatcher(OnTextChanged callback)
        {
            UnsubscribeFromTextWatcher("glob", callback);
        }

        public void UnsubscribeFromTextWatcher(string textWatcher, OnTextChanged callback)
        {
            if (!TextWatchers.ContainsKey(textWatcher))
            {
                //TODO: Error
            }

            TextWatchers[textWatcher].TextChanged -= callback;
        }

        public KeyStatus EvaluateKeyStatus(Keys key)
        {
            KeyStatus status = KeyStatus.Up;
            if (CurrentKeyboardState.IsKeyDown(key))
            {
                status = KeyStatus.Down;
                if (PreviousKeyboardState.IsKeyUp(key))
                {
                    status |= KeyStatus.Pressed;
                }
            }
            else if (PreviousKeyboardState.IsKeyDown(key))
            {
                status |= KeyStatus.Released;
            }
            return status;
        }

        public void Update(GameTime gameTime)
        {
            PreviousKeyboardState = CurrentKeyboardState;
            PreviousMouseState = CurrentMouseState;
            PreviousGamePad = CurrentGamePad;

            CurrentKeyboardState = KeyboardTransposer.TransposeState(Keyboard.GetState());
            CurrentMouseState = Mouse.GetState();

            if (GamePad.MaximumGamePadCount > 0)
            {
                CurrentGamePad = GamePad.GetState(0);
            }

            foreach (var controller in InputMap.Values)
            {
                controller.Poll(this);
            }

            foreach (var item in TextWatchers)
            {
                item.Value.Update(gameTime.ElapsedGameTime.Milliseconds, this);
            }

            EvaluateKeyChanges();
        }

        private void EvaluateKeyChanges()
        {
            var downKeys = CurrentKeyboardState.GetPressedKeys();
            var previousDownKeys = PreviousKeyboardState.GetPressedKeys();

            foreach (var item in downKeys)
            {
                if(!previousDownKeys.Contains(item))
                {
                    KeyPressed?.Invoke(item, KeyStatus.Pressed);
                }
            }

            foreach (var item in previousDownKeys)
            {
                if(!downKeys.Contains(item))
                {
                    KeyReleased?.Invoke(item, KeyStatus.Released);
                }
            }
        }
    }
}
