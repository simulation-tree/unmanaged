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
        public const uint Capacity = 255;
        public const uint MaxCharacterValue = 256;

        private fixed byte characters[(int)Capacity];
        private byte length;

        public uint Length
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

        public FixedString(string text)
        {
            CopyFrom(text.AsUSpan());
        }

        public FixedString(USpan<char> text)
        {
            CopyFrom(text);
        }

        public FixedString(USpan<byte> utf8Bytes)
        {
            CopyFrom(utf8Bytes);
        }

        public FixedString(void* utf8Bytes)
        {
            USpan<byte> span = new(utf8Bytes, Capacity);
            CopyFrom(span);
        }

        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[(int)length];
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
                    throw new ArgumentException("Invalid UTF-8 byte sequence.");
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
                            throw new ArgumentException("Invalid UTF-8 byte sequence.");
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

        public readonly uint CopyTo(USpan<char> buffer)
        {
            for (uint i = 0; i < length; i++)
            {
                buffer[i] = (char)characters[i];
            }

            return length;
        }

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

        public readonly FixedString Slice(uint start)
        {
            return Slice(start, length - start);
        }

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

        public readonly uint IndexOf(char value)
        {
            for (uint i = 0; i < length; i++)
            {
                if (characters[i] == value)
                {
                    return i;
                }
            }

            throw new IndexOutOfRangeException();
        }

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

            throw new IndexOutOfRangeException();
        }

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

            throw new IndexOutOfRangeException();
        }

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

            throw new IndexOutOfRangeException();
        }

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

            throw new IndexOutOfRangeException();
        }

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

        public readonly uint LastIndexOf(FixedString text)
        {
            uint thisLength = Length;
            for (uint i = thisLength - 1; i != uint.MaxValue; i--)
            {
                if (characters[i] == text[text.Length - 1])
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

            throw new IndexOutOfRangeException();
        }

        public readonly bool TryLastIndexOf(FixedString text, out uint index)
        {
            uint thisLength = Length;
            for (uint i = thisLength - 1; i != uint.MaxValue; i--)
            {
                if (characters[i] == text[text.Length - 1])
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

        public void Clear()
        {
            length = 0;
        }

        public void Append(char c)
        {
            ThrowIfLengthExceedsCapacity((uint)(length + 1));
            characters[length] = (byte)c;
            length = (byte)(length + 1);
        }

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

        public void Append(USpan<char> text)
        {
            ThrowIfLengthExceedsCapacity(length + text.Length);
            for (uint i = 0; i < text.Length; i++)
            {
                characters[length + i] = (byte)text[i];
            }

            length = (byte)(length + text.Length);
        }

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

        public void RemoveAt(uint index)
        {
            ThrowIfIndexOutOfRange(index);
            for (uint i = index; i < length - 1; i++)
            {
                characters[i] = characters[i + 1];
            }

            length = (byte)(length - 1);
        }

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

        public void Insert(uint index, char c)
        {
            ThrowIfLengthExceedsCapacity((uint)(length + 1));
            ThrowIfIndexOutOfRange(index);
            for (uint i = length; i > index; i--)
            {
                characters[i] = characters[i - 1];
            }

            characters[index] = (byte)c;
            length = (byte)(length + 1);
        }

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

        public void Replace(char oldValue, char newValue)
        {
            for (uint i = 0; i < length; i++)
            {
                if (characters[i] == oldValue)
                {
                    characters[i] = (byte)newValue;
                }
            }
        }

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

        public readonly override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is FixedString other && Equals(other);
        }

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

        [Conditional("DEBUG")]
        private readonly void ThrowIfIndexOutOfRange(uint index)
        {
            if (index >= length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range of {Length}");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfLengthExceedsCapacity(uint length)
        {
            if (length > Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"Length exceeds the maximum of {Capacity}");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfCharacterIsOutOfRange(char c)
        {
            if (c >= MaxCharacterValue)
            {
                throw new ArgumentOutOfRangeException(nameof(c), $"Character value exceeds the maximum of {MaxCharacterValue}");
            }
        }

        public static implicit operator FixedString(string text)
        {
            return new(text);
        }

        public static bool operator ==(FixedString a, FixedString b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(FixedString a, FixedString b)
        {
            return !a.Equals(b);
        }
    }
}
