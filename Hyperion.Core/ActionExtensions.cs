using System;

namespace Hyperion.Core
{
    public static class ActionExtensions
    {
        public static void Raise<T>(this Action<T> action, T data)
        {
            if (action != null && data != null)
            {
                action(data);
            }
        }
    }
}
