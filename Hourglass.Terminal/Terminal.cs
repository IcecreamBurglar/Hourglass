using Hourglass.Terminal.Input;
using Hourglass.Terminal.Interpreting;
using Hourglass.Terminal.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hourglass.Terminal
{
    public class Terminal
    {
        //The maximum number of pixels that can be on a line before wrapping to the next.
        public int MaxWidth { get; set; }


        //The string that denotes that we're continuing our input on a new line.
        public string InputContinuation { get; set; }


        //The inputmanager from whence input comes.
        public TerminalInputProvider InputManager { get; private set; }

        //The rendermanager.
        public RenderManager RenderManager { get; private set; }

        //The interpreter.
        public Interpreter Interpreter { get; set; }

        //The current output color.
        public (byte R, byte G, byte B, byte A) OutputColor { get; set; }

        //The current output back color.
        public (byte R, byte G, byte B, byte A) OutputBackColor { get; set; }

        //Whether or not we echo input.
        public bool Echo { get; set; }
        
        //The current output color for echoes.
        public (byte R, byte G, byte B, byte A) EchoColor { get; set; }

        //The current output back color for echoes.
        public (byte R, byte G, byte B, byte A) EchoBackColor { get; set; }

        
        //The current output color for input.
        public (byte R, byte G, byte B, byte A) InputColor { get; set; }

        //The current output back color for echoes.
        public (byte R, byte G, byte B, byte A) InputBackColor { get; set; }

        //Gets the current input caret index.
        public int CaretPosition => InputManager.GetCaretPosition();

        //A string to prepend to echoed input.
        public string EchoPrompt { get; set; }

        //Gets the history.
        public string[] History => _history.ToArray();

        //Gets the history index.
        public int HistoryIndex => _historyIndex;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                InputManager.Enabled = value;
                RenderManager.Enabled = value;
                _enabled = value;
            }
        }
        
        //The buffer.
        private Stack<TerminalOutput> _bufferStack;
        //The buffer used for input.
        private List<string> _inputBuffer;

        private List<string> _history;
        private int _historyIndex;

        private List<string> _completionOptions;
        private int _completionOptionIndex;
        private string _lastCompletion;
        //Tracks the position of the word we last resolved completions for.
        private int _completionWordPosition;

        private bool _enabled;

        public Terminal(int width, Interpreter interpreter)
            : this(width, "", TerminalInputProvider.Default, RenderManager.Default, interpreter)
        {
        }

        public Terminal(int width, TerminalInputProvider inputManager, RenderManager renderManager, Interpreter interpreter)
            : this(width, "", inputManager, renderManager, interpreter)
        {
        }

        public Terminal(int width, string inputContinuation, TerminalInputProvider inputManager, RenderManager renderManager, Interpreter interpreter)
        {
            MaxWidth = width;

            _bufferStack = new Stack<TerminalOutput>();

            _inputBuffer = new List<string>(10);
            _inputBuffer.Add("");
            InputContinuation = inputContinuation;

            InputManager = inputManager;
            inputManager.TextChanged += OnInputChanged;
            inputManager.ExecuteInput += OnExecuteInput;
            inputManager.HistoryNext += OnHistoryNext;
            inputManager.HistoryPrevious += OnHistoryPrevious;
            inputManager.AutoComplete += OnAutoComplete;

            RenderManager = renderManager;
            Interpreter = interpreter;
            
            OutputColor = (255, 255, 255, 255);
            OutputBackColor = (0, 0, 0, 0);

            Echo = true;
            EchoPrompt = "> ";
            EchoColor = (255, 255, 255, 255);
            EchoBackColor = (192, 192, 192, 255);

            InputColor = (0, 0, 0, 255);
            InputBackColor = (255, 255, 0, 255);

            InputContinuation = "->";

            _history = new List<string>();
            _historyIndex = -1;

            _completionOptions = new List<string>();
            _completionOptionIndex = -1;
            _lastCompletion = "";
            _completionWordPosition = -1;

            _enabled = false;
        }

        public string GetInputText()
        {
            return _inputBuffer[LastInputIndex()];
        }

        public TerminalOutput[] GetBottomUpOutput(ref int startLine, int count)
        {
            if(_bufferStack.Count == 0)
            {
                return new TerminalOutput[0];
            }

            var output = new List<TerminalOutput>(count);
            
            if(startLine + count - 1 >= _bufferStack.Count)
            {
                startLine = _bufferStack.Count - count;
            }

            for (int i = startLine + count - 1; i >= startLine && i >= 0; i--)
            {
                output.Add(_bufferStack.ElementAt(i));
            }

            return output.ToArray();
        }

        public void Clear()
        {
            _bufferStack.Clear();
        }

        public void WriteSeparator()
        {
            var charWidth = RenderManager.MeasureTextWidth("─");
            string curLine = "";
            do 
            {
                curLine += '─';
            } while (RenderManager.MeasureTextWidth(curLine) < MaxWidth - charWidth * 2);
            Write(curLine);
        }

        public void Write(string format, object arg)
        {
            Write(string.Format(format, arg));
        }

        public void Write(string format, object arg0, object arg1)
        {
            Write(string.Format(format, arg0, arg1));
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            Write(string.Format(format, arg0, arg1, arg2));
        }


        public void Write(char value)
        {
            Write(value.ToString());
        }

        public void Write(byte value)
        {
            Write(value.ToString());
        }

        public void Write(int value)
        {
            Write(value.ToString());
        }

        public void Write(float value)
        {
            Write(value.ToString());
        }

        public void Write(double value)
        {
            Write(value.ToString());
        }

        public void Write(string text)
        {
            text = text.TrimEnd();
            if (!text.Contains("\n"))
            {
                AddLineToBuffer(text);
                return;
            }
            foreach (var line in text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string lineWithEnding = line.TrimEnd() + Environment.NewLine;
                if (RenderManager.MeasureTextWidth(lineWithEnding) > MaxWidth)
                {
                    string wrappedLine = "";
                    for (int i = 0; i < lineWithEnding.Length; i++)
                    {
                        if (RenderManager.MeasureTextWidth(wrappedLine) >= MaxWidth)
                        {
                            AddLineToBuffer(wrappedLine);
                            wrappedLine = "";
                        }
                        wrappedLine += lineWithEnding[i];
                    }
                }
                else
                {
                    AddLineToBuffer(line.TrimEnd());
                }
            }
        }


        private void AddLineToBuffer(string line)
        {
            _bufferStack.Push(new TerminalOutput(line, OutputColor, OutputBackColor));
        }

        private int LastInputIndex()
        {
            return _inputBuffer.Count - 1;
        }

        private void WriteEcho(string echoText)
        {
            if(Echo)
            {
                var oldColor = OutputColor;
                var oldBackColor = OutputBackColor;

                OutputColor = EchoColor;
                OutputBackColor = EchoBackColor;
                Write("{0}{1}", EchoPrompt, echoText);

                OutputColor = oldColor;
                OutputBackColor = oldBackColor;
            }
        }

        private void WriteInput()
        {
            var oldColor = OutputColor;
            var oldBackColor = OutputBackColor;

            OutputColor = InputColor;
            OutputBackColor = InputBackColor;

            foreach (var item in _inputBuffer)
            {
                Write("{0}{1}", EchoPrompt, item);
            }

            OutputColor = oldColor;
            OutputBackColor = oldBackColor;
        }

        private void OnExecuteInput()
        {
            var lastIndex = LastInputIndex();
            if(_history.Count == 0 || _history[_history.Count - 1] != _inputBuffer[lastIndex])
            { 
                _history.Insert(0, _inputBuffer[lastIndex]);
            }
            if (_inputBuffer[lastIndex].EndsWith(InputContinuation))
            {
                //_inputBuffer[lastIndex] = _inputBuffer[lastIndex]
                //                                    .Remove(_inputBuffer[lastIndex]
                //                                        .IndexOf(InputContinuation[0])) + Environment.NewLine;
                
                //WriteEcho(_inputBuffer[lastIndex]);
                InputManager.ResetTextInput();
                _inputBuffer.Add("");
                return;
            }

            string input = "";
            //WriteEcho(_inputBuffer[lastIndex]);
            for (int i = 0; i < _inputBuffer.Count; i++)
            {
                if (_inputBuffer[i].EndsWith(InputContinuation))
                {
                    _inputBuffer[i] = _inputBuffer[i]
                        .Remove(_inputBuffer[i].IndexOf(InputContinuation[0])) + Environment.NewLine;
                }
                input += _inputBuffer[i];
            }
            input = input.Trim();

            WriteInput();

            Interpreter.Execute(input);

            _inputBuffer.Clear();
            _inputBuffer.Add("");
            InputManager.ResetTextInput();
        }

        private void OnInputChanged(ref string textInput)
        {
            if(!textInput.EndsWith(_lastCompletion))
            {
                _completionOptions.Clear();
                _completionOptionIndex = 0;
            }

            textInput = textInput.Replace(Environment.NewLine, "");
            int width = RenderManager.MeasureTextWidth(textInput + "WWW");

            var curIndex = LastInputIndex();
            if (width >= MaxWidth)
            {
                _inputBuffer[curIndex] = textInput + InputContinuation;
                _inputBuffer.Add("");
                curIndex++;
                InputManager.ResetTextInput();
            }
            else
            {
                if(_inputBuffer.Count == 0)
                {
                    _inputBuffer.Add(textInput);
                }
                else
                {
                    _inputBuffer[curIndex] = textInput;
                }
            }
        }

        private void OnAutoComplete()
        {
            var curWord = InputManager.GetCurrentWord();
            int curWordPosition = curWord.WordIndex;
            if (_completionWordPosition != curWordPosition)
            {
                _completionOptions.Clear();
            }

            if (_completionOptions.Count == 0)
            {
                var firstWord = InputManager.GetFirstWord();
                _completionOptions.AddRange(Interpreter.GetCompletionOptions(firstWord.Text, curWord.Text, curWordPosition));
                _completionWordPosition = curWordPosition;
                //TODO: Create a lookup table recalling words and the completion indices for those words.
                _completionOptionIndex = 0;
            }
            if (_completionOptions.Count > 0)
            {
                _lastCompletion = _completionOptions[_completionOptionIndex];
                InputManager.SetCurrentWord(_completionOptions[_completionOptionIndex]);
                _completionOptionIndex++;
                if (_completionOptionIndex >= _completionOptions.Count)
                {
                    _completionOptionIndex = 0;
                }
            }
        }

        private void OnHistoryNext()
        {
            _historyIndex++;
            if (_historyIndex >= _history.Count)
            {
                _historyIndex = _history.Count - 1;
            }
            if(_historyIndex < 0)
            {
                return;
            }
            InputManager.SetTextInput(_history[_historyIndex]);
        }

        private void OnHistoryPrevious()
        {
            _historyIndex--;
            if (_historyIndex < 0)
            {
                _historyIndex = 0;
            }
            if(_historyIndex >= _history.Count)
            {
                return;
            }
            InputManager.SetTextInput(_history[_historyIndex]);
        }
    }
}
