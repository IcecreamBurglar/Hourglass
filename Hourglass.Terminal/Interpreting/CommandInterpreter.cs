using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hourglass.Terminal.Interpreting
{
    //TODO: Implement optional arguments.

    public delegate void OnBadCommand(string message, string commandName);
    public delegate void OnCommandNotFound(string commandName);
    public delegate void OnOutput(string value);


    public class CommandInterpreter : Interpreter
    {
        public event OnBadCommand BadCommand;
        public event OnCommandNotFound CommandNotFound;
        public event OnOutput Output;


        public CommandInterpreter()
        {
        }

        public override void Execute(string code)
        {
            string[] lines = code.Split('\n');
            object lastResult = null;
            foreach (var item in lines)
            {
                lastResult = Eval(item.Trim(), out bool failed);
                if(failed)
                {
                    return;
                }
            }

            if(lastResult != null)
            {
                Output?.Invoke(lastResult.ToString());
            }
        }

        public override string[] GetCompletionOptions(string firstWord, string word, int wordPosition)
        {
            if (wordPosition > 0 && ResolveContextualCompletionCallback != null)
            {
                var resolved = ResolveContextualCompletionCallback(firstWord, wordPosition);
                if (resolved.Length != 1 || resolved[0] != DEFER_RESOLUTION)
                {
                    return resolved;
                }
            }

            var results = new List<string>();
            word = word.ToLower();
            foreach (var item in _functions)
            {
                if(results.Contains(item.Key))
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(word))
                {
                    results.Add(item.Key);
                    continue;
                }
                if (item.Key.ToLower().StartsWith(word))
                {
                    results.Add(item.Key);
                }
            }
            return results.ToArray();
        }


        private object Eval(string code, out bool failed)
        {
            var signature = SplitCommand(code);
            failed = true;

            if (!_functions.ContainsKey(signature.Name))
            {
                if (ContainsValue(signature.Name))
                {
                    Output?.Invoke($"{signature.Name} = {GetValue(signature.Name, out var _)}");
                }
                else
                {
                    CommandNotFound?.Invoke(signature.Name);
                }
                return null;
            }

            var candidates = _functions[signature.Name].Where((a) => a.Method.GetParameters().Length == signature.Args.Length);
            
            if (!candidates.Any())
            {
                CommandNotFound?.Invoke(signature.Name);
                return null;
            }

            candidates = TrimCandidates(candidates, signature.Args);
            if (candidates.Count() > 1)
            {
                BadCommand?.Invoke("Ambiguous command specified.", signature.Name);
                return null;
            }

            failed = false;
            return Call(candidates.ElementAt(0), signature.Name, signature.Args);
        }

        private IEnumerable<Delegate> TrimCandidates(IEnumerable<Delegate> input, string[] args)
        {
            //TODO: Argument type inference
            List<Delegate> output = new List<Delegate>();
            output.AddRange(input);
            return output;
        }

        private object Call(Delegate address, string name, string[] args)
        {
            List<object> parsedArgs = new List<object>();
            var parameters = address.Method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                var curParam = parameters[i];
                string arg = args[i];
                object argValue = arg;
                if(ContainsValue(arg))
                {
                    var value = GetValue(arg, out bool success);
                    if(success)
                    {
                        if(value.GetType() == curParam.ParameterType || curParam.ParameterType.IsAssignableFrom(value.GetType()))
                        {
                            parsedArgs.Add(value);
                            continue;
                        }
                        else
                        {
                            argValue = value.ToString();
                        }
                    }
                }
                try
                {
                    parsedArgs.Add(Convert.ChangeType(argValue, curParam.ParameterType));
                }
                catch
                {
                    BadCommand?.Invoke($"Bad argument type detected. Can't cast value '{args[i]}' to type '{curParam.ParameterType}'.", name);
                    return "";
                }
            }
            return address.DynamicInvoke(parsedArgs.ToArray());
        }

        private (string Name, string[] Args) SplitCommand(string command)
        {
            var args = new List<string>();
            string name = "";


            bool parsingName = true;
            bool inQuote = false;
            string curArg = "";
            bool quoted = false;

            void AddArg()
            {
                if (string.IsNullOrWhiteSpace(curArg) && !quoted)
                {
                    return;
                }
                args.Add(curArg);
                curArg = "";
                quoted = false;
            }

            for (int i = 0; i < command.Length; i++)
            {
                char curChar = command[i];
                if(parsingName)
                {
                    if(curChar == ' ')
                    {
                        parsingName = false;
                        continue;
                    }
                    name += curChar;
                    continue;
                }
                if(curChar == '\"')
                {
                    inQuote = !inQuote;
                    if(inQuote)
                    {
                        quoted = true;
                    }
                    continue;
                }
                if(curChar == '\\')
                {
                    if(i + 1 >= command.Length)
                    {
                        break;
                    }
                    char nextChar = command[i + 1];
                    curArg += nextChar;
                    i++;
                    continue;
                }
                if(curChar == ' ' && !inQuote)
                {
                    AddArg();
                    continue;
                }
                curArg += curChar;
            }

            AddArg();
            return (name, args.ToArray());
        }
    }
}
