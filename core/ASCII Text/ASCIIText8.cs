using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// Container of up to 7 extended ASCII <see cref="char"/> values.
    /// </summary>
    [SkipLocalsInit]
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public unsafe struct ASCIIText8 : IEquatable<ASCIIText8>
    {
        /// <summary>
        /// Maximum number of characters that can be stored.
        /// </summary>
        public const int Capacity = 7;

        /// <summary>
        /// Maximum value of a single <see cref="char"/>.
        /// </summary>
        public const char MaxCharacterValue = (char)256;

        private fixed byte characters[Capacity];
        private byte length;

        /// <summary>
        /// Number of characters in this string.
        /// </summary>
        public int Length
        {
            readonly get => length;
            set
            {
                ThrowIfLengthExceedsCapacity(value);

                if (value > length)
                {
                    for (int i = length; i < value; i++)
                    {
                        characters[i] = 0;
                    }
                }

                length = (byte)value;
            }
        }

        /// <summary>
        /// Accesses a character in this string.
        /// </summary>
        public char this[int index]
        {
            readonly get
            {
                ThrowIfIndexOutOfRange(index);

                return (char)characters[index];
            }
            set
            {
                ThrowIfIndexOutOfRange(index);
                ThrowIfCharacterIsOutOfRange(value);

                characters[index] = (byte)value;
            }
        }

        /// <summary>
        /// Creates a new instance from the given <paramref name="text"/>.
        /// </summary>
        public ASCIIText8(string text)
        {
            ThrowIfLengthExceedsCapacity(text.Length);

            length = (byte)text.Length;
            for (int i = 0; i < length; i++)
            {
                char c = text[i];
                if (c == '\0')
                {
                    break;
                }

                characters[i] = (byte)c;
            }
        }

        /// <summary>
        /// Creates a new instance from the given <paramref name="text"/>.
        /// </summary>
        public ASCIIText8(ReadOnlySpan<char> text)
        {
            ThrowIfLengthExceedsCapacity(text.Length);

            length = (byte)text.Length;
            for (int i = 0; i < length; i++)
            {
                char c = text[i];
                if (c == '\0')
                {
                    break;
                }

                characters[i] = (byte)c;
            }
        }

        /// <summary>
        /// Creates a new instance from the given <paramref name="text"/> collection.
        /// </summary>
        public ASCIIText8(IEnumerable<char> text)
        {
            int index = 0;
            foreach (char c in text)
            {
                if (c == '\0')
                {
                    break;
                }

                characters[index] = (byte)c;
                index++;

                ThrowIfLengthExceedsCapacity(index);
            }

            length = (byte)index;
        }

        /// <summary>
        /// Creates a new instance from the given <paramref name="utf8Bytes"/>.
        /// </summary>
        public ASCIIText8(ReadOnlySpan<byte> utf8Bytes)
        {
            int index = 0;
            while (index < utf8Bytes.Length)
            {
                byte firstByte = utf8Bytes[index];
                byte byteCount;
                if ((firstByte & 0x80) == 0)
                {
                    byteCount = 1;
                }
                else if ((firstByte & 0xE0) == 0xC0)
                {
                    byteCount = 2;
                }
                else if ((firstByte & 0xF0) == 0xE0)
                {
                    byteCount = 3;
                }
                else if ((firstByte & 0xF8) == 0xF0)
                {
                    byteCount = 4;
                }
                else
                {
                    throw new ArgumentException("Invalid UTF-8 byte sequence");
                }

                int codePoint;
                if (byteCount == 1)
                {
                    codePoint = firstByte;
                }
                else
                {
                    codePoint = firstByte & (0xFF >> (byteCount + 1));
                    for (int i = 1; i < byteCount; i++)
                    {
                        byte b = utf8Bytes[index + i];
                        if ((b & 0xC0) != 0x80)
                        {
                            throw new ArgumentException("Invalid UTF-8 byte sequence");
                        }

                        codePoint = (codePoint << 6) | (b & 0x3F);
                    }
                }

                char value = (char)codePoint;
                if (value == '\0')
                {
                    break;
                }
                else
                {
                    ThrowIfCharacterIsOutOfRange(value);
                    characters[index] = (byte)value;
                    index++;
                    ThrowIfLengthExceedsCapacity(index);
                }
            }

            length = (byte)index;
        }

        /// <summary>
        /// Creates a new instance from the given <paramref name="utf8Bytes"/> pointer.
        /// <para>
        /// Reads until reaching a null terminator, or 7 characters.
        /// </para>
        /// </summary>
        public ASCIIText8(void* utf8Bytes)
        {
            ReadOnlySpan<byte> span = new(utf8Bytes, Capacity);
            int index = 0;
            while (index < span.Length)
            {
                byte firstByte = span[index];
                byte byteCount;
                if ((firstByte & 0x80) == 0)
                {
                    byteCount = 1;
                }
                else if ((firstByte & 0xE0) == 0xC0)
                {
                    byteCount = 2;
                }
                else if ((firstByte & 0xF0) == 0xE0)
                {
                    byteCount = 3;
                }
                else if ((firstByte & 0xF8) == 0xF0)
                {
                    byteCount = 4;
                }
                else
                {
                    throw new ArgumentException("Invalid UTF-8 byte sequence");
                }

                int codePoint;
                if (byteCount == 1)
                {
                    codePoint = firstByte;
                }
                else
                {
                    codePoint = firstByte & (0xFF >> (byteCount + 1));
                    for (int i = 1; i < byteCount; i++)
                    {
                        byte b = span[index + i];
                        if ((b & 0xC0) != 0x80)
                        {
                            throw new ArgumentException("Invalid UTF-8 byte sequence");
                        }

                        codePoint = (codePoint << 6) | (b & 0x3F);
                    }
                }

                char value = (char)codePoint;
                if (value == '\0')
                {
                    break;
                }
                else
                {
                    ThrowIfCharacterIsOutOfRange(value);
                    characters[index] = (byte)value;
                    index++;
                    ThrowIfLengthExceedsCapacity(index);
                }
            }

            length = (byte)index;
        }

        /// <summary>
        /// Retrieves the <see cref="string"/> representation of this string.
        /// </summary>
        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[length];
            CopyTo(buffer);
            return buffer.ToString();
        }

        /// <summary>
        /// Copies the state of another text span.
        /// </summary>
        public void CopyFrom(ReadOnlySpan<char> text)
        {
            ThrowIfLengthExceedsCapacity(text.Length);

            length = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\0')
                {
                    break;
                }

                characters[i] = (byte)c;
                length++;
            }
        }

        /// <summary>
        /// Copies the state from a UTF8 byte span.
        /// </summary>
        public void CopyFrom(ReadOnlySpan<byte> utf8Bytes)
        {
            int index = 0;
            while (index < utf8Bytes.Length)
            {
                byte firstByte = utf8Bytes[index];
                byte byteCount;
                if ((firstByte & 0x80) == 0)
                {
                    byteCount = 1;
                }
                else if ((firstByte & 0xE0) == 0xC0)
                {
                    byteCount = 2;
                }
                else if ((firstByte & 0xF0) == 0xE0)
                {
                    byteCount = 3;
                }
                else if ((firstByte & 0xF8) == 0xF0)
                {
                    byteCount = 4;
                }
                else
                {
                    throw new ArgumentException("Invalid UTF-8 byte sequence");
                }

                int codePoint;
                if (byteCount == 1)
                {
                    codePoint = firstByte;
                }
                else
                {
                    codePoint = firstByte & (0xFF >> (byteCount + 1));
                    for (int i = 1; i < byteCount; i++)
                    {
                        byte b = utf8Bytes[index + i];
                        if ((b & 0xC0) != 0x80)
                        {
                            throw new ArgumentException("Invalid UTF-8 byte sequence");
                        }

                        codePoint = (codePoint << 6) | (b & 0x3F);
                    }
                }

                char value = (char)codePoint;
                if (value == '\0')
                {
                    break;
                }
                else
                {
                    ThrowIfCharacterIsOutOfRange(value);
                    characters[index] = (byte)value;
                    index++;
                    ThrowIfLengthExceedsCapacity(index);
                }
            }

            length = (byte)index;
        }

        /// <summary>
        /// Copies the state of this string to the given buffer of UTF8 bytes.
        /// </summary>
        /// <returns>Amount of <see cref="byte"/>s copied.</returns>
        public readonly int CopyTo(Span<byte> utf8Bytes)
        {
            int byteIndex = 0;
            for (int i = 0; i < length; i++)
            {
                char c = (char)characters[i];
                if (c <= 0x7F)
                {
                    utf8Bytes[byteIndex++] = (byte)c;
                }
                else if (c <= 0x7FF)
                {
                    utf8Bytes[byteIndex++] = (byte)(0xC0 | (c >> 6)); //first 5 bits
                    utf8Bytes[byteIndex++] = (byte)(0x80 | (c & 0x3F)); //last 6 bits
                }
            }

            return byteIndex;
        }

        /// <summary>
        /// Copies the state of this string to the given buffer of characters.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public readonly int CopyTo(Span<char> buffer)
        {
            for (int i = 0; i < length; i++)
            {
                buffer[i] = (char)characters[i];
            }

            return length;
        }

        /// <summary>
        /// Slice this string from the given start index and length.
        /// </summary>
        public readonly ASCIIText8 Slice(int start, int length)
        {
            ThrowIfIndexOutOfRange(start);
            ThrowIfLengthExceedsCapacity(start + length);

            ASCIIText8 result = default;
            for (int i = 0; i < length; i++)
            {
                result.characters[i] = characters[start + i];
            }

            result.length = (byte)length;
            return result;
        }

        /// <summary>
        /// Slice this string from the given start index to the end.
        /// </summary>
        public readonly ASCIIText8 Slice(int start)
        {
            return Slice(start, length - start);
        }

        /// <summary>
        /// Checks if this string contains the given character.
        /// </summary>
        public readonly bool Contains(char c)
        {
            fixed (byte* ptr = characters)
            {
                ReadOnlySpan<byte> span = new(ptr, length);
                return span.Contains((byte)c);
            }
        }

        /// <summary>
        /// Checks if this string contains the given text.
        /// </summary>
        public readonly bool Contains(ReadOnlySpan<char> text)
        {
            for (int i = 0; i < length; i++)
            {
                if (characters[i] == text[0])
                {
                    bool found = true;
                    for (int j = 1; j < text.Length; j++)
                    {
                        if (i + j >= length || characters[i + j] != text[j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if this string contains the given text.
        /// </summary>
        public readonly bool Contains(ASCIIText8 text)
        {
            for (int i = 0; i < length; i++)
            {
                if (characters[i] == text[0])
                {
                    bool found = true;
                    for (int j = 1; j < text.length; j++)
                    {
                        if (i + j >= length || characters[i + j] != text[j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves the first index of the given character.
        /// </summary>
        public readonly int IndexOf(char value)
        {
            fixed (byte* ptr = characters)
            {
                ReadOnlySpan<byte> span = new(ptr, length);
                return span.IndexOf((byte)value);
            }
        }

        /// <summary>
        /// Attempts to retrieve the first index of the given character.
        /// </summary>
        /// <returns><see langword="true"/> if found.</returns>
        public readonly bool TryIndexOf(char value, out int index)
        {
            fixed (byte* ptr = characters)
            {
                ReadOnlySpan<byte> span = new(ptr, length);
                index = span.IndexOf((byte)value);
                return index != -1;
            }
        }

        /// <summary>
        /// Retrieves the first index of the given text.
        /// <para>May throw <see cref="ArgumentException"/> if not contained.</para>
        /// </summary>
        public readonly int IndexOf(ReadOnlySpan<char> text)
        {
            for (int i = 0; i < length; i++)
            {
                if (characters[i] == text[0])
                {
                    bool found = true;
                    for (int j = 1; j < text.Length; j++)
                    {
                        if (i + j >= length || characters[i + j] != text[j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        return i;
                    }
                }
            }

            throw new ArgumentException($"The text {text.ToString()} was not found in this string");
        }

        /// <summary>
        /// Attempts to retrieve the first index of the given text.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryIndexOf(ReadOnlySpan<char> text, out int index)
        {
            for (int i = 0; i < length; i++)
            {
                if (characters[i] == text[0])
                {
                    bool found = true;
                    for (int j = 1; j < text.Length; j++)
                    {
                        if (i + j >= length || characters[i + j] != text[j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        index = i;
                        return true;
                    }
                }
            }

            index = 0;
            return false;
        }

        /// <summary>
        /// Retrieves the first index of the given text.
        /// <para>May throw <see cref="ArgumentException"/> if not contained.</para>
        /// </summary>
        public readonly int IndexOf(ASCIIText8 text)
        {
            for (int i = 0; i < length; i++)
            {
                if (characters[i] == text[0])
                {
                    bool found = true;
                    for (int j = 1; j < text.length; j++)
                    {
                        if (i + j >= length || characters[i + j] != text[j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        return i;
                    }
                }
            }

            throw new ArgumentException($"The text {text.ToString()} was not found in this string");
        }

        /// <summary>
        /// Attempts to retrieve the first index of the given text.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryIndexOf(ASCIIText8 text, out int index)
        {
            for (int i = 0; i < length; i++)
            {
                if (characters[i] == text[0])
                {
                    bool found = true;
                    for (int j = 1; j < text.length; j++)
                    {
                        if (i + j >= length || characters[i + j] != text[j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        index = i;
                        return true;
                    }
                }
            }

            index = 0;
            return false;
        }

        /// <summary>
        /// Retrieves the last index of the given character.
        /// </summary>
        public readonly int LastIndexOf(char value)
        {
            fixed (byte* ptr = characters)
            {
                ReadOnlySpan<byte> span = new(ptr, length);
                return span.LastIndexOf((byte)value);
            }
        }

        /// <summary>
        /// Attempts to retrieve the last index of the given character.
        /// </summary>
        /// <returns><see langword="true"/> if found.</returns>
        public readonly bool TryLastIndexOf(char value, out int index)
        {
            fixed (byte* ptr = characters)
            {
                ReadOnlySpan<byte> span = new(ptr, length);
                index = span.LastIndexOf((byte)value);
                return index != -1;
            }
        }

        /// <summary>
        /// Retrieves the last index of the given text.
        /// <para>May throw <see cref="ArgumentException"/> if not contained.</para>
        /// </summary>
        public readonly int LastIndexOf(ReadOnlySpan<char> text)
        {
            int thisLength = Length;
            for (int i = thisLength - 1; i >= 0; i--)
            {
                if (characters[i] == text[text.Length - 1])
                {
                    bool found = true;
                    for (int j = 1; j < text.Length; j++)
                    {
                        if (i - j < 0 || characters[i - j] != text[text.Length - j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        return i - text.Length + 1;
                    }
                }
            }

            throw new ArgumentException($"The text {text.ToString()} was not found in this string");
        }

        /// <summary>
        /// Attempts to retrieve the last index of the given text.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryLastIndexOf(ReadOnlySpan<char> text, out int index)
        {
            int thisLength = Length;
            for (int i = thisLength - 1; i >= 0; i--)
            {
                if (characters[i] == text[text.Length - 1])
                {
                    bool found = true;
                    for (int j = 1; j < text.Length; j++)
                    {
                        if (i - j < 0 || characters[i - j] != text[text.Length - j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        index = i - text.Length + 1;
                        return true;
                    }
                }
            }

            index = 0;
            return false;
        }

        /// <summary>
        /// Retrieves the last index of the given text.
        /// <para>May throw <see cref="ArgumentException"/> if not contained.</para>
        /// </summary>
        public readonly int LastIndexOf(ASCIIText8 text)
        {
            int thisLength = Length;
            for (int i = thisLength - 1; i >= 0; i--)
            {
                if (characters[i] == text[text.Length - 1])
                {
                    bool found = true;
                    for (int j = 1; j < text.length; j++)
                    {
                        if (i - j < 0 || characters[i - j] != text[text.length - j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        return i - text.length + 1;
                    }
                }
            }

            throw new ArgumentException($"The text {text.ToString()} was not found in this string");
        }

        /// <summary>
        /// Attempts to retrieve the last index of the given text.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryLastIndexOf(ASCIIText8 text, out int index)
        {
            int thisLength = Length;
            for (int i = thisLength - 1; i >= 0; i--)
            {
                if (characters[i] == text[text.Length - 1])
                {
                    bool found = true;
                    for (int j = 1; j < text.length; j++)
                    {
                        if (i - j < 0 || characters[i - j] != text[text.length - j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        index = i - text.length + 1;
                        return true;
                    }
                }
            }

            index = 0;
            return false;
        }

        /// <summary>
        /// Checks if this string starts with the given text.
        /// </summary>
        public readonly bool StartsWith(ReadOnlySpan<char> text)
        {
            if (text.Length > length)
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (characters[i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if this string starts with the given text.
        /// </summary>
        public readonly bool StartsWith(ASCIIText8 text)
        {
            if (text.length > length)
            {
                return false;
            }

            for (int i = 0; i < text.length; i++)
            {
                if (characters[i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if this string ends with the given text.
        /// </summary>
        public readonly bool EndsWith(ReadOnlySpan<char> text)
        {
            if (text.Length > length)
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (characters[length - text.Length + i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if this string ends with the given text.
        /// </summary>
        public readonly bool EndsWith(ASCIIText8 text)
        {
            if (text.length > length)
            {
                return false;
            }

            for (int i = 0; i < text.length; i++)
            {
                if (characters[length - text.length + i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Clears the contents of this string.
        /// </summary>
        public void Clear()
        {
            length = 0;
        }

        /// <summary>
        /// Appends a character to the end of this string.
        /// </summary>
        public void Append(char c)
        {
            ThrowIfLengthExceedsCapacity(length + 1);

            characters[length] = (byte)c;
            length = (byte)(length + 1);
        }

        /// <summary>
        /// Appends a formattable object to the end of this string.
        /// </summary>
#if NET
        public void Append<T>(T formattable) where T : ISpanFormattable
        {
            Span<char> buffer = stackalloc char[Capacity];
            formattable.TryFormat(buffer, out int charsWritten, default, default);
            ThrowIfLengthExceedsCapacity(length + charsWritten);

            for (int i = 0; i < charsWritten; i++)
            {
                characters[length + i] = (byte)buffer[i];
            }

            length = (byte)(length + charsWritten);
        }
#else
        public void Append<T>(T formattable) where T : IFormattable
        {
            string text = formattable.ToString(default, default);
            ThrowIfLengthExceedsCapacity(length + text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                characters[length + i] = (byte)text[i];
            }

            length = (byte)(length + text.Length);
        }
#endif

        /// <summary>
        /// Appends a text span to the end of this string.
        /// </summary>
        public void Append(ReadOnlySpan<char> text)
        {
            ThrowIfLengthExceedsCapacity(length + text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                characters[length + i] = (byte)text[i];
            }

            length = (byte)(length + text.Length);
        }

        /// <summary>
        /// Appends a text span to the end of this string.
        /// </summary>
        public void Append(ASCIIText8 text)
        {
            int textLength = text.Length;
            ThrowIfLengthExceedsCapacity(length + textLength);

            for (int i = 0; i < textLength; i++)
            {
                characters[length + i] = (byte)text[i];
            }

            length = (byte)(length + textLength);
        }

        /// <summary>
        /// Removes the character at the given index.
        /// </summary>
        public void RemoveAt(int index)
        {
            ThrowIfIndexOutOfRange(index);

            for (int i = index; i < length - 1; i++)
            {
                characters[i] = characters[i + 1];
            }

            length = (byte)(length - 1);
        }

        /// <summary>
        /// Removes a range of characters starting at the given index.
        /// </summary>
        public void RemoveRange(int start, int length)
        {
            int thisLength = Length;
            ThrowIfIndexOutOfRange(start);
            ThrowIfLengthExceedsCapacity(start + length);

            for (int i = start; i < thisLength - length; i++)
            {
                characters[i] = characters[i + length];
            }

            this.length = (byte)(thisLength - length);
        }

        /// <summary>
        /// Inserts a character at the given index.
        /// </summary>
        public void Insert(int index, char c)
        {
            ThrowIfLengthExceedsCapacity(length + 1);
            ThrowIfIndexIsPastRange(index);

            for (int i = length; i > index; i--)
            {
                characters[i] = characters[i - 1];
            }

            characters[index] = (byte)c;
            length = (byte)(length + 1);
        }

        /// <summary>
        /// Inserts a formattable object at the given index.
        /// </summary>
#if NET
        public void Insert<T>(int index, T formattable) where T : ISpanFormattable
        {
            Span<char> buffer = stackalloc char[Capacity];
            formattable.TryFormat(buffer, out int charsWritten, default, default);
            ThrowIfLengthExceedsCapacity(length + charsWritten);
            ThrowIfIndexIsPastRange(index);

            for (int i = length; i > index; i--)
            {
                characters[i + charsWritten - 1] = characters[i - 1];
            }

            for (int i = 0; i < charsWritten; i++)
            {
                characters[index + i] = (byte)buffer[i];
            }

            length = (byte)(length + charsWritten);
        }
#else
        public void Insert<T>(int index, T formattable) where T : IFormattable
        {
            string text = formattable.ToString(default, default);
            ThrowIfLengthExceedsCapacity(length + text.Length);
            ThrowIfIndexIsPastRange(index);

            for (int i = length; i > index; i--)
            {
                characters[i + text.Length - 1] = characters[i - 1];
            }

            for (int i = 0; i < text.Length; i++)
            {
                characters[index + i] = (byte)text[i];
            }

            length = (byte)(length + text.Length);
        }
#endif

        /// <summary>
        /// Inserts a text span at the given index.
        /// </summary>
        public void Insert(int index, ReadOnlySpan<char> text)
        {
            ThrowIfLengthExceedsCapacity(length + text.Length);
            ThrowIfIndexIsPastRange(index);

            for (int i = length; i > index; i--)
            {
                characters[i + text.Length - 1] = characters[i - 1];
            }

            for (int i = 0; i < text.Length; i++)
            {
                characters[index + i] = (byte)text[i];
            }

            length = (byte)(length + text.Length);
        }

        /// <summary>
        /// Inserts a text span at the given index.
        /// </summary>
        public void Insert(int index, ASCIIText8 text)
        {
            int textLength = text.Length;
            ThrowIfLengthExceedsCapacity(length + textLength);

            for (int i = length; i > index; i--)
            {
                characters[i + textLength - 1] = characters[i - 1];
            }

            for (int i = 0; i < textLength; i++)
            {
                characters[index + i] = (byte)text[i];
            }

            length = (byte)(length + textLength);
        }

        /// <summary>
        /// Replaces all instances of the given character with another.
        /// </summary>
        /// <returns><c>true</c> if a replacement was done.</returns>
        public bool TryReplace(char oldValue, char newValue)
        {
            bool done = false;
            for (int i = 0; i < length; i++)
            {
                ref byte c = ref characters[i];
                if (c == oldValue)
                {
                    c = (byte)newValue;
                    done |= true;
                }
            }

            return done;
        }

        /// <summary>
        /// Attempts to replace all instances of the given text with another.
        /// </summary>
        /// <returns><c>true</c> if a replacement was done.</returns>
        public bool TryReplace(ReadOnlySpan<char> oldValue, ReadOnlySpan<char> newValue)
        {
            for (int i = 0; i < length; i++)
            {
                if (characters[i] == oldValue[0])
                {
                    bool found = true;
                    for (int j = 1; j < oldValue.Length; j++)
                    {
                        if (i + j >= length || characters[i + j] != oldValue[j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        if (oldValue.Length == newValue.Length)
                        {
                            for (int j = 0; j < newValue.Length; j++)
                            {
                                characters[i + j] = (byte)newValue[j];
                            }
                        }
                        else
                        {
                            int difference = newValue.Length - oldValue.Length;
                            if (difference > 0)
                            {
                                ThrowIfLengthExceedsCapacity(length + difference);
                            }

                            for (int j = length; j > i + oldValue.Length; j--)
                            {
                                characters[j + difference - 1] = characters[j - 1];
                            }

                            for (int j = 0; j < newValue.Length; j++)
                            {
                                characters[i + j] = (byte)newValue[j];
                            }

                            length = (byte)(length + difference);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to replace all instances of the given text with another.
        /// </summary>
        /// <returns><c>true</c> if a replacement was done.</returns>
        public bool TryReplace(ASCIIText8 oldValue, ASCIIText8 newValue)
        {
            for (int i = 0; i < length; i++)
            {
                if (characters[i] == oldValue[0])
                {
                    bool found = true;
                    for (int j = 1; j < oldValue.length; j++)
                    {
                        if (i + j >= length || characters[i + j] != oldValue[j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        if (oldValue.length == newValue.length)
                        {
                            for (int j = 0; j < newValue.length; j++)
                            {
                                characters[i + j] = (byte)newValue[j];
                            }
                        }
                        else
                        {
                            int difference = newValue.length - oldValue.length;
                            if (difference > 0)
                            {
                                ThrowIfLengthExceedsCapacity((length + difference));
                            }

                            for (int j = length; j > i + oldValue.length; j--)
                            {
                                characters[j + difference - 1] = characters[j - 1];
                            }

                            for (int j = 0; j < newValue.length; j++)
                            {
                                characters[i + j] = (byte)newValue[j];
                            }

                            length = (byte)(length + difference);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public readonly bool Equals(ASCIIText8 other)
        {
            if (length != other.length)
            {
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                if (characters[i] != other[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public readonly bool Equals(string other)
        {
            if (length != other.Length)
            {
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                if (characters[i] != other[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public readonly bool Equals(ReadOnlySpan<char> other)
        {
            if (length != other.Length)
            {
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                if (characters[i] != other[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public readonly override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is ASCIIText8 other && Equals(other);
        }

        /// <summary>
        /// Hash code based on contents and the length.
        /// </summary>
        public readonly override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < length; i++)
                {
                    hash = (hash * 31) + characters[i];
                }

                return hash;
            }
        }

        /// <summary>
        /// Only in debug, throws if the given index is out of range.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        [Conditional("DEBUG")]
        private readonly void ThrowIfIndexOutOfRange(int index)
        {
            if (index >= length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range of {Length}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfIndexIsPastRange(int index)
        {
            if (index > length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range of {Length}");
            }
        }

        /// <summary>
        /// Only in debug, throws if the given length exceeds the capacity.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfLengthExceedsCapacity(int length)
        {
            if (length > Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"Length exceeds the maximum of {Capacity}");
            }
        }

        /// <summary>
        /// Only in debug, throws if the given character is out of range.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfCharacterIsOutOfRange(char c)
        {
            if (c >= MaxCharacterValue)
            {
                throw new ArgumentOutOfRangeException(nameof(c), $"Character value exceeds the maximum of {MaxCharacterValue}");
            }
        }

        /// <inheritdoc/>
        public static implicit operator ASCIIText8(string text)
        {
            return new(text);
        }

        /// <inheritdoc/>
        public static implicit operator ASCIIText8(Span<char> text)
        {
            return new(text);
        }

        /// <inheritdoc/>
        public static implicit operator ASCIIText8(ReadOnlySpan<char> text)
        {
            return new(text);
        }



        /// <inheritdoc/>
        public static explicit operator ASCIIText16(ASCIIText8 value)
        {
            Span<char> span = stackalloc char[value.Length];
            value.CopyTo(span);
            return new ASCIIText16(span);
        }

        /// <inheritdoc/>
        public static explicit operator ASCIIText32(ASCIIText8 value)
        {
            Span<char> span = stackalloc char[value.Length];
            value.CopyTo(span);
            return new ASCIIText32(span);
        }

        /// <inheritdoc/>
        public static explicit operator ASCIIText64(ASCIIText8 value)
        {
            Span<char> span = stackalloc char[value.Length];
            value.CopyTo(span);
            return new ASCIIText64(span);
        }

        /// <inheritdoc/>
        public static explicit operator ASCIIText128(ASCIIText8 value)
        {
            Span<char> span = stackalloc char[value.Length];
            value.CopyTo(span);
            return new ASCIIText128(span);
        }

        /// <inheritdoc/>
        public static explicit operator ASCIIText256(ASCIIText8 value)
        {
            Span<char> span = stackalloc char[value.Length];
            value.CopyTo(span);
            return new ASCIIText256(span);
        }

        /// <inheritdoc/>
        public static explicit operator ASCIIText512(ASCIIText8 value)
        {
            Span<char> span = stackalloc char[value.Length];
            value.CopyTo(span);
            return new ASCIIText512(span);
        }

        /// <inheritdoc/>
        public static explicit operator ASCIIText1024(ASCIIText8 value)
        {
            Span<char> span = stackalloc char[value.Length];
            value.CopyTo(span);
            return new ASCIIText1024(span);
        }

        /// <inheritdoc/>
        public static bool operator ==(ASCIIText8 a, ASCIIText8 b)
        {
            return a.Equals(b);
        }

        /// <inheritdoc/>
        public static bool operator !=(ASCIIText8 a, ASCIIText8 b)
        {
            return !a.Equals(b);
        }
    }
}