using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// Container of up to 255 total characters.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 256)]
    public unsafe struct FixedString : IEquatable<FixedString>
    {
        /// <summary>
        /// Maximum number of characters that can be stored.
        /// </summary>
        public const uint Capacity = 255;

        /// <summary>
        /// Maximum value of a single <see cref="char"/>.
        /// </summary>
        public const uint MaxCharacterValue = 256;

        private fixed byte characters[(int)Capacity];
        private byte length;

        /// <summary>
        /// Number of characters in this string.
        /// </summary>
        public byte Length
        {
            readonly get => length;
            set
            {
                ThrowIfLengthExceedsCapacity(value);
                if (value > length)
                {
                    for (uint i = length; i < value; i++)
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
        public char this[uint index]
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
        /// Creates a new fixed string from a <see cref="string"/> value.
        /// </summary>
        public FixedString(string text)
        {
            ThrowIfLengthExceedsCapacity((uint)text.Length);
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
        /// Creates a new fixed string from a <see cref="USpan{T}"/> of characters.
        /// </summary>
        public FixedString(USpan<char> text)
        {
            ThrowIfLengthExceedsCapacity(text.Length);
            length = (byte)text.Length;
            for (uint i = 0; i < length; i++)
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
        /// Creates a new fixed string from a <see cref="USpan{T}"/> of UTF8 bytes.
        /// </summary>
        public FixedString(USpan<byte> utf8Bytes)
        {
            uint index = 0;
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
                    for (uint i = 1; i < byteCount; i++)
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
        /// Creates a new fixed string from a pointer to a null-terminated UTF8 string/bytes.
        /// </summary>
        public FixedString(void* utf8Bytes)
        {
            USpan<byte> span = new(utf8Bytes, Capacity);
            uint index = 0;
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
                    for (uint i = 1; i < byteCount; i++)
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
            USpan<char> buffer = stackalloc char[length];
            CopyTo(buffer);
            return buffer.ToString();
        }

        /// <summary>
        /// Copies the state of another text span.
        /// </summary>
        public void CopyFrom(USpan<char> text)
        {
            ThrowIfLengthExceedsCapacity(text.Length);
            length = 0;
            for (uint i = 0; i < text.Length; i++)
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
        public void CopyFrom(USpan<byte> utf8Bytes)
        {
            uint index = 0;
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
                    for (uint i = 1; i < byteCount; i++)
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
        public readonly uint CopyTo(USpan<byte> utf8Bytes)
        {
            uint byteIndex = 0;
            for (uint i = 0; i < length; i++)
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
        public readonly uint CopyTo(USpan<char> buffer)
        {
            for (uint i = 0; i < length; i++)
            {
                buffer[i] = (char)characters[i];
            }

            return length;
        }

        /// <summary>
        /// Slice this string from the given start index and length.
        /// </summary>
        public readonly FixedString Slice(uint start, uint length)
        {
            ThrowIfIndexOutOfRange(start);
            ThrowIfLengthExceedsCapacity(start + length);

            FixedString result = default;
            for (uint i = 0; i < length; i++)
            {
                result.characters[i] = characters[start + i];
            }

            result.characters[255] = (byte)length;
            return result;
        }

        /// <summary>
        /// Slice this string from the given start index to the end.
        /// </summary>
        public readonly FixedString Slice(uint start)
        {
            return Slice(start, length - start);
        }

        /// <summary>
        /// Checks if this string contains the given character.
        /// </summary>
        public readonly bool Contains(char c)
        {
            for (uint i = 0; i < length; i++)
            {
                if (characters[i] == c)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if this string contains the given text.
        /// </summary>
        public readonly bool Contains(USpan<char> text)
        {
            for (uint i = 0; i < length; i++)
            {
                if (characters[i] == text[0])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
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
        public readonly bool Contains(FixedString text)
        {
            for (uint i = 0; i < length; i++)
            {
                if (characters[i] == text[0])
                {
                    bool found = true;
                    for (uint j = 1; j < text.length; j++)
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
        /// <para>May throw <see cref="ArgumentException"/> if not contained.</para>
        /// </summary>
        public readonly uint IndexOf(char value)
        {
            for (uint i = 0; i < length; i++)
            {
                if (characters[i] == value)
                {
                    return i;
                }
            }

            throw new ArgumentException($"The character {value} was not found in this string");
        }

        /// <summary>
        /// Attempts to retrieve the first index of the given character.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryIndexOf(char value, out uint index)
        {
            for (uint i = 0; i < length; i++)
            {
                if (characters[i] == value)
                {
                    index = i;
                    return true;
                }
            }

            index = 0;
            return false;
        }

        /// <summary>
        /// Retrieves the first index of the given text.
        /// <para>May throw <see cref="ArgumentException"/> if not contained.</para>
        /// </summary>
        public readonly uint IndexOf(USpan<char> text)
        {
            for (uint i = 0; i < length; i++)
            {
                if (characters[i] == text[0])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
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
        public readonly bool TryIndexOf(USpan<char> text, out uint index)
        {
            for (uint i = 0; i < length; i++)
            {
                if (characters[i] == text[0])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
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
        public readonly uint IndexOf(FixedString text)
        {
            for (uint i = 0; i < length; i++)
            {
                if (characters[i] == text[0])
                {
                    bool found = true;
                    for (uint j = 1; j < text.length; j++)
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
        public readonly bool TryIndexOf(FixedString text, out uint index)
        {
            for (uint i = 0; i < length; i++)
            {
                if (characters[i] == text[0])
                {
                    bool found = true;
                    for (uint j = 1; j < text.length; j++)
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
        /// <para>May throw <see cref="ArgumentException"/> if not contained.</para>
        /// </summary>
        public readonly uint LastIndexOf(char value)
        {
            uint thisLength = Length;
            for (uint i = thisLength - 1; i != uint.MaxValue; i--)
            {
                if (characters[i] == value)
                {
                    return i;
                }
            }

            throw new ArgumentException($"The character {value} was not found in this string");
        }

        /// <summary>
        /// Attempts to retrieve the last index of the given character.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryLastIndexOf(char value, out uint index)
        {
            uint thisLength = Length;
            for (uint i = thisLength - 1; i != uint.MaxValue; i--)
            {
                if (characters[i] == value)
                {
                    index = i;
                    return true;
                }
            }

            index = 0;
            return false;
        }

        /// <summary>
        /// Retrieves the last index of the given text.
        /// <para>May throw <see cref="ArgumentException"/> if not contained.</para>
        /// </summary>
        public readonly uint LastIndexOf(USpan<char> text)
        {
            uint thisLength = Length;
            for (uint i = thisLength - 1; i != uint.MaxValue; i--)
            {
                if (characters[i] == text[text.Length - 1])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
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
        public readonly bool TryLastIndexOf(USpan<char> text, out uint index)
        {
            uint thisLength = Length;
            for (uint i = thisLength - 1; i != uint.MaxValue; i--)
            {
                if (characters[i] == text[text.Length - 1])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
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
        public readonly uint LastIndexOf(FixedString text)
        {
            uint thisLength = Length;
            for (uint i = thisLength - 1; i != uint.MaxValue; i--)
            {
                if (characters[i] == text[(byte)(text.Length - 1)])
                {
                    bool found = true;
                    for (uint j = 1; j < text.length; j++)
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
        public readonly bool TryLastIndexOf(FixedString text, out uint index)
        {
            uint thisLength = Length;
            for (uint i = thisLength - 1; i != uint.MaxValue; i--)
            {
                if (characters[i] == text[(byte)(text.Length - 1)])
                {
                    bool found = true;
                    for (uint j = 1; j < text.length; j++)
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
        public readonly bool StartsWith(USpan<char> text)
        {
            if (text.Length > length)
            {
                return false;
            }

            for (uint i = 0; i < text.Length; i++)
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
        public readonly bool StartsWith(FixedString text)
        {
            if (text.length > length)
            {
                return false;
            }

            for (uint i = 0; i < text.length; i++)
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
        public readonly bool EndsWith(USpan<char> text)
        {
            if (text.Length > length)
            {
                return false;
            }

            for (uint i = 0; i < text.Length; i++)
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
        public readonly bool EndsWith(FixedString text)
        {
            if (text.length > length)
            {
                return false;
            }

            for (uint i = 0; i < text.length; i++)
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
            ThrowIfLengthExceedsCapacity(length + 1u);
            characters[length] = (byte)c;
            length = (byte)(length + 1);
        }

        /// <summary>
        /// Appends a formattable object to the end of this string.
        /// </summary>
#if NET
        public void Append<T>(T formattable) where T : ISpanFormattable
        {
            Span<char> buffer = stackalloc char[256];
            formattable.TryFormat(buffer, out int charsWritten, default, default);
            ThrowIfLengthExceedsCapacity(length + (uint)charsWritten);
            for (uint i = 0; i < charsWritten; i++)
            {
                characters[length + i] = (byte)buffer[(int)i];
            }

            length = (byte)(length + charsWritten);
        }
#else
        public void Append<T>(T formattable) where T : IFormattable
        {
            string text = formattable.ToString(default, default);
            ThrowIfLengthExceedsCapacity(length + (uint)text.Length);
            for (uint i = 0; i < text.Length; i++)
            {
                characters[length + i] = (byte)text[(int)i];
            }

            length = (byte)(length + text.Length);
        }
#endif

        /// <summary>
        /// Appends a text span to the end of this string.
        /// </summary>
        public void Append(USpan<char> text)
        {
            ThrowIfLengthExceedsCapacity(length + text.Length);
            for (uint i = 0; i < text.Length; i++)
            {
                characters[length + i] = (byte)text[i];
            }

            length = (byte)(length + text.Length);
        }

        /// <summary>
        /// Appends a text span to the end of this string.
        /// </summary>
        public void Append(FixedString text)
        {
            uint textLength = text.Length;
            ThrowIfLengthExceedsCapacity(length + textLength);
            for (uint i = 0; i < textLength; i++)
            {
                characters[length + i] = (byte)text[i];
            }

            length = (byte)(length + textLength);
        }

        /// <summary>
        /// Removes the character at the given index.
        /// </summary>
        public void RemoveAt(uint index)
        {
            ThrowIfIndexOutOfRange(index);

            for (uint i = index; i < length - 1; i++)
            {
                characters[i] = characters[i + 1];
            }

            length = (byte)(length - 1);
        }

        /// <summary>
        /// Removes a range of characters starting at the given index.
        /// </summary>
        public void RemoveRange(uint start, uint length)
        {
            uint thisLength = Length;
            ThrowIfIndexOutOfRange(start);
            ThrowIfLengthExceedsCapacity(start + length);
            for (uint i = start; i < thisLength - length; i++)
            {
                characters[i] = characters[i + length];
            }

            this.length = (byte)(thisLength - length);
        }

        /// <summary>
        /// Inserts a character at the given index.
        /// </summary>
        public void Insert(uint index, char c)
        {
            ThrowIfLengthExceedsCapacity(length + 1u);
            ThrowIfIndexOutOfRange(index);
            for (uint i = length; i > index; i--)
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
        public void Insert<T>(uint index, T formattable) where T : ISpanFormattable
        {
            Span<char> buffer = stackalloc char[256];
            formattable.TryFormat(buffer, out int charsWritten, default, default);
            ThrowIfLengthExceedsCapacity(length + (uint)charsWritten);
            ThrowIfIndexOutOfRange(index);
            for (uint i = length; i > index; i--)
            {
                characters[i + charsWritten - 1] = characters[i - 1];
            }

            for (uint i = 0; i < charsWritten; i++)
            {
                characters[index + i] = (byte)buffer[(int)i];
            }

            length = (byte)(length + charsWritten);
        }
#else
        public void Insert<T>(uint index, T formattable) where T : IFormattable
        {
            string text = formattable.ToString(default, default);
            ThrowIfLengthExceedsCapacity(length + (uint)text.Length);
            ThrowIfIndexOutOfRange(index);
            for (uint i = length; i > index; i--)
            {
                characters[i + text.Length - 1] = characters[i - 1];
            }

            for (uint i = 0; i < text.Length; i++)
            {
                characters[index + i] = (byte)text[(int)i];
            }

            length = (byte)(length + text.Length);
        }
#endif

        /// <summary>
        /// Inserts a text span at the given index.
        /// </summary>
        public void Insert(uint index, USpan<char> text)
        {
            ThrowIfLengthExceedsCapacity(length + text.Length);
            ThrowIfIndexOutOfRange(index);
            for (uint i = length; i > index; i--)
            {
                characters[i + text.Length - 1] = characters[i - 1];
            }

            for (uint i = 0; i < text.Length; i++)
            {
                characters[index + i] = (byte)text[i];
            }

            length = (byte)(length + text.Length);
        }

        /// <summary>
        /// Inserts a text span at the given index.
        /// </summary>
        public void Insert(uint index, FixedString text)
        {
            uint textLength = text.Length;
            ThrowIfLengthExceedsCapacity(length + textLength);
            for (uint i = length; i > index; i--)
            {
                characters[i + textLength - 1] = characters[i - 1];
            }

            for (uint i = 0; i < textLength; i++)
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
            for (uint i = 0; i < length; i++)
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
        public bool TryReplace(USpan<char> oldValue, USpan<char> newValue)
        {
            for (uint i = 0; i < length; i++)
            {
                if (characters[i] == oldValue[0])
                {
                    bool found = true;
                    for (uint j = 1; j < oldValue.Length; j++)
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
                            for (uint j = 0; j < newValue.Length; j++)
                            {
                                characters[i + j] = (byte)newValue[j];
                            }
                        }
                        else
                        {
                            uint difference = newValue.Length - oldValue.Length;
                            if (difference > 0)
                            {
                                ThrowIfLengthExceedsCapacity(length + difference);
                            }

                            for (uint j = length; j > i + oldValue.Length; j--)
                            {
                                characters[j + difference - 1] = characters[j - 1];
                            }

                            for (uint j = 0; j < newValue.Length; j++)
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
        public bool TryReplace(FixedString oldValue, FixedString newValue)
        {
            for (uint i = 0; i < length; i++)
            {
                if (characters[i] == oldValue[0])
                {
                    bool found = true;
                    for (uint j = 1; j < oldValue.length; j++)
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
                            for (uint j = 0; j < newValue.length; j++)
                            {
                                characters[i + j] = (byte)newValue[j];
                            }
                        }
                        else
                        {
                            int difference = newValue.length - oldValue.length;
                            if (difference > 0)
                            {
                                ThrowIfLengthExceedsCapacity((uint)(length + difference));
                            }

                            for (uint j = length; j > i + oldValue.length; j--)
                            {
                                characters[j + difference - 1] = characters[j - 1];
                            }

                            for (uint j = 0; j < newValue.length; j++)
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
        public readonly bool Equals(FixedString other)
        {
            if (length != other.length)
            {
                return false;
            }

            for (uint i = 0; i < length; i++)
            {
                if (characters[i] != other[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public readonly bool Equals(USpan<char> other)
        {
            if (length != other.Length)
            {
                return false;
            }

            for (uint i = 0; i < length; i++)
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
            return obj is FixedString other && Equals(other);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                for (uint i = 0; i < length; i++)
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
        private readonly void ThrowIfIndexOutOfRange(uint index)
        {
            if (index >= length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range of {Length}");
            }
        }

        /// <summary>
        /// Only in debug, throws if the given length exceeds the capacity.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfLengthExceedsCapacity(uint length)
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
        public static implicit operator FixedString(string text)
        {
            return new(text);
        }

        /// <inheritdoc/>
        public static bool operator ==(FixedString a, FixedString b)
        {
            return a.Equals(b);
        }

        /// <inheritdoc/>
        public static bool operator !=(FixedString a, FixedString b)
        {
            return !a.Equals(b);
        }
    }
}
