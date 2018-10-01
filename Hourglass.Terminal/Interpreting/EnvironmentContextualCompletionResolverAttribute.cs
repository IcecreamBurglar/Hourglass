using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hourglass.Terminal.Interpreting
{
    public class EnvironmentContextualCompletionResolverAttribute : Attribute
    {
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
        public sealed class EnvironmentContextualCompletionResolver : Attribute
        {
            public EnvironmentContextualCompletionResolver()
            {
            }
        }
    }
}
