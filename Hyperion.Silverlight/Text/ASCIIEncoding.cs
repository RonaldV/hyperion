using System.Text;
using System;

namespace Hyperion.Silverlight.Text
{
    public class ASCIIEncoding : Encoding
    {
        public override int GetByteCount(char[] chars, int index, int count)
        {
            if (chars == null)
            {
                throw new ArgumentNullException("chars");
            }
            if (index < 0 || index > chars.Length)
            {
                throw new ArgumentOutOfRangeException("index", "chars array");
            }
            if (count < 0 || count > (chars.Length - index))
            {
                throw new ArgumentOutOfRangeException("count", "chars array");
            }
            return count;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            EncoderFallbackBuffer buffer = null;
            char[] fallback_chars = null;

            return GetBytes(chars, charIndex, charCount, bytes, byteIndex,
                     ref buffer, ref fallback_chars);
        }

        private int GetBytes(char[] chars, int charIndex, int charCount,
                  byte[] bytes, int byteIndex,
                  ref EncoderFallbackBuffer buffer,
                  ref char[] fallback_chars)
        {
            if (chars == null)
                throw new ArgumentNullException("chars");

            unsafe
            {
                fixed (char* cptr = chars)
                {
                    return InternalGetBytes(cptr, chars.Length, charIndex, charCount, bytes, byteIndex, ref buffer, ref fallback_chars);
                }
            }
        }

        unsafe int InternalGetBytes(char* chars, int charLength, int charIndex, int charCount,
                  byte[] bytes, int byteIndex,
                  ref EncoderFallbackBuffer buffer,
                  ref char[] fallback_chars)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (charIndex < 0 || charIndex > charLength)
                throw new ArgumentOutOfRangeException("charIndex", "ArgRange_StringIndex");
            if (charCount < 0 || charCount > (charLength - charIndex))
                throw new ArgumentOutOfRangeException("charCount", "ArgRange_StringRange");
            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException("byteIndex", "ArgRange_Array");
            if ((bytes.Length - byteIndex) < charCount)
                throw new ArgumentException("Arg_InsufficientSpace");

            int count = charCount;
            char ch;
            while (count-- > 0)
            {
                ch = chars[charIndex++];
                if (ch < (char)0x80)
                {
                    bytes[byteIndex++] = (byte)ch;
                }
                else
                {
                    if (buffer == null)
                        buffer = EncoderFallback.CreateFallbackBuffer();
                    if (Char.IsSurrogate(ch) && count > 1 &&
                        Char.IsSurrogate(chars[charIndex]))
                        buffer.Fallback(ch, chars[charIndex], charIndex++ - 1);
                    else
                        buffer.Fallback(ch, charIndex - 1);
                    if (fallback_chars == null || fallback_chars.Length < buffer.Remaining)
                        fallback_chars = new char[buffer.Remaining];
                    for (int i = 0; i < fallback_chars.Length; i++)
                        fallback_chars[i] = buffer.GetNextChar();
                    byteIndex += GetBytes(fallback_chars, 0,
                        fallback_chars.Length, bytes, byteIndex,
                        ref buffer, ref fallback_chars);
                }
            }
            return charCount;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (index < 0 || index > bytes.Length)
            {
                throw new ArgumentOutOfRangeException("index", "ArgRange_Array");
            }
            if (count < 0 || count > (bytes.Length - index))
            {
                throw new ArgumentOutOfRangeException("count", "ArgRange_Array");
            }
            return count;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            DecoderFallbackBuffer buffer = null;
            return GetChars(bytes, byteIndex, byteCount, chars,
                charIndex, ref buffer);
        }

        private int GetChars(byte[] bytes, int byteIndex, int byteCount,
                  char[] chars, int charIndex,
                  ref DecoderFallbackBuffer buffer)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (chars == null)
                throw new ArgumentNullException("chars");
            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException("byteIndex", "ArgRange_Array");
            if (byteCount < 0 || byteCount > (bytes.Length - byteIndex))
                throw new ArgumentOutOfRangeException("byteCount", "ArgRange_Array");
            if (charIndex < 0 || charIndex > chars.Length)
                throw new ArgumentOutOfRangeException("charIndex", "ArgRange_Array");

            if ((chars.Length - charIndex) < byteCount)
                throw new ArgumentException("Arg_InsufficientSpace");

            int count = byteCount;
            while (count-- > 0)
            {
                char c = (char)bytes[byteIndex++];
                if (c < '\x80')
                    chars[charIndex++] = c;
                else
                {
                    if (buffer == null)
                        buffer = DecoderFallback.CreateFallbackBuffer();
                    buffer.Fallback(bytes, byteIndex);
                    while (buffer.Remaining > 0)
                        chars[charIndex++] = buffer.GetNextChar();
                }
            }
            return byteCount;
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
            {
                throw new ArgumentOutOfRangeException("charCount", "charCount can not be negative");
            }
            return charCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException("byteCount", "byteCount can not be negative");
            }
            return byteCount;
        }
    }
}
