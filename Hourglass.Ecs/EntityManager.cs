using System;
using System.Collections.Generic;
using System.Linq;

namespace Hourglass.Ecs
{
    public delegate T EntityModifier<T>(T component) where T : IComponent;

    public class EntityManager
    {
        public bool RecycleIds { get; private set; }
        public EntitySystem[] Systems => _systems.ToArray();
        
        public object[] Entities => _entities.ToArray();
        public int EntityCount => _entities.Count;
        public string[] Categories => _entityCategories.Keys.ToArray();

        private List<EntitySystem> _systems;
        private List<object> _entities;
        private Dictionary<string, List<int>> _entityCategories;

        private Queue<int> _recycledIds;
        private int _nextIdIndex;

        public EntityManager(bool recycleIds)
        {
            _systems = new List<EntitySystem>();
            _entities = new List<object>(ushort.MaxValue);
            _nextIdIndex = 0;
            _entityCategories = new Dictionary<string, List<int>>();
            RecycleIds = recycleIds;
            if(recycleIds)
            {
                _recycledIds = new Queue<int>();
            }
        }

        public int CreateEntity()
        {
            return CreateEntity("", new IComponent[0]);
        }

        public int CreateEntity(string category)
        {
            return CreateEntity(category, new IComponent[0]);
        }

        public int CreateEntity(params IComponent[] components)
        {
            return CreateEntity("", components);
        }

        public int CreateEntity(string category, params IComponent[] components)
        {
            int id = _nextIdIndex;
            bool recycled = false;
            if(RecycleIds && _recycledIds.Count > 0)
            {
                id = _recycledIds.Dequeue();
                recycled = true;
            }
            _entities.Insert(id, _entities.Count + 1);
            _entities.AddRange(components);
            
            for(int i = 0; i < id; i++)
            {
                var value = (int)_entities[i];
                value++;
                _entities[i] = value;
            }

            if(components != null && components.Length > 0)
            {
                ValidifyEntity(id);
            }
            if(!recycled)
            {
                _nextIdIndex++;
            }
            return id;
        }

        public EntityManager AddSystem(EntitySystem system)
        {
            if(system.EntityManager != null)
            {
                throw new InvalidOperationException("That entity system has already been assigned to a manager.");
            }
            _systems.Add(system);
            system.AddedToManager(this);
            for (int i = 0; i < _nextIdIndex; i++)
            {
                system.EntityModified(i);
            }
            return this;
        }

        public EntityManager RemoveSystem(EntitySystem system)
        {
            if(system.EntityManager != this)
            {
                throw new InvalidOperationException("That entity system is not assigned to this manager.");
            }
            _systems.Remove(system);
            system.RemovedFromManager();
            for (int i = 0; i < _nextIdIndex; i++)
            {
                system.EntityDestroyed(i);
            }
            return this;
        }

        public bool ContainsSystem(EntitySystem system)
        {
            return _systems.Contains(system);
        }

        public EntityManager DestroyEntity(int entityId)
        {            
            foreach (var system in _systems)
            {
                system.EntityDestroyed(entityId);
            }

            var entityStart = (int)_entities[entityId];
            var nextEntityStart = _entities.Count;
            if(entityId + 1 < _nextIdIndex)
            {
                nextEntityStart = (int)_entities[entityId + 1];
            }

            for (int i = 0; i < nextEntityStart - entityStart; i++)
            {
                _entities.RemoveAt(entityStart);
            }
            for (int i = entityId + 1; i < _nextIdIndex; i++)
            {
                var value = (int)_entities[i];
                value -= nextEntityStart - entityStart;
                _entities[i] = value;
            }

            _entities[entityId] = -1;

            if(RecycleIds)
            {
                _recycledIds.Enqueue(entityId);
            }
            return this;
        }

        public EntityManager DestroyEntities(string category)
        {
            foreach (var item in _entityCategories[category])
            {
                DestroyEntity(item);
            }
            _entityCategories[category].Clear();
            return this;
        }

        public int[] GetEntitiesInCategory(string category)
        {
            if(_entityCategories.ContainsKey(category))
            {
                return _entityCategories[category].ToArray();
            }
            return new int[0];
        }

        public bool EntityIsAlive(int entityId)
        {
            return _nextIdIndex > entityId && entityId > 0 && (_entities[entityId] is int i && i != -1);
        }

        public T AddNewComponent<T>(int entityId) where T : IComponent, new()
        {
            T value = new T();
            AddComponent(entityId, value);
            return value;
        }
        
        public EntityManager AddComponent<T>(int entityId, T component) where T : IComponent
        {
            if (component.OwnerId != 0)
            {
                return this;
            }
            component.OwnerId = entityId;

            var start = (int)_entities[entityId];
            _entities.Insert(start, component);
            for (int i = entityId + 1; i < _nextIdIndex; i++)
            {
                var value = (int)_entities[i];
                value++;
                _entities[i] = value;
            }
            
            ValidifyEntity(entityId);
            return this;
        }

