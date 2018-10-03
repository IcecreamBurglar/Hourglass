using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Hourglass.Terminal.Interpreting
{
    public delegate void OnBadCommand(string message, string commandName);
    public delegate void OnCommandNotFound(string commandName);
    public delegate void OnOutput(string value);


    public enum AmbiguousParamRules
    {
        FavorExplicit,
        FavorImplicit,
        Error,
    }

    public class CommandInterpreter : Interpreter
    {
        public const string DEFER_RESOLUTION = "!@#DEFER#@!";

        public event OnBadCommand BadCommand;
        public event OnCommandNotFound CommandNotFound;
        public event OnOutput Output;

        public bool AllowAmbiguousExecution { get; set; }
        public AmbiguousParamRules AmbiguousParamRules { get; set; }


        public CommandInterpreter()
        {
            AllowAmbiguousExecution = true;
            AmbiguousParamRules = AmbiguousParamRules.FavorExplicit;
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
            if (wordPosition > 1 && ResolveContextualCompletionCallback != null)
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
                if (String.IsNullOrWhiteSpace(word))
                {
                    results.Add(item.Key);
                    continue;
                }
                if (item.Key.ToLower().StartsWith(word))
                {
                    results.Add(item.Key);
                }
            }
            //TODO: Use Values as well.
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


            return Invoke(signature.Name, _functions[signature.Name], signature.Args, out failed);
        }

        private object Invoke(string commandName, IEnumerable<Delegate> candidates, string[] args, out bool failed)
        {
            failed = true;

            int highestScore = Int32.MinValue;
            object[] matchingArgs = new object[0];
            Delegate matchingDelegate = null;
            bool ambiguousCandidates = false;

            foreach (var candidate in candidates)
            {
                var parameters = candidate.Method.GetParameters();
                if (parameters.Length != args.Length)
                {
                    //continue;
                }
                int curScore = GetMatchScore(parameters, args, ref matchingArgs);
                if (curScore > highestScore)
                {
                    matchingDelegate = candidate;
                    highestScore = curScore;
                }
                else if (curScore == highestScore)
                {
                    ambiguousCandidates = true;
                    matchingDelegate = candidate;
                    if (!AllowAmbiguousExecution)
                    {
                        break;
                    }
                }
            }


            if (matchingDelegate == null)
            {
                CommandNotFound?.Invoke(commandName);
                return null;
            }
            if (!AllowAmbiguousExecution && ambiguousCandidates)
            {
                BadCommand?.Invoke("Ambiguous command specified.", commandName);
                return null;
            }

            failed = false;
            return Call(matchingDelegate, matchingArgs);
        }


        private int GetMatchScore(ParameterInfo[] parameters, string[] args, ref object[] parsedArgs)
        {
            int score = 0;
            if (parameters.Length < args.Length)
            {
                return 0;
            }
            List<object> argValues = new List<object>();

            void AddArgValue(object value, int points)
            {
                argValues.Add(value);
                score += points;
            }


            for (int i = 0; i < parameters.Length; i++)
            {
                var curParam = parameters[i];
                if (i >= args.Length)
                {
                    if (curParam.HasDefaultValue)
                    {
                        int bonus = 0;
                        if (AmbiguousParamRules == AmbiguousParamRules.FavorImplicit)
                        {
                            bonus += 4;
                        }
                        else if (AmbiguousParamRules == AmbiguousParamRules.FavorExplicit)
                        {
                            bonus -= 1;
                        }
                        AddArgValue(curParam.DefaultValue, bonus);
                    }
                    continue;
                }

                string curArg = args[i];
                object value = null;
                if (ContainsValue(curArg))
                {
                    value = GetValue(curArg, out bool success);
                    if (success)
                    {
                        if (value.GetType() == curParam.ParameterType || curParam.ParameterType.IsInstanceOfType(value))
                        {
                            AddArgValue(value, 4);
                            continue;
                        }
                        curArg = value.ToString();
                    }
                    else
                    {
                        continue;
                    }
                }

                if (curParam.ParameterType.IsEnum)
                {
                    var enumNames = Enum.GetNames(curParam.ParameterType);
                    if (enumNames.Contains(curArg))
                    {
                        AddArgValue(Enum.Parse(curParam.ParameterType, curArg, true), 4);
                        continue;
                    }
                }

                int typeBonus = 1;
                object argValue = null;
                if (curParam.ParameterType == typeof(string))
                {
                    typeBonus = 3;
                    argValue = curArg; //ParsePrimitive(curParam.ParameterType, curArg);
                }
                else if (curParam.ParameterType == typeof(float) ||
                         curParam.ParameterType == typeof(double) ||
                         curParam.ParameterType == typeof(decimal) ||
                         curParam.ParameterType == typeof(bool))
                {
                    typeBonus = 2;
                    argValue = ParsePrimitive(curParam.ParameterType, curArg);
                }
                else if (curParam.ParameterType.IsPrimitive)
                {
                    typeBonus = 1;
                }
                else if(curArg.StartsWith("(") && curArg.EndsWith(")"))
                {
                    argValue = ParseType(curParam.ParameterType, curArg);
                    typeBonus = 4;
                }

                if (argValue == null)
                {
                    try
                    {
                        argValue = Convert.ChangeType(curArg, curParam.ParameterType);
                    }
                    catch
                    {
                        score = Int32.MinValue;
                        break;
                    }
                }
                AddArgValue(argValue, typeBonus);
            }

            if (score > 0)
            {
                parsedArgs = argValues.ToArray();
            }
            return score;
        }

        private object Call(Delegate address, object[] args)
        {
            /*
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
            */
            return address.DynamicInvoke(args);
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
                if (String.IsNullOrWhiteSpace(curArg) && !quoted)
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

        private object ParsePrimitive(Type primitiveType, string input)
        {
            //Integer Types
            if (primitiveType == typeof(long))
            {
                return Int64.Parse(input);
            }
            else if (primitiveType == typeof(ulong))
            {
                return UInt64.Parse(input);
            }
            else if (primitiveType == typeof(int))
            {
                return Int32.Parse(input);
            }
            else if (primitiveType == typeof(uint))
            {
                return UInt32.Parse(input);
            }
            else if (primitiveType == typeof(short))
            {
                return Int16.Parse(input);
            }
            else if (primitiveType == typeof(ushort))
            {
                return UInt16.Parse(input);
            }
            else if (primitiveType == typeof(byte))
            {
                return Byte.Parse(input);
            }
            else if (primitiveType == typeof(sbyte))
            {
                return SByte.Parse(input);
            }
            //Floating-Point Types
            else if (primitiveType == typeof(decimal))
            {
                return Decimal.Parse(input);
            }
            else if (primitiveType == typeof(double))
            {
                return Double.Parse(input);
            }
            else if (primitiveType == typeof(float))
            {
                return Single.Parse(input);
            }
            //Misc Types
            else if (primitiveType == typeof(bool))
            {
                return Boolean.Parse(input);
            }
            else if (primitiveType == typeof(char))
            {
                return Char.Parse(input);
            }

            return input;
        }

        private object ParseType(Type expectedType, string input)
        {
            string[] paramStrings = FindArgsForCtor(input);

            int highestScore = Int32.MinValue;
            object[] matchingArgs = null;
            ConstructorInfo matchingCtor = null;

            foreach (var candidate in expectedType.GetConstructors())
            {
                var parameters = candidate.GetParameters();
                if (parameters.Length != paramStrings.Length)
                {
                    //continue;
                }
                matchingArgs = new object[paramStrings.Length];
                int curScore = GetMatchScore(parameters, paramStrings, ref matchingArgs);
                if (curScore >= highestScore)
                {
                    matchingCtor = candidate;
                    highestScore = curScore;
                }
            }

            if (matchingCtor == null)
            {
                //TODO: Error out here
            }

            return Activator.CreateInstance(expectedType, matchingArgs);
        }

        private string[] FindArgsForCtor(string input)
        {
            if (input.StartsWith("("))
            {
                input = input.Remove(0, 1);
            }

            if (input.EndsWith(")"))
            {
                input = input.Remove(input.Length - 1, 1);
            }

            var curItemBuilder = new StringBuilder();
            List<string> result = new List<string>();
            bool inSubValue = false;
            for (int i = 0; i < input.Length; i++)
            {
                var curChar = input[i];
                if (curChar == '(')
                {
                    inSubValue = true;
                }
                else if (curChar == ')')
                {
                    inSubValue = false;
                    curItemBuilder.Append(curChar);
                    result.Add(curItemBuilder.ToString().Trim());
                    curItemBuilder.Clear();

                    continue;
                }
                else if (curChar == ' ' && !inSubValue && curItemBuilder.ToString().Length > 0)
                {
                    result.Add(curItemBuilder.ToString().Trim());
                    curItemBuilder.Clear();
                    continue;
                }

                if (curChar == ',')
                {
                    continue;
                }
                curItemBuilder.Append(curChar);
            }

            if (curItemBuilder.ToString().Length > 0)
            {
                result.Add(curItemBuilder.ToString().Trim());
            }


            return result.ToArray();
        }
    }
}
