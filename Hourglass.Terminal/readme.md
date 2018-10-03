# The Terminal's Dependencies
This extensible terminal has been tucked away behind a couple layers of abstraction. \
 * The Interpeter 
   + Execution of input
   + Hosts the *Terminal Environment*
 * The RenderManager
   + Takes measurements of strings
   + It is intended for implementors to also render the terminal here
 * The InputProvider
   + Reports text changes to the console
   + Parses input text into words to be consumed by the terminal and interpreter
   
It's designed this way to allow for customizations. For instance, you could host an ironpython
editor from right within your game through a custom interpreter implementation. \

# The Terminal
The Terminal itself is mostly just a tangled web of formatting and string manipulation.
But it does offer some features, such as:
  * Built-in colorization of output text (it's up to the renderer to respect the color settings)
  * Multiline input support
  * Input auto-completion
  * Input history

## Usage
Instantiating a terminal requires only a `maxWidth` integer, and the input provider, render manager, and interpreter.
And it looks something like this
```cs
var windowWidth = 100;
var inputProvider = new MonoGameInputProvider(inputManager);
var renderManager = new MonoGameRenderer(font, backgroundTexture, spriteBatch);
var interpreter = new CommandInterpreter();
var terminal = new Terminal(windowWidth, inputProvider, renderManager, interpreter);

interpreter.SetEnvironment(new TerminalEnvironment());
```
Optionally, you can pass in a `lineContinuation` string which tells the terminal what to look out for when a user wants
to input multiple lines of text. \
When the user leaves the lineContinuation string at the end of a line of input, the terminal will continue onto the next line.

### The MonoGameInputProvider
As can be seen in the above snipper, the `MonoGameInputProvider` requires a reference to an `InputManager`.
The InputManager is contained within the library *HourGlass.MonoGame.Input*, and handles abstracting away keyboard, mouse,
controller, and all other kinds of input. But most importantly for the MonoGameInputProvider, the InputManager
*handles the observation of text input*. \
The MonoGameInputProvider adds a new TextWatcher aptly named "terminal" to the input manager. Additionally, it watches for
important key presses, such as Enter/Return for execution, the Up/Down arrows for history cycling, and tab for auto-completion.

### The MonoGameRenderer
The MonoGameRenderer requires a spritefont, texture, and spritebatch for instantiation. The font is -- well, the font that
is used for rendering text. The texture is used for background rendering, and the spritebatch is used for drawing to
the screen. \
The MonoGameRenderer measures string lengths for the Terminal, as well as drawing the caret position, the prompt, all text,
and the background.

### The CommandInterpreter
This is where all the real heavy-lifting takes place. The command interpreter is the default Interpreter that works how you
would probably expect it to; in the format "[command] [args separated by spaces]". After you've instantiates the
CommandInterpreter, you must set its environment. \
The environment can be any Type, and there are various features available to the design of the environment. \
Firstly, you designate methods/functions as being commands with the `[EnvironmentItem]` attribute, optionally specifying
a command name to use. If no command name is specified, the name of the function is used. Here are some other features
available:
 *  Any number of parameters for commands
 *  Any type of parameter is possible
 *  Optional parameters are legal
 *  Command overloading
 *  *Contextual Auto Completion Resolution* (actually partially im feature of the abstract super-class *Interpreter*)
OK, so that last one might stand out. All it means is that the author of the environment can specify the options
for auto completion based upon which parameter the user is currently inputting to. So, for instance, you could have
the following
```cs
public enum Names
{
  Bob,
  Jill,
}


[EnvironmentContextualCompletionResolver]
public string[] Resolve(string command, int argPos)
{
  if (command == "Greet")
  {
      if (argPos == 2)
      {
          return new[] {"Bob", "Jill"};
      }
  }
  return new []  {CommandInterpreter.DEFER_RESOLUTION};
}

[EnvironmentItem]
private void Greet(string who)
{
  Console.WriteLine(&"Hello, {who}");
}
```

Then, if the user has the following currently input into the terminal "Greet " and they trigger auto-completion,
the terminal will cycle through the string array {"Bob", "Jill"}. But of course the user could still input
any string they wanted. By the way, `Interpreter.DEFER_RESOLUTION` is a wacky
string constant denoting that you'd prefer the interpreter decide what to do. And to take an educated guess,
it probably won't really do anything maybe.)

## Conclusion
OK, so that's the basic rundown. \
If you find an issue, please report it! \
If you have questions/comments, please file an issue! \
If you modify/add to the source, please submit a PR! \


# Extending
