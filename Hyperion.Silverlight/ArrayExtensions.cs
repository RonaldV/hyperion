namespace Hyperion.Silverlight
{
    public static class ArrayExtensions
    {
        public static int IndexOf<T>(this T[] array, T value)
            where T : System.IEquatable<T>
        {
            var length = array.Length;
            for (var i = 0; i < length; i++)
            {
                if (array[i].Equals(value))
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool EqualsArray<T>(this T[] array, T[] value)
            where T : System.IEquatable<T>
        {
            if (ReferenceEquals(array, value))
            {
                return true;
            }
            if (array == null || value == null)
            {
                return false;
            }
            if (array.Length != value.Length)
            {
                return false;
            }

            var length = array.Length;
            for (var i = 0; i < length; i++)
            {
                if (!array[i].Equals(value[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
