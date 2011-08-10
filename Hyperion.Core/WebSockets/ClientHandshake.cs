using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using SharpNeatLib.Maths;

namespace Hyperion.Core.WebSockets
{
    // TODO add proxy Proxy-authorization
    public class ClientHandshake
    {
        private const string DefaultUpgrade = "WebSocket";
        private const string DefaultConnection = "Upgrade";
        public const string DefaultOrigin = "null";
        private const string GetText = "GET ";
        private const string SpaceCharacter = " ";
        private const char Seperator = ':';
        private readonly IDictionary<string, Action<ClientHandshake, string>> settersByFieldName = new Dictionary<string, Action<ClientHandshake, string>> 
        {
            {"upgrade", (handshake, x) => handshake.Upgrade = x},
            {"connection", (handshake, x) => handshake.Connection = x},
            {"host", (handshake, x) => handshake.Host = x},
            {"origin", (handshake, x) => handshake.Origin = x},
            {"sec-websocket-protocol", (handshake, x) => handshake.Subprotocol = x},
            {"sec-websocket-key1", (handshake, x) => handshake.Key1 = x},
            {"sec-websocket-key2", (handshake, x) => handshake.Key2 = x},
            {"cookie", (handshake, x) => 
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

        public ClientHandshake()
        {
        }

        public ClientHandshake(string resourceName, string host, string origin)
        {
            ResourceName = resourceName;
            Upgrade = DefaultUpgrade;
            Connection = DefaultConnection;
            Host = host;
            Origin = origin;

            var random = new FastRandom(DateTime.UtcNow.Second);
            Key1 = GenerateKey1And2(random);
            Key2 = GenerateKey1And2(random);
            Key3 = GenerateKey3(random);
        }

        public string ResourceName { get; set; }
        public string Upgrade { get; set; }
        public string Connection { get; set; }
        public string Host { get; set; }
        public string Origin { get; set; }
        /// <summary>
        /// Only needed when using subprotocol
        /// </summary>
        public string Subprotocol { get; set; }
        public HttpCookieCollection Cookies { get; set; }
        public string Key1 { get; set; }
        public string Key2 { get; set; }
        public byte[] Key3 { get; set; }
        public IDictionary<string, string> ExtraFields { get; set; }

        private string GenerateKey1And2(FastRandom random)
        {
            // Changed random.Next(1, 13) to random.Next(2, 13) else max would be too large
            var spaces = random.Next(2, 13);
            var max = random.NextUInt() / spaces;
            var number = random.Next(0, (int)max + 1);
            var product = number * spaces;
            var key = product.ToString();

            var amountOfCharacters = random.Next(1, 13);
            var amountFirstSeries = amountOfCharacters / 2;
            var amountSecondSeries = amountOfCharacters - amountFirstSeries;
            var firstSeries = new List<string>();
            for (var i = 0; i < amountFirstSeries; i++)
            {
                // First unicode series
                var unicodeChar = char.ConvertFromUtf32(random.Next(21, 48));
                firstSeries.Add(unicodeChar);
            }
            var secondSeries = new List<string>();
            for (var i = 0; i < amountSecondSeries; i++)
            {
                // Second unicode series
                var unicodeChar = char.ConvertFromUtf32(random.Next(58, 126));
                secondSeries.Add(unicodeChar);
            }

            firstSeries.ForEach(unicodeCharacter => key = InsertAtRandomLocation(key, unicodeCharacter, random));
            secondSeries.ForEach(unicodeCharacter => key = InsertAtRandomLocation(key, unicodeCharacter, random));

            // Insert random spaces
            for (var i = 0; i < spaces; i++)
            {
                var location = random.Next(1, key.Length - 1);
                key = key.Insert(location, SpaceCharacter);
            }

            return key;
        }

        private static string InsertAtRandomLocation(string key, string unicodeCharacter, FastRandom random)
        {
            var location = random.Next(0, key.Length);
            return key.Insert(location, unicodeCharacter);
        }

        //private static string GenerateKey3(FastRandom random)
        //{
        //    var key3ByteList = new List<byte>();
        //    key3ByteList.AddRange(BitConverter.GetBytes(random.NextUInt()));
        //    key3ByteList.AddRange(BitConverter.GetBytes(random.NextUInt()));
        //    var key3Bytes = key3ByteList.ToArray();
        //    if (BitConverter.IsLittleEndian)
        //    {
        //        Array.Reverse(key3Bytes);
        //    }

        //    return Encoding.ASCII.GetString(key3Bytes);
        //}

        private static byte[] GenerateKey3(FastRandom random)
        {
            var key = string.Empty;
            const int count = 8;
            for (var i = 0; i < count; i++)
            {
                key += char.ConvertFromUtf32(random.Next(21, 126));
            }

            return Encoding.ASCII.GetBytes(key);
        }

        public bool IsValid(string host, string origin)
        {
            return !string.IsNullOrEmpty(ResourceName) &&
                   string.Compare(Upgrade, DefaultUpgrade, true) == 0 &&
                   string.Compare(Connection, DefaultConnection, true) == 0 &&
                   !string.IsNullOrEmpty(Host) &&
                   Host == host &&
                   !string.IsNullOrEmpty(Origin) &&
                   (origin == DefaultOrigin || Origin == origin) &&
                   !string.IsNullOrEmpty(Key1) &&
                   !string.IsNullOrEmpty(Key2) &&
                   Key3 != null && 
                   Key3.Length == 8;
        }

        private void SetProperty(string lineInHandshake)
        {
            if (lineInHandshake.Length < 1)
            {
                return;
            }
            if (lineInHandshake.StartsWith(GetText))
            {
                var indexOfSecondSpace = lineInHandshake.IndexOf(SpaceCharacter, GetText.Length);
                if (indexOfSecondSpace > -1)
                {
                    ResourceName = lineInHandshake.Substring(GetText.Length, indexOfSecondSpace - GetText.Length);
                }
            }
            else
            {
                var seperatorIndex = lineInHandshake.IndexOf(Seperator);
                if (seperatorIndex > -1)
                {
                    var valueStartIndex = seperatorIndex + 2;
                    var fieldName = lineInHandshake.Substring(0, seperatorIndex);
                    var fieldValue = lineInHandshake.Substring(valueStartIndex);
                    var fieldNameLowerCase = fieldName.ToLower();
                    if (settersByFieldName.ContainsKey(fieldNameLowerCase))
                    {
                        settersByFieldName[fieldNameLowerCase](this, fieldValue);
                    }
                    else if (!string.IsNullOrEmpty(fieldName))
                    {
                        ExtraFields[fieldName] = fieldValue;
                    }
                }
                //else
                //{
                //    var bytes = Encoding.UTF8.GetBytes(lineInHandshake.ToCharArray(), 0, 8);
                //    Key3 = Encoding.ASCII.GetString(bytes);
                //    //Key3 = lineInHandshake.Substring(0, 8);
                //}
            }
        }

        //public void Parse(byte[] bytes, int index, int count)
        //{
        //    OtherHeaderFields = new Dictionary<string, string>();

        //    using (var memoryStream = new MemoryStream(bytes, index, count - 8))
        //    using (var binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
        //    {
        //        while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
        //        {
                    
        //        }
        //    }

        //    Key3 = new byte[8];
        //    Array.Copy(bytes, count - 8, Key3, 0, 8);
        //}

        public void Parse(byte[] bytes, int index, int count)
        {
            if (count < 8)
            {
                return;
            }

            ExtraFields = new Dictionary<string, string>();

            using (var memoryStream = new MemoryStream(bytes, index, count - 8))
            using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8))
            {
                while (!streamReader.EndOfStream)
                {
                    SetProperty(streamReader.ReadLine());
                }
            }

            Key3 = new byte[8];
            Array.Copy(bytes, count - 8, Key3, 0, 8);
        }

        /// <summary>
        /// Handshake in bytes
        /// </summary>
        public byte[] ToByteArray()
        {
            var bytes = Encoding.UTF8.GetBytes(ToString());
            var byteList = new List<byte>(bytes.Length + 8);
            byteList.AddRange(bytes);
            byteList.AddRange(Key3);
            return byteList.ToArray();
        }

        /// <summary>
        /// Create text message of the handshake without key3
        /// </summary>
        public override string ToString()
        {
            var message = string.Concat("GET ", ResourceName, " HTTP/1.1\r\n",
                                        "Upgrade: ", Upgrade, "\r\n",
                                        "Connection: ", Connection, "\r\n",
                                        "Host: ", Host, "\r\n",
                                        "Origin: ", Origin, "\r\n",
                                        "Sec-WebSocket-Key1: ", Key1, "\r\n",
                                        "Sec-WebSocket-Key2: ", Key2, "\r\n");

            if (Subprotocol != null)
            {
                message = string.Concat(message, "Sec-WebSocket-Protocol: ", Subprotocol, "\r\n");
            }
            if (Cookies != null)
            {
                message = string.Concat(message, "Cookie: ", Cookies.ToString(), "\r\n");
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