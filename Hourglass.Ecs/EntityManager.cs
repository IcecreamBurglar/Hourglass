using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Hourglass.Ecs
{
    public class EntityManager
    {
        //An estimated number of components per entity.
        //This number is arbitrary and doesn't really alter the operation
        //of this EntityManager.
        //Its's only used for the capacity when instantiating the
        //list of entities.
        private const int COMPONENTS_PER_ENTITY = 6;

        public List<SystemBase> Systems { get; private set; }

        private List<EntityItem> _entities;
        private Queue<int> _deadIds;
        private int _nextId;

        public EntityManager(int entityCount)
        {
            Systems = new List<SystemBase>();
            _entities = new List<EntityItem>(entityCount * COMPONENTS_PER_ENTITY);
            _deadIds = new Queue<int>(entityCount);
            _nextId = 0;
        }
        
        public void RefreshSystems(int entityId)
        {
            //Inform each system that the entity has been modified.
            foreach (var system in Systems)
            {
                system.EntityModified(entityId);
            }
        }

        public int CreateEntity()
        {
            //Check for a recyclable ID first.
            if(_deadIds.Count != 0)
            {
                int id = _deadIds.Dequeue();
                var ent = _entities[id];
                ent.IsAlive = true;
                _entities[id] = ent;
                return id;
            }

            //Increment each previous entity's starting index.
            for (int i = 0; i < _nextId; i++)
            {
                var ent = _entities[i];
                ent.EntityIndex++;
                _entities[i] = ent;
            }
            //Add this entity in with a starting index pointing to the end
            //of the array.
            _entities.Insert(_nextId, new EntityItem(_entities.Count + 1));
            return _nextId++;
        }

        public int BuildEntity(params ComponentBase[] components)
        {
            //Go ahead and create an entity for use.
            int id = CreateEntity();

            //Because we don't recycle IDs, it's OK to assume this entity will just
            //be placed on the end and thus the components can blindly be added
            //like this without adverse consequences.
            foreach (var item in components)
            {
                item.OwnerId = id;
                _entities.Add(new EntityItem(item));
            }

            //Notify the systems about our new entity.
            RefreshSystems(id);

            return id;
        }

        public void DestroyEntity(int entityId)
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //Find the end index of the entity's component list.
            int endIndex = _entities.Count;
            if(IsEntityPointer(entityId + 1, out var nextEntityItem))
            {
                endIndex = nextEntityItem.EntityIndex;
            }

            //Hold the actual number of components this entity owns.
            int componentCount = endIndex - entItem.EntityIndex;

            //Decrement all following entity pointers to account for the removal
            //of the components from the current entity.
            IterateEntityPointers(entityId + 1, (i, e) =>
                {
                    e.EntityIndex -= componentCount;
                    _entities[i] = e;
                });

            //Remove the entity and mark the pointer as dead.
            _entities.RemoveRange(entItem.EntityIndex, componentCount);
            entItem.IsAlive = false;
            _entities[entityId] = entItem;
            _deadIds.Enqueue(entityId);
        }

        public void PushComponent(int entityId, ComponentBase component)
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //If there are entities after this one, increment their starting indices.
            IterateEntityPointers(entityId + 1, (i, e) =>
            {
                e.EntityIndex++;
                _entities[i] = e;
            });

            //Add the component under the entity's area.
            component.OwnerId = entityId;
            _entities.Insert(entItem.EntityIndex, new EntityItem(component));

            //Notify the systems that an entity has been modified.
            RefreshSystems(entityId);
        }

        public void PushComponents(int entityId, params ComponentBase[] components)
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //If there are entities after this one, increment their starting indices.
            IterateEntityPointers(entityId + 1, (i, e) =>
            {
                e.EntityIndex += components.Length;
                _entities[i] = e;
            });

            //Add the components under the entity's area.
            foreach (var component in components)
            {
                component.OwnerId = entityId;
                _entities.Insert(entItem.EntityIndex, new EntityItem(component));
            }

            //Notify the systems that an entity has been modified.
            RefreshSystems(entityId);
        }

        public void RemoveComponent(int entityId, ComponentBase component)
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //Find the index of the component.
            int componentIndex = -1;
            IterateEntity(entItem, entityId, (i, c) =>
            {
                if(c.Component == component)
                {
                    c.Component.OwnerId = -1;
                    componentIndex = i;
                    return true;
                }
                return false;
            });
            
            //Sanity check to make sure the component exists.
            if(componentIndex == -1)
            {
                throw new InvalidOperationException("The specified entity does not contain that component.");
            }

            //If there are entities after this one, decrement their starting indices.
            IterateEntityPointers(entityId + 1, (i, e) =>
            {
                e.EntityIndex--;
                _entities[i] = e;
            });

            //Remove the component
            _entities.RemoveAt(componentIndex);

            //Notify the systems that an entity has been modified.
            RefreshSystems(entityId);
        }

        public void RemoveComponent<T>(int entityId) where T : ComponentBase
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //Find the index of the component.
            int componentIndex = -1;
            IterateEntity(entItem, entityId, (i, c) =>
            {
                if (c.Component is T)
                {
                    c.Component.OwnerId = -1;
                    componentIndex = i;
                    return true;
                }
                return false;
            });

            //Sanity check to make sure the component exists.
            if (componentIndex == -1)
            {
                throw new InvalidOperationException("The specified entity does not contain that component.");
            }

            //If there are entities after this one, decrement their starting indices.
            IterateEntityPointers(entityId + 1, (i, e) =>
            {
                e.EntityIndex--;
                _entities[i] = e;
            });

            //Remove the component
            _entities.RemoveAt(componentIndex);

            //Notify the systems that an entity has been modified.
            RefreshSystems(entityId);
        }

        public void RemoveComponent<T>(int entityId, Predicate<T> check) where T : ComponentBase
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //Find the index of the component.
            int componentIndex = -1;
            IterateEntity(entItem, entityId, (i, c) =>
            {
                if (c.Component is T && check((T)c.Component))
                {
                    c.Component.OwnerId = -1;
                    componentIndex = i;
                    return true;
                }
                return false;
            });

            //Sanity check to make sure the component exists.
            if (componentIndex == -1)
            {
                return;
            }

            //If there are entities after this one, decrement their starting indices.
            IterateEntityPointers(entityId + 1, (i, e) =>
            {
                e.EntityIndex--;
                _entities[i] = e;
            });

            //Remove the component
            _entities.RemoveAt(componentIndex);

            //Notify the systems that an entity has been modified.
            RefreshSystems(entityId);
        }

        public void RemoveComponents(int entityId, Predicate<ComponentBase> check)
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //Find the indices of the components.
            List<int> componentIndices = new List<int>();
            IterateEntity(entItem, entityId, (i, c) =>
            {
                if (check(c.Component))
                {
                    c.Component.OwnerId = -1;
                    componentIndices.Add(i);
                }
                return false;
            });

            //Sanity check to make sure there are components to be removed.
            if (componentIndices.Count == 0)
            {
                return;
            }

            //If there are entities after this one, decrement their starting indices.
            IterateEntityPointers(entityId + 1, (i, e) =>
            {
                e.EntityIndex -= componentIndices.Count;
                _entities[i] = e;
            });

            //Remove the components.
            foreach (var componentIndex in componentIndices)
            {
                _entities.RemoveAt(componentIndex);
            }

            //Notify the systems that an entity has been modified.
            RefreshSystems(entityId);
        }

        public bool ContainsComponent(int entityId, ComponentBase component)
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //Check if the component exists.
            bool foundComponent = false;
            IterateEntity(entItem, entityId, (i, c) =>
            {
                if (c.Component == component)
                {
                    foundComponent = true;
                    //return true to inform IterateEntity to stop iterating.
                    return true;
                }
                return false;
            });

            //Return the results.
            return foundComponent;
        }

        public bool ContainsComponent<T>(int entityId) where T : ComponentBase
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //Check if the component exists.
            bool foundComponent = false;
            IterateEntity(entItem, entityId, (i, c) =>
            {
                if (c.Component is T)
                {
                    foundComponent = true;
                    //return true to inform IterateEntity to stop iterating.
                    return true;
                }
                return false;
            });

            //Return the results.
            return foundComponent;
        }

        public bool ContainsComponent<T>(int entityId, Predicate<T> check) where T : ComponentBase
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //Check if the component exists.
            bool foundComponent = false;
            IterateEntity(entItem, entityId, (i, c) =>
            {
                if (c.Component is T && check((T)c.Component))
                {
                    foundComponent = true;
                    return true;
                }
                return false;
            });

            //Return the results.
            return foundComponent;
        }

        public T GetComponent<T>(int entityId) where T : ComponentBase
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //Check if the component exists.
            T component = null;
            IterateEntity(entItem, entityId, (i, c) =>
            {
                if (c.Component is T)
                {
                    component = (T)c.Component;
                    return true;
                }
                return false;
            });

            if(component == null)
            {
                throw new InvalidOperationException($"The entity does not contain a component of type {typeof(T)}.");
            }

            //Return the results.
            return component;
        }

        public T GetComponent<T>(int entityId, Predicate<T> check) where T : ComponentBase
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //Check if the component exists.
            T component = null;
            IterateEntity(entItem, entityId, (i, c) =>
            {
                if (c.Component is T && check((T)c.Component))
                {
                    component = (T)c.Component;
                    return true;
                }
                return false;
            });
            

            //Return the results.
            return component;
        }

        public IEnumerable<ComponentBase> GetComponents(int entityId)
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //Iterate until necessary and yield return the components.
            int endIndex = _entities.Count;
            if (IsEntityPointer(entityId + 1, out var nextEnt))
            {
                endIndex = nextEnt.EntityIndex;
            }
            for (int i = entItem.EntityIndex; i < endIndex; i++)
            {
                yield return _entities[i].Component;
            }
        }


        public IEnumerable<ComponentBase> GetComponents(int entityId, Predicate<ComponentBase> check)
        {
            //Sanity check to make sure the passed entityId is valid.
            if (!IsEntityPointer(entityId, out var entItem) || !entItem.IsAlive)
            {
                throw new InvalidOperationException("The specified entity does not exist.");
            }

            //Iterate until necessary and yield return the components.
            int endIndex = _entities.Count;
            if (IsEntityPointer(entityId + 1, out var nextEnt))
            {
                endIndex = nextEnt.EntityIndex;
            }
            for (int i = entItem.EntityIndex; i < endIndex; i++)
            {
                if(check(_entities[i].Component))
                {
                    yield return _entities[i].Component;
                }
            }
        }

        private bool IsEntityPointer(int entityId, out EntityItem entityItem)
        {
            entityItem = EntityItem.Empty;
            if (entityId < 0
                || entityId >= _entities.Count
                || _entities[entityId].IsComponent)
            {
                return false;
            }

            entityItem = _entities[entityId];
            return true;
        }

        private void IterateEntityPointers(int startingId, Action<int, EntityItem> action)
        {
            for (int i = startingId; i < _entities.Count && IsEntityPointer(i, out var item); i++)
            {
                action(i, item);
            }
        }
        
        private void IterateEntity(EntityItem entity, int entityId, Func<int, EntityItem, bool> action)
        {
            int endIndex = _entities.Count;
            if(IsEntityPointer(entityId + 1, out var nextEnt))
            {
                endIndex = nextEnt.EntityIndex;
            }
            for (int i = entity.EntityIndex; i < endIndex; i++)
            {
                if(action(i, _entities[i]))
                {
                    break;
                }
            }
        }

        private struct EntityItem
        {
            public static readonly EntityItem Empty = new EntityItem(-1, null);

            public bool IsAlive;
            public int EntityIndex;
            public ComponentBase Component;
            public bool IsComponent;

            public EntityItem(int entityIndex)
                : this(entityIndex, null)
            {
            }

            public EntityItem(ComponentBase component)
                : this(-1, component)
            {
            }

            private EntityItem(int entityIndex, ComponentBase component)
            {
                EntityIndex = entityIndex;
                Component = component;
                IsComponent = component != null;
                IsAlive = !IsComponent;
            }
        }

            
    }

    
}
