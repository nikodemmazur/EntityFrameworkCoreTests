using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace EntityFrameworkCoreTests.TestHelpers
{
    public abstract class TestClassBase
    {
        public MethodInfo[] ListFactMethods()
        {
            var type = GetType();
            var typeInfo = type.GetTypeInfo();
            var methods = typeInfo.GetMethods().Where(mi => mi.GetCustomAttributes<FactAttribute>().Count() > 0);
            return methods.ToArray();
        }

        public string[] ListFactMethodNames() =>
            ListFactMethods().Select(mi => mi.Name).ToArray();

        /// <summary>
        /// Returns the caller name.
        /// </summary>
        /// <param name="stackDepth">Value 0 indicates the direct caller of this method.</param>
        /// <returns></returns>
        public string GetCallerName(int stackDepth = 0)
        {
            var stackTrace = new StackTrace();
            var method = stackTrace.GetFrame(stackDepth + 1)?.GetMethod()
                ?? throw new InvalidOperationException("Cannot obtain info about the caller.");
            return method.Name;
        }
    }
}