        public EntityManager RemoveComponent<T>(int entityId) where T : IComponent
        {
            var entityStart = (int)_entities[entityId];
            var nextEntityStart = _entities.Count;
            if(entityId + 1 < _nextIdIndex)
            {
                nextEntityStart = (int)_entities[entityId + 1];
            }

            for (int i = entityStart; i < nextEntityStart; i++)
            {
                if (_entities[i] is T)
                {
                    _entities.RemoveAt(i);
                    ValidifyEntity(entityId);
                    return this;
                }
            }
            for (int i = entityId + 1; i < _nextIdIndex; i++)
            {
                var value = (int)_entities[i];
                value--;
                _entities[i] = value;
            }

            return this;
        }

        public EntityManager RemoveComponent(int entityId, string componentName)
        {
            var entityStart = (int)_entities[entityId];
            var nextEntityStart = _entities.Count;
            if(entityId + 1 < _nextIdIndex)
            {
                nextEntityStart = (int)_entities[entityId + 1];
            }

            for (int i = entityStart; i < nextEntityStart; i++)
            {
                if (((IComponent)_entities[i]).ComponentName == componentName)
                {
                    _entities.RemoveAt(i);
                    ValidifyEntity(entityId);
                    return this;
                }
            }
            for (int i = entityId + 1; i < _nextIdIndex; i++)
            {
                var value = (int)_entities[i];
                value--;
                _entities[i] = value;
            }
            
            return this;
        }

        public EntityManager ClearComponents(int entityId)
        {            
            var entityStart = (int)_entities[entityId];
            var nextEntityStart = _entities.Count;
            if(entityId + 1 < _nextIdIndex)
            {
                nextEntityStart = (int)_entities[entityId + 1];
            }

            for (int i = 0; i < nextEntityStart - entityStart; i++)
            {
                _entities.RemoveAt(entityStart);
                ValidifyEntity(entityId);
                return this;
            }
            for (int i = entityId + 1; i < _entities.Count; i++)
            {
                var value = (int)_entities[i];
                value -= nextEntityStart - entityStart;
                _entities[i] = value;
            }

            return this;
        }

        public IComponent[] EnumComponents(int entityId)
        {
            var entityStart = (int)_entities[entityId];
            var nextEntityStart = _entities.Count;
            if(entityId + 1 < _nextIdIndex)
            {
                nextEntityStart = (int)_entities[entityId + 1];
            }

            var components = new IComponent[nextEntityStart - entityStart];
            for (int i = entityStart; i < nextEntityStart; i++)
            {
                components[i - entityStart] = (IComponent)_entities[i];
            }
            return components;
        }

        public bool ContainsComponent<T>(int entityId) where T : IComponent
        {
            var entityStart = (int)_entities[entityId];
            var nextEntityStart = _entities.Count;
            if(entityId + 1 < _nextIdIndex)
            {
                nextEntityStart = (int)_entities[entityId + 1];
            }

            for (int i = entityStart; i < nextEntityStart; i++)
            {
                if(_entities[i] is T)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsComponent(int entityId, string componentName)
        {
            var entityStart = (int)_entities[entityId];
            var nextEntityStart = _entities.Count;
            if(entityId + 1 < _nextIdIndex)
            {
                nextEntityStart = (int)_entities[entityId + 1];
            }

            for (int i = entityStart; i < nextEntityStart; i++)
            {
                if(((IComponent)_entities[i]).ComponentName == componentName)
                {
                    return true;
                }
            }
            return false;
        }
        
        public T ReadComponent<T>(int entityId) where T : IComponent
        {
            return ReadComponent<T>(entityId, default(T));
        }
        
        public T ReadComponent<T>(int entityId, T defaultValue) where T : IComponent
        {
            var entityStart = (int)_entities[entityId];
            var nextEntityStart = _entities.Count;
            if(entityId + 1 < _nextIdIndex)
            {
                nextEntityStart = (int)_entities[entityId + 1];
            }

            for (int i = entityStart; i < nextEntityStart; i++)
            {
                if(_entities[i] is T)
                {
                    return (T)_entities[i];
                }
            }
            return defaultValue;
        }

        public void ActOn<T>(int entityId, EntityModifier<T> act) where T : IComponent
        {
            var entityStart = (int)_entities[entityId];
            var nextEntityStart = _entities.Count;
            if(entityId + 1 < _nextIdIndex)
            {
                nextEntityStart = (int)_entities[entityId + 1];
            }

            for (int i = entityStart; i < nextEntityStart; i++)
            {
                if(_entities[i] is T)
                {
                    var item = (T)_entities[i];
                    item = act(item);
                    _entities[i] = item;
                }
            }
        }

        public IComponent GetComponent(int entityId, Type componentType)
        {
            var entityStart = (int)_entities[entityId];
            var nextEntityStart = _entities.Count;
            if(entityId + 1 < _nextIdIndex)
            {
                nextEntityStart = (int)_entities[entityId + 1];
            }

            for (int i = entityStart; i < nextEntityStart; i++)
            {
                if(_entities[i].GetType() == componentType)
                {
                    return (IComponent)_entities[i];
                }
            }
            return null;
        }

        private void ValidifyEntity(int entityId)
        {
            foreach (var system in _systems)
            {
                system.EntityModified(entityId);
            }
        }
    }
}
