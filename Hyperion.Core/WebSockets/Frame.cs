using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hyperion.Core.WebSockets
{
    // TODO handle HasError, send WebSocket closing handshake
    public class Frame
    {
        private const byte OpeningFrameType = 0x00;
        private const byte ClosingFrameType = 0xFF;
        private readonly List<byte> frameBytes;

        public Frame(string data)
        {
            IsClosed = true;
            HasError = false;
            frameBytes = Encoding.UTF8.GetBytes(data).ToList();
        }

        public Frame()
        {
            IsClosed = false;
            HasError = false;
            frameBytes = new List<byte>();
        }

        public bool IsClosed { get; private set; }
        public bool HasError { get; private set; }

        //public void Add(byte[] bytes)
        //{
        //    var hasClosingFrameType = false;

        //    var closingFrameIndex = bytes.IndexOf(ClosingFrameType);
        //    if (closingFrameIndex < 0)
        //    {
        //        closingFrameIndex = bytes.Length;
        //    }
        //    else
        //    {
        //        hasClosingFrameType = true;
        //    }

        //    var openingFrameIndex = bytes.IndexOf(OpeningFrameType);
        //    if (openingFrameIndex < 0 || closingFrameIndex < openingFrameIndex)
        //    {
        //        openingFrameIndex = 0;
        //    }
        //    else
        //    {
        //        // Remove existing bytes from the frame
        //        // when an opening frame type has been found
        //        frameBytes.Clear();
        //        openingFrameIndex++;
        //    }

        //    for (var i = openingFrameIndex; i < closingFrameIndex; i++)
        //    {
        //        frameBytes.Add(bytes[i]);
        //    }

        //    IsClosed = hasClosingFrameType;
        //}

        //public void Add(byte[] bytes)
        //{
        //    var frameType = bytes[0];
        //    if ((frameType & 0x80) == 0x80)
        //    {
        //        // NOTE length could be larger the 32bit
        //        var i = 0;
        //        var length = 0;
        //        byte b = 0;
        //        do
        //        {
        //            i++;
        //            b = bytes[i];
        //            int bV = b & 0x7F;
        //            length = (length * 128) + bV;
        //        } while ((b & 0x80) == 0x80);
        //        // Discard length bytes
        //        i += length;
        //        if (frameType == 0xFF && length == 0)
        //        {
        //            // TODO send WebSocket closing handshake
        //            HasError = true;
        //        }
        //    }
        //    else if ((frameType & 0x80) == 0x00)
        //    {
        //        if (frameType == OpeningFrameType)
        //        {
        //            AddBytes(bytes, 1);
        //        }
        //        else
        //        {
        //            // NOTE This does not work with adding to an existing frame
        //            HasError = true;
        //        }
        //    }
        //    else 
        //    {
        //        // NOTE There should allready be bytes added at this point
        //        AddBytes(bytes, 0);
        //    }
        //}

        public void Add(byte[] bytes)
        {
            var frameType = bytes[0];
            if ((frameType & 0x80) == 0x80)
            {
                // At server If /type/ is not a 0xFF byte. Abort connection.

                // NOTE Not sure why this is in the protocol for. This seems useless.
                // NOTE length could be larger then 32bit and overflow
                // NOTE Why is the data framing specification different on the client and server? It should be the same.
                var i = 0;
                var length = 0;
                byte b = 0;
                do
                {
                    i++;
                    b = bytes[i]; // At server If /b/ is not a 0x00 byte... do the following steps
                    int bV = b & 0x7F;
                    length = (length * 128) + bV;
                } while ((b & 0x80) == 0x80);
                // Discard length bytes
                i += length;
                if (frameType == 0xFF && length == 0)
                {
                    HasError = true;
                }
            }
            else if (frameType == OpeningFrameType)
            {
                Add(bytes, 1);
            }
            else if (frameBytes.Count > 0)
            {
                Add(bytes, 0);
            }
            else
            {
                HasError = true;
            }
        }

        private void Add(byte[] bytes, int startIndex)
        {
            var hasClosingFrameType = false;
            var closingFrameIndex = bytes.IndexOf(ClosingFrameType);
            if (closingFrameIndex < 0)
            {
                closingFrameIndex = bytes.Length;
            }
            else
            {
                hasClosingFrameType = true;
            }

            for (var i = startIndex; i < closingFrameIndex; i++)
            {
                frameBytes.Add(bytes[i]);
            }

            IsClosed = hasClosingFrameType;
        }

        public byte[] ToByteArray()
        {
            frameBytes.Insert(0, OpeningFrameType);
            frameBytes.Add(ClosingFrameType);
            return frameBytes.ToArray();
        }

        public string ToContentString()
        {
            return Encoding.UTF8.GetString(frameBytes.ToArray(), 0, frameBytes.Count);
        }
    }
}
