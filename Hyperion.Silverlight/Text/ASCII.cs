using System.Collections.Generic;
using System;

namespace Hyperion.Silverlight.Text
{
    public class ASCII
    {
        /// <summary>
        /// Very simple string to ASCII byte array conversion
        /// The chars need to be in the 0 to 127 ASCII range
        /// </summary>
        /// <param name="s">String to convert in ASCII bytes</param>
        /// <returns>ASCII byte array</returns>
        public static byte[] GetBytes(string s)
        {
            var chars = s.ToCharArray();
            var bytes = new byte[chars.Length];
            var i = 0;
            foreach (var c in chars)
            {
                bytes[i] = (byte)c;
                i++;
            }

            return bytes;
            //var bytes = new List<byte>();
            //foreach (var c in s)
            //{
            //    Convert.T
            //}

            //return bytes.ToArray();
        }
    }
}
