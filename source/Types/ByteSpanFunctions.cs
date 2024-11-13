using System;

namespace Unmanaged
{
    //todo: move this into a serialization lib? maybe into `data`? but BinaryReader and writer depend on these :(
    /// <summary>
    /// Extension functions for <see cref="USpan{T}"/> of bytes.
    /// </summary>
    public static class ByteSpanFunctions
    {
        /// <summary>
        /// Peeks the next UTF-8 character in the stream.
        /// </summary>
        /// <returns>Amount of bytes read.</returns>
        public static byte PeekUTF8(this USpan<byte> bytes, uint position, out char low, out char high)
        {
            high = default;
            byte firstByte = bytes[position];
            int codePoint;
            byte additional;
            if ((firstByte & 0x80) == 0)
            {
                additional = 0;
                codePoint = firstByte;
            }
            else if ((firstByte & 0xE0) == 0xC0)
            {
                additional = 1;
                codePoint = firstByte & 0x1F;
            }
            else if ((firstByte & 0xF0) == 0xE0)
            {
                additional = 2;
                codePoint = firstByte & 0x0F;
            }
            else if ((firstByte & 0xF8) == 0xF0)
            {
                additional = 3;
                codePoint = firstByte & 0x07;
            }
            else
            {
                throw new FormatException("Invalid UTF-8 byte sequence");
            }

            for (uint j = 1; j <= additional; j++)
            {
                byte next = bytes[position + j];
                if ((next & 0xC0) != 0x80)
                {
                    throw new FormatException("Invalid UTF-8 continuation byte");
                }

                codePoint = codePoint << 6 | next & 0x3F;
            }

            if (codePoint <= 0xFFFF)
            {
                low = (char)codePoint;
            }
            else
            {
                codePoint -= 0x10000;
                high = (char)((codePoint >> 10) + 0xD800);
                low = (char)((codePoint & 0x3FF) + 0xDC00);
            }

            return (byte)(additional + 1);
        }

        /// <summary>
        /// Reads the bytes formatted as UTF8 into the given character buffer.
        /// <para>Reads <paramref name="length"/> amount of characters, or
        /// until a <c>default</c> character is found (included in the buffer).</para>
        /// </summary>
        /// <returns>Amount of character values copied.</returns>
        public static uint PeekUTF8Span(this USpan<byte> bytes, uint start, uint length, USpan<char> buffer)
        {
            uint t = 0;
            uint i = 0;
            if (length > bytes.Length)
            {
                length = bytes.Length;
            }

            while (i < length)
            {
                uint cLength = bytes.PeekUTF8(start + i, out char low, out char high);
                if (low == default)
                {
                    buffer[t++] = default;
                    return t;
                }

                if (high != default)
                {
                    buffer[t++] = high;
                    buffer[t++] = low;
                }
                else
                {
                    buffer[t++] = low;
                }

                i += cLength;
            }

            return t;
        }

        /// <summary>
        /// Reads how long the UTF-8 text is by counting characters
        /// until the terminator.
        /// </summary>
        public static uint GetUTF8Length(this USpan<byte> bytes)
        {
            uint position = 0;
            while (position < bytes.Length)
            {
                byte next = bytes[position];
                if (next == default)
                {
                    break;
                }

                position += bytes.PeekUTF8(position, out _, out _);
            }

            return position;
        }
    }
}