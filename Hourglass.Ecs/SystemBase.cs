using System;
using System.Collections.Generic;
using System.Text;

namespace Hourglass.Ecs
{
    public abstract class SystemBase
    {
        protected List<int> _validEntities;

        protected SystemBase()
        {
            _validEntities = new List<int>();
        }

        internal void EntityModified(int entityId)
        {
            bool wasValid = _validEntities.Contains(entityId);
            bool isValid = IsEntityValid(entityId);

            if(wasValid && !isValid)
            {
                _validEntities.Remove(entityId);
            }
            else if(!wasValid && isValid)
            {
                _validEntities.Add(entityId);
            }
        }

        protected abstract bool IsEntityValid(int entityId);
    }
}
