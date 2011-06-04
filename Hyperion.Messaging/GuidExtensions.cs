using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Hyperion.Messaging
{
    public static class GuidExtensions
    {
        private static int sequentialUuidCounter;

        public static Guid NextUuid(this Guid uuid)
        {
            var ticksAsBytes = BitConverter.GetBytes(DateTime.Now.Ticks);
            Array.Reverse(ticksAsBytes);
            var increment = Interlocked.Increment(ref sequentialUuidCounter);
            var currentAsBytes = BitConverter.GetBytes(increment);
            Array.Reverse(currentAsBytes);
            var bytes = new byte[16];
            Array.Copy(ticksAsBytes, 0, bytes, 0, ticksAsBytes.Length);
            Array.Copy(currentAsBytes, 0, bytes, 12, currentAsBytes.Length);
            return bytes.TransformToGuidWithProperSorting();
        }

        public static Guid TransformToGuidWithProperSorting(this byte[] bytes)
        {
            return new Guid(new[]
            {
                bytes[10],
                bytes[11],
                bytes[12],
                bytes[13],
                bytes[14],
                bytes[15],
                bytes[8],
                bytes[9],
                bytes[6],
                bytes[7],
                bytes[4],
                bytes[5],
                bytes[0],
                bytes[1],
                bytes[2],
                bytes[3],
            });
        }

        public static byte[] TransformToValueForEsentSorting(this Guid guid)
        {
            var bytes = guid.ToByteArray();
            return new[]
            {
                bytes[12],
                bytes[13],
                bytes[14],
                bytes[15],
                bytes[10],
                bytes[11],
                bytes[8],
                bytes[9],
                bytes[6],
                bytes[7],
                bytes[0],
                bytes[1],
                bytes[2],
                bytes[3],
                bytes[4],
                bytes[5],
            };
        }
    }
}
