using System;
using System.Reflection;

namespace winPEAS.Helpers
{
    internal static class ReflectionHelper
    {
        public static object InvokeMemberMethod(object target, string name, object[] args = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            object result = InvokeMember(target, name, BindingFlags.InvokeMethod, args);

            return result;
        }

        public static object InvokeMemberProperty(object target, string name, object[] args = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            object result = InvokeMember(target, name, BindingFlags.GetProperty, args);

            return result;
        }

        private static object InvokeMember(object target, string name, BindingFlags invokeAttr, object[] args = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            object result = target.GetType().InvokeMember(name, invokeAttr, null, target, args);

            return result;
        }
    }
}
