using System;
using System.IO;

namespace Hyperion.Core
{
    public static class StreamExtensions
    {
        public static int EndRead(this Stream stream, IAsyncResult asyncResult, Action<Exception> log)
        {
            try
            {
                return stream.EndRead(asyncResult);
            }
            catch (IOException ex)
            {
                log(ex);
                return -1;
            }
        }

        public static void EndWrite(this Stream stream, IAsyncResult asyncResult, Action<Exception> log)
        {
            try
            {
                stream.EndWrite(asyncResult);
            }
            catch (IOException ex)
            {
                log(ex);
            }
        }
    }
}
