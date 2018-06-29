using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hourglass.Ecs
{
    [Serializable]
    public class EntityException : Exception
    {
        public int EntityId { get; private set; }

        public EntityException(int entityId)
            : this(entityId, $"An unexpected error with entity {entityId:X4}.", null)
        {
        }

        public EntityException(int entityId, string message)
            : this(entityId, message, null)
        {
        }

        public EntityException(int entityId, string message, Exception inner) : base(message, inner)
        {
            EntityId = entityId;
        }
    }
}
