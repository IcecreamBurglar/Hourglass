using System;
using System.Collections.Generic;
using System.Text;

namespace Hourglass.Ecs
{
    public abstract class ComponentBase : ICloneable
    {
        public string Name => _name;
        public int OwnerId { get; internal set; }

        private string _name;

        protected ComponentBase()
        {
            _name = GetType().Name;
            OwnerId = -1;
        }

        public abstract object Clone();
    }
}
