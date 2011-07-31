using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Hyperion.Core.WebSockets
{
    // TODO remove parsing and just create validation by bytes input
    public class ServerHandshake
    {
        private const string DefaultCode = "101";
        private const string DefaultUpgrade = "WebSocket";
        private const string DefaultConnection = "Upgrade";
        private const string DefaultOrigin = "null";
        private const string HttpText = "HTTP/1.1 ";
        private const string SpaceCharacter = " ";
        private const char Seperator = ':';

        private readonly IDictionary<string, Action<ServerHandshake, string>> settersByFieldName = new Dictionary<string, Action<ServerHandshake, string>> 
        {
            {"upgrade", (handshake, x) => handshake.Upgrade = x},
            {"connection", (handshake, x) => handshake.Connection = x},
            {"sec-websocket-location", (handshake, x) => handshake.Location = x},
            {"sec-websocket-origin", (handshake, x) => handshake.Origin = x},
            {"sec-websocket-protocol", (handshake, x) => handshake.Subprotocol = x},
            {"set-cookie", (handshake, x) => 
                {
                    handshake.Cookies = new HttpCookieCollection();
                    var cookies = x.Split(';');
                    foreach (var cookie in cookies)
                    {
                        var equalsIndex = cookie.IndexOf('=');
                        handshake.Cookies.Add(new HttpCookie(cookie.Substring(0, equalsIndex).TrimStart(),
                             cookie.Substring(equalsIndex + 1)));
                    }
                }}  
        };

        public ServerHandshake()
        {
        }

        public ServerHandshake(string origin, string location, string subprotocol, string key1, string key2, byte[] key3)
        {
            Upgrade = DefaultUpgrade;
            Connection = DefaultConnection;
            Origin = origin;
            Location = location;
            Subprotocol = subprotocol;
            Response = GenerateResponse(key1, key2, key3);
        }

        public string Code { get; set; }
        public string Upgrade { get; set; }
        public string Connection { get; set; }
        public string Origin { get; set; }
        public string Location { get; set; }
        public string Subprotocol { get; set; }
        public HttpCookieCollection Cookies { get; set; }
        public byte[] Response { get; set; }
        public Dictionary<string, string> ExtraFields { get; set; }

        public byte[] GenerateResponse(string key1, string key2, byte[] key3)
        {
            var challenge = new List<byte>(16);
            challenge.AddRange(GetBigEndianBytes(GeneratePart(key1)));
            challenge.AddRange(GetBigEndianBytes(GeneratePart(key2)));
            challenge.AddRange(key3);

            return MD5.Create().ComputeHash(challenge.ToArray());
        }

        private uint GeneratePart(string key)
        {
            var keyNumber = long.Parse(new string(key.Where(char.IsNumber).ToArray()));
            var spaces = key.Count(char.IsWhiteSpace); // TODO if spaces is zero, abort connection
            var part = (uint)(keyNumber / spaces); // TODO if part is < 1, abort connection

            return part;
        }

        private byte[] GetBigEndianBytes(uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        public bool IsValid(string location, string origin, string subProtocol, byte[] expected)
        {
            return Code == DefaultCode &&
                   Upgrade == DefaultUpgrade &&
                   string.Compare(Connection, DefaultConnection, true) == 0 &&
                   !ExtraFields.ContainsKey(string.Empty) &&
                   Location != null &&
                   Location == location &&
                   Origin != null &&
                   (origin == DefaultOrigin || Origin == origin) &&
                   Subprotocol == subProtocol &&
                   Response.EqualsArray(expected);
        }

        private void SetProperty(string lineInHandshake)
        {
            if (lineInHandshake.Length < 1)
            {
                return;
            }
            if (lineInHandshake.StartsWith(HttpText))
            {
                var indexOfSecondSpace = lineInHandshake.IndexOf(SpaceCharacter, HttpText.Length);
                Code = lineInHandshake.Substring(HttpText.Length, indexOfSecondSpace - HttpText.Length);
            }
            else
            {
                var seperatorIndex = lineInHandshake.IndexOf(Seperator);
                if (seperatorIndex > -1)
                {
                    // TODO there should only be one of each
                    var valueStartIndex = seperatorIndex + 2;
                    var fieldName = lineInHandshake.Substring(0, seperatorIndex);
                    var fieldValue = lineInHandshake.Substring(valueStartIndex);
                    var fieldNameLowerCase = fieldName.ToLower();
                    if (settersByFieldName.ContainsKey(fieldNameLowerCase))
                    {
                        settersByFieldName[fieldNameLowerCase](this, fieldValue);
                    }
                    else
                    {
                        ExtraFields[fieldNameLowerCase] = fieldValue;
                    }
                }
                //else
                //{
                //    var bytes = Encoding.UTF8.GetBytes(lineInHandshake.ToCharArray(), 0, 16);
                //    Response = Encoding.ASCII.GetString(bytes);
                //}
            }
        }

        public void Parse(byte[] bytes, int index, int count)
        {
            ExtraFields = new Dictionary<string, string>();

            using (var memoryStream = new MemoryStream(bytes, index, count))
            using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8))
            {
                while (!streamReader.EndOfStream)
                {
                    SetProperty(streamReader.ReadLine());
                }
            }

            Response = new byte[16];
            Array.Copy(bytes, count - 16, Response, 0, 16);
        }

        /// <summary>
        /// Handshake in bytes
        /// </summary>
        public byte[] ToByteArray()
        {
            var bytes = Encoding.UTF8.GetBytes(ToString());
            var byteList = new List<byte>(bytes.Length + 8);
            byteList.AddRange(bytes);
            byteList.AddRange(Response);
            return byteList.ToArray();
        }

        /// <summary>
        /// Create text message of the handshake without response
        /// </summary>
        public override string ToString()
        {
            var message = string.Concat("HTTP/1.1 101 WebSocket Protocol Handshake\r\n",
                                        "Upgrade: ", Upgrade, "\r\n",
                                        "Connection: ", Connection, "\r\n",
                                        "Sec-WebSocket-Location: ", Location, "\r\n",
                                        "Sec-WebSocket-Origin: ", Origin, "\r\n");
            if (Subprotocol != null && !Subprotocol.Equals("null"))
            {
                message = string.Concat(message, "Sec-WebSocket-Protocol: ", Subprotocol, "\r\n");
            }
            if (Cookies != null)
            {
                message = string.Concat(message, "Set-Cookie: ", Cookies.ToString(), "\r\n");
            }
            if (ExtraFields != null)
            {
                message = ExtraFields.Aggregate(message, (current, field) =>
                    string.Concat(current, field.Key, ": ", field.Value, "\r\n"));
            }
            message += "\r\n";

            return message;
        }
    }
}