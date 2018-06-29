using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Hourglass.Terminal.Interpreting
{
    public delegate void OnSetEnvironmentValue(string key, object value);
    public delegate object OnGetEnvironmentValue(string key, out bool success);
    
    public abstract class Interpreter
    {
        public OnSetEnvironmentValue SetEnvironmentValue { get; set; }
        public OnGetEnvironmentValue GetenvironmentValue { get; set; }

        protected object _environment;
        protected Type _environmentType;
        protected Dictionary<string, List<Delegate>> _functions;
        protected Dictionary<string, Value> _values;

        protected Interpreter()
        {
            _environment = null;
            _environmentType = null;
            _functions = new Dictionary<string, List<Delegate>>();
            _values = new Dictionary<string, Value>();

            SetEnvironmentValue = (k, v) =>
            {
                if(_values.ContainsKey(k))
                {
                    _values[k].SetValue(_environment, v);
                }
                else
                {
                    var value = new Value(k);
                    value.SetValue(_environment, v);
                    _values.Add(k, value);
                }
            };
            GetenvironmentValue = (string k, out bool s) =>
            {
                s = false;
                if (_values.ContainsKey(k))
                {
                    s = true;
                    return _values[k].GetValue(_environment, out var _);
                }
                return null;
            };
        }

        public bool ContainsValue(string key)
        {
            return _values.ContainsKey(key);
        }
        
        public object GetValue(string key, out bool success)
        {
            if (_values.ContainsKey(key))
            {
                var value = _values[key];
                return value.GetValue(_environment, out success);
            }

            return GetenvironmentValue(key, out success);
        }

        public void SetValue(string key, object value)
        {
            if (_values.ContainsKey(key))
            {
                _values[key].SetValue(_environment, value);
            }
            SetEnvironmentValue(key, value);
        }

        public void AddFunction(Delegate address)
        {
            AddFunction(address, address.Method.Name);
        }

        public void AddFunction(Delegate address, string name)
        {
            LoadEnvironmentFunction(address, name);
        }


        public void SetEnvironment(object environment)
        {
            SetEnvironment(environment, environment.GetType());
        }

        public void SetEnvironment(Type environmentType)
        {
            SetEnvironment(null, environmentType);
        }
        
        private void SetEnvironment(object environment, Type environmentType)
        {
            _environment = environment;
            _environmentType = environmentType;
            _functions = new Dictionary<string, List<Delegate>>();
            _values = new Dictionary<string, Value>();
            LoadEnvironment();
        }


        protected virtual void LoadEnvironment()
        {
            var bindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var functions = _environmentType.GetMethods(bindingFlags);
            var properties = _environmentType.GetProperties(bindingFlags);
            var fields = _environmentType.GetFields(bindingFlags);

            bool GetName(MemberInfo input, out string name)
            {
                var attribute = input.GetCustomAttribute<EnvironmentItemAttribute>();
                if (attribute == null)
                {
                    name = "";
                    return false;
                }

                name = attribute.Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = input.Name;
                }
                return true;
            }

            foreach (var item in functions)
            {
                if(!GetName(item, out var name))
                {
                    continue;
                }
                LoadEnvironmentFunction(item.CreateDelegate(_environment.GetType(), _environment), name);
            }
            foreach (var item in properties)
            {
                if (!GetName(item, out var name))
                {
                    continue;
                }
                LoadEnvironmentProperty(item, name);
            }
            foreach (var item in fields)
            {
                if (!GetName(item, out var name))
                {
                    continue;
                }
                LoadEnvironmentField(item, name);
            }
        }

        protected virtual void LoadEnvironmentFunction(Delegate func, string name)
        {
            if(!_functions.ContainsKey(name))
            {
                _functions.Add(name, new List<Delegate>(3));
            }
            _functions[name].Add(func);
        }

        protected virtual void LoadEnvironmentProperty(PropertyInfo property, string name)
        {
            _values.Add(name, new Value(name, property));
        }

        protected virtual void LoadEnvironmentField(FieldInfo field, string name)
        {
            _values.Add(name, new Value(name, field));
        }
        

        public abstract void Execute(string code);
        public abstract string[] GetCompletionOptions(string word);
        

        protected enum ValueSources
        {
            Property,
            Field,
            Memory,
        }

        protected class Value
        {
            public string Name { get; private set; }
            public ValueSources ValueSource { get; private set; }
            
            private PropertyInfo _underlyingProperty;
            private FieldInfo _underlyingField;
            private object _underlyingValue;

            public Value(string name)
                : this(name, null, null)
            {
                ValueSource = ValueSources.Memory;
            }

            public Value(string name, PropertyInfo underlyingProperty)
                : this(name, underlyingProperty, null)
            {
                ValueSource = ValueSources.Property;
            }

            public Value(string name, FieldInfo underlyingField)
                : this(name, null, underlyingField)
            {
                ValueSource = ValueSources.Field;
            }

            private Value(string name, PropertyInfo underlyingProperty, FieldInfo underlyingField)
            {
                Name = name;
                _underlyingProperty = underlyingProperty;
                _underlyingField = underlyingField;
            }

            public object GetValue(object context, out bool success)
            {
                if(_underlyingProperty != null)
                {
                    if(_underlyingProperty.CanRead)
                    {
                        success = true;
                        return _underlyingProperty.GetValue(context);
                    }
                }
                else if(_underlyingField != null)
                {
                    success = true;
                    return _underlyingField.GetValue(context);
                }
                else
                {
                    success = true;
                    return _underlyingValue;
                }
                success = false;
                return false;
            }

            public bool SetValue(object context, object value)
            {
                if (_underlyingProperty != null)
                {
                    if (_underlyingProperty.CanWrite)
                    {
                        _underlyingProperty.SetValue(context, value);
                        return true;
                    }
                }
                else if (_underlyingField != null)
                {
                    _underlyingField.SetValue(context, value);
                    return true;
                }
                else
                {
                    _underlyingValue = value;
                    return true;
                }
                return false;
            }
        }
    }
}
