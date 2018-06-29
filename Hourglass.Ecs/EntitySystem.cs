using System.Collections.Generic;

namespace Hourglass.Ecs
{
    public abstract class EntitySystem
    {
        public EntityManager EntityManager { get; private set; }
        protected List<int> _validEntites;

        protected EntitySystem()
        {
            _validEntites = new List<int>();
        }

        public virtual void AddedToManager(EntityManager manager)
        {
            EntityManager = manager;
        }

        public virtual void RemovedFromManager()
        {
        }

        public void EntityModified(int entityId)
        {
            bool valid = EntityValid(entityId);
            bool wasValid = _validEntites.Contains(entityId);
            
            if (valid && !wasValid)
            {
                _validEntites.Add(entityId);
            }
            if (!valid && wasValid)
            {
                _validEntites.Remove(entityId);
            }
        }

        public void EntityDestroyed(int entityId)
        {
            if (_validEntites.Contains(entityId))
            {
                _validEntites.Remove(entityId);
            }
        }

        protected abstract bool EntityValid(int entityId);
    }
}
