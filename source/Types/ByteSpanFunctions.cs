﻿using System;

//todo: move this into a serialization lib?
public static class ByteSpanFunctions
{
    /// <summary>
    /// Peeks the next UTF-8 character in the stream.
    /// </summary>
    /// <returns>Amount of bytes read.</returns>
    public static byte PeekUTF8(this ReadOnlySpan<byte> bytes, uint position, out char low, out char high)
    {
        high = default;
        byte firstByte = bytes[(int)position];
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
            byte next = bytes[(int)(position + j)];
            if ((next & 0xC0) != 0x80)
            {
                throw new FormatException("Invalid UTF-8 continuation byte");
            }

            codePoint = (codePoint << 6) | (next & 0x3F);
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

        additional++;
        return additional;
    }

    /// <summary>
    /// Reads a UTF-8 span of characters into the provided buffer.
    /// </summary>
    /// <returns>Amount of character values copied.</returns>
    public static int PeekUTF8Span(this ReadOnlySpan<byte> bytes, uint start, uint length, Span<char> buffer)
    {
        int t = 0;
        if (bytes.Length < length)
        {
            length = (uint)bytes.Length;
        }

        uint i = 0;
        while (i < length)
        {
            uint cLength = PeekUTF8(bytes, start + i, out char low, out char high);
            if (low == default)
            {
                break;
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
}