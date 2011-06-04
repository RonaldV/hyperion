using System;
using System.Net.Sockets;

namespace Hyperion.Core
{
    public static class SocketExtensions
    {
        public static void EndConnect(this Socket socket, IAsyncResult asyncResult, Action<Exception> log)
        {
            try
            {
                socket.EndConnect(asyncResult);
            }
            catch (ObjectDisposedException ex)
            {
                log(ex);
            }
            catch (SocketException ex)
            {
                log(ex);
            }
        }

        public static int EndReceive(this Socket socket, IAsyncResult asyncResult, Action<Exception> log)
        {
            try
            {
                return socket.EndReceive(asyncResult);
            }
            catch (ObjectDisposedException ex)
            {
                log(ex);
                return -1;
            }
            catch (SocketException ex)
            {
                log(ex);
                return -1;
            }
        }

        public static int SafeEndReceive(this Socket socket, IAsyncResult asyncResult)
        {
            try
            {
                return socket.EndReceive(asyncResult);
            }
            catch (ObjectDisposedException)
            {
                return -1;
            }
            catch (SocketException)
            {
                return -1;
            }
        }

        public static Socket SafeEndAccept(this Socket socket, IAsyncResult asyncResult)
        {
            try
            {
                return socket.EndAccept(asyncResult);
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            catch (SocketException)
            {
                return null;
            }
        }
    }
}
