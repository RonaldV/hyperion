﻿using System;

namespace Hyperion.Silverlight.WebSockets
{
    public static class UriExtensions
    {
        private const string PortDelimiter = ":";

        public static int WebSocketPort(this Uri uri)
        {
            if (uri.Port > 0)
            {
                return uri.Port;
            }
            if (uri.Scheme.Equals(UriWeb.UriSchemeWs))
            {
                return 80;
            }
            if (uri.Scheme.Equals(UriWeb.UriSchemeWss))
            {
                return 443;
            }
            return -1;
        }

        public static string WebSocketAuthority(this Uri uri)
        {
            if (uri.Port > -1 &&
                uri.Port != 80 &&
                uri.Port != 443)
            {
                // When not using port 80 or 443 default ports 
                // return host:port
                return string.Concat(uri.DnsSafeHost, PortDelimiter, uri.Port);
            }
            return uri.DnsSafeHost;
        }
    }
}
