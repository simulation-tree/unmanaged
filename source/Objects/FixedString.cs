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
        public const uint MaxLength = 255;
        public const uint MaxCharValue = 256;

        private fixed byte chars[256];

        public uint Length
        {
            readonly get => chars[255];
            set
            {
                ThrowIfLengthExceedsMax(value);
                uint length = Length;
                if (value > length)
                {
                    for (uint i = length; i < value; i++)
                    {
                        chars[i] = 0;
                    }
                }

                chars[255] = (byte)value;
            }
        }

        public char this[uint index]
        {
            readonly get
            {
                ThrowIfIndexOutOfRange(index);
                return (char)chars[index];
            }
            set
            {
                ThrowIfIndexOutOfRange(index);
                ThrowIfCharIsOutOfRange(value);
                chars[index] = (byte)value;
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
            USpan<byte> span = new(utf8Bytes, MaxLength);
            CopyFrom(span);
        }

        public readonly override string ToString()
        {
            uint length = Length;
            USpan<char> buffer = stackalloc char[(int)length];
            CopyTo(buffer);
            return buffer.ToString();
        }

        public void CopyFrom(USpan<char> text)
        {
            ThrowIfLengthExceedsMax(text.Length);
            byte length = 0;
            for (uint i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\0')
                {
                    break;
                }

                chars[i] = (byte)c;
                length++;
            }

            chars[255] = length;
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
                    ThrowIfCharIsOutOfRange(value);
                    chars[index] = (byte)value;
                    index++;
                    ThrowIfLengthExceedsMax(index);
                }
            }

            chars[255] = (byte)index;
        }

        public readonly uint CopyTo(USpan<byte> utf8Bytes)
        {
            uint byteIndex = 0;
            uint length = Length;
            for (uint i = 0; i < length; i++)
            {
                char c = (char)chars[i];
                if (c <= 0x7F)
                {
                    utf8Bytes[byteIndex++] = (byte)c;
                }
                else if (c <= 0x7FF)
                {
                    utf8Bytes[byteIndex++] = (byte)(0xC0 | (c >> 6)); // First 5 bits
                    utf8Bytes[byteIndex++] = (byte)(0x80 | (c & 0x3F)); // Last 6 bits
                }
            }

            return byteIndex;
        }

        public readonly uint CopyTo(USpan<char> buffer)
        {
            uint length = Length;
            for (uint i = 0; i < length; i++)
            {
                buffer[i] = (char)chars[i];
            }

            return length;
        }

        public readonly FixedString Slice(uint start, uint length)
        {
            ThrowIfIndexOutOfRange(start);
            ThrowIfLengthExceedsMax(start + length);
            FixedString result = default;
            for (uint i = 0; i < length; i++)
            {
                result.chars[i] = chars[start + i];
            }

            result.chars[255] = (byte)length;
            return result;
        }

        public readonly FixedString Slice(uint start)
        {
            return Slice(start, Length - start);
        }

        public readonly bool Contains(char c)
        {
            for (uint i = 0; i < Length; i++)
            {
                if (chars[i] == c)
                {
                    return true;
                }
            }

            return false;
        }

        public readonly bool Contains(USpan<char> text)
        {
            uint length = Length;
            for (uint i = 0; i < length; i++)
            {
                if (chars[i] == text[0])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
                    {
                        if (i + j >= length || chars[i + j] != text[j])
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
            uint length = Length;
            for (uint i = 0; i < length; i++)
            {
                if (chars[i] == text[0])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
                    {
                        if (i + j >= length || chars[i + j] != text[j])
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
            for (uint i = 0; i < Length; i++)
            {
                if (chars[i] == value)
                {
                    return i;
                }
            }

            throw new IndexOutOfRangeException();
        }

        public readonly bool TryIndexOf(char value, out uint index)
        {
            for (uint i = 0; i < Length; i++)
            {
                if (chars[i] == value)
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
            uint length = Length;
            for (uint i = 0; i < length; i++)
            {
                if (chars[i] == text[0])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
                    {
                        if (i + j >= length || chars[i + j] != text[j])
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
            uint length = Length;
            for (uint i = 0; i < length; i++)
            {
                if (chars[i] == text[0])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
                    {
                        if (i + j >= length || chars[i + j] != text[j])
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
            uint length = Length;
            for (uint i = 0; i < length; i++)
            {
                if (chars[i] == text[0])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
                    {
                        if (i + j >= length || chars[i + j] != text[j])
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
            uint length = Length;
            for (uint i = 0; i < length; i++)
            {
                if (chars[i] == text[0])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
                    {
                        if (i + j >= length || chars[i + j] != text[j])
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
            for (uint i = Length - 1; i >= 0; i--)
            {
                if (chars[i] == value)
                {
                    return i;
                }
            }

            throw new IndexOutOfRangeException();
        }

        public readonly bool TryLastIndexOf(char value, out uint index)
        {
            for (uint i = Length - 1; i >= 0; i--)
            {
                if (chars[i] == value)
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
            uint length = Length;
            for (uint i = length - 1; i >= 0; i--)
            {
                if (chars[i] == text[text.Length - 1])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
                    {
                        if (i - j < 0 || chars[i - j] != text[text.Length - j])
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
            uint length = Length;
            for (uint i = length - 1; i >= 0; i--)
            {
                if (chars[i] == text[text.Length - 1])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
                    {
                        if (i - j < 0 || chars[i - j] != text[text.Length - j])
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
            uint length = Length;
            for (uint i = length - 1; i >= 0; i--)
            {
                if (chars[i] == text[text.Length - 1])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
                    {
                        if (i - j < 0 || chars[i - j] != text[text.Length - j])
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

        public readonly bool TryLastIndexOf(FixedString text, out uint index)
        {
            uint length = Length;
            for (uint i = length - 1; i >= 0; i--)
            {
                if (chars[i] == text[text.Length - 1])
                {
                    bool found = true;
                    for (uint j = 1; j < text.Length; j++)
                    {
                        if (i - j < 0 || chars[i - j] != text[text.Length - j])
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

        public readonly bool StartsWith(USpan<char> text)
        {
            if (text.Length > Length)
            {
                return false;
            }

            for (uint i = 0; i < text.Length; i++)
            {
                if (chars[i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        public readonly bool StartsWith(FixedString text)
        {
            if (text.Length > Length)
            {
                return false;
            }

            for (uint i = 0; i < text.Length; i++)
            {
                if (chars[i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        public readonly bool EndsWith(USpan<char> text)
        {
            if (text.Length > Length)
            {
                return false;
            }

            for (uint i = 0; i < text.Length; i++)
            {
                if (chars[Length - text.Length + i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        public readonly bool EndsWith(FixedString text)
        {
            if (text.Length > Length)
            {
                return false;
            }

            for (uint i = 0; i < text.Length; i++)
            {
                if (chars[Length - text.Length + i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        public void Clear()
        {
            chars[255] = 0;
        }

        public void Append(char c)
        {
            uint length = Length;
            ThrowIfLengthExceedsMax(length + 1);
            chars[length] = (byte)c;
            chars[255] = (byte)(length + 1);
        }

        public void Append<T>(T formattable) where T : ISpanFormattable
        {
            uint length = Length;
            Span<char> buffer = stackalloc char[256];
            formattable.TryFormat(buffer, out int charsWritten, default, default);
            ThrowIfLengthExceedsMax(length + (uint)charsWritten);
            for (uint i = 0; i < charsWritten; i++)
            {
                chars[length + i] = (byte)buffer[(int)i];
            }

            chars[255] = (byte)(length + charsWritten);
        }

        public void Append(USpan<char> text)
        {
            uint length = Length;
            ThrowIfLengthExceedsMax(length + text.Length);
            for (uint i = 0; i < text.Length; i++)
            {
                chars[length + i] = (byte)text[i];
            }

            chars[255] = (byte)(length + text.Length);
        }

        public void Append(FixedString text)
        {
            uint length = Length;
            uint textLength = text.Length;
            ThrowIfLengthExceedsMax(length + textLength);
            for (uint i = 0; i < textLength; i++)
            {
                chars[length + i] = (byte)text[i];
            }

            chars[255] = (byte)(length + textLength);
        }

        public void RemoveAt(uint index)
        {
            uint length = Length;
            ThrowIfIndexOutOfRange(index);
            for (uint i = index; i < length - 1; i++)
            {
                chars[i] = chars[i + 1];
            }

            chars[255] = (byte)(length - 1);
        }

        public void RemoveRange(uint start, uint length)
        {
            uint totalLength = Length;
            ThrowIfIndexOutOfRange(start);
            ThrowIfLengthExceedsMax(start + length);
            for (uint i = start; i < totalLength - length; i++)
            {
                chars[i] = chars[i + length];
            }

            chars[255] = (byte)(totalLength - length);
        }

        public void Insert(uint index, char c)
        {
            uint length = Length;
            ThrowIfLengthExceedsMax(length + 1);
            ThrowIfIndexOutOfRange(index);
            for (uint i = length; i > index; i--)
            {
                chars[i] = chars[i - 1];
            }

            chars[index] = (byte)c;
            chars[255] = (byte)(length + 1);
        }

        public void Insert<T>(uint index, T formattable) where T : ISpanFormattable
        {
            uint length = Length;
            Span<char> buffer = stackalloc char[256];
            formattable.TryFormat(buffer, out int charsWritten, default, default);
            ThrowIfLengthExceedsMax(length + (uint)charsWritten);
            ThrowIfIndexOutOfRange(index);
            for (uint i = length; i > index; i--)
            {
                chars[i + charsWritten - 1] = chars[i - 1];
            }

            for (uint i = 0; i < charsWritten; i++)
            {
                chars[index + i] = (byte)buffer[(int)i];
            }

            chars[255] = (byte)(length + charsWritten);
        }

        public void Insert(uint index, USpan<char> text)
        {
            uint length = Length;
            ThrowIfLengthExceedsMax(length + text.Length);
            ThrowIfIndexOutOfRange(index);
            for (uint i = length; i > index; i--)
            {
                chars[i + text.Length - 1] = chars[i - 1];
            }

            for (uint i = 0; i < text.Length; i++)
            {
                chars[index + i] = (byte)text[i];
            }

            chars[255] = (byte)(length + text.Length);
        }

        public void Insert(uint index, FixedString text)
        {
            uint length = Length;
            uint textLength = text.Length;
            ThrowIfLengthExceedsMax(length + textLength);
            for (uint i = length; i > index; i--)
            {
                chars[i + textLength - 1] = chars[i - 1];
            }

            for (uint i = 0; i < textLength; i++)
            {
                chars[index + i] = (byte)text[i];
            }

            chars[255] = (byte)(length + textLength);
        }

        public void Replace(char oldValue, char newValue)
        {
            for (uint i = 0; i < Length; i++)
            {
                if (chars[i] == oldValue)
                {
                    chars[i] = (byte)newValue;
                }
            }
        }

        public bool TryReplace(USpan<char> oldValue, USpan<char> newValue)
        {
            uint length = Length;
            for (uint i = 0; i < length; i++)
            {
                if (chars[i] == oldValue[0])
                {
                    bool found = true;
                    for (uint j = 1; j < oldValue.Length; j++)
                    {
                        if (i + j >= length || chars[i + j] != oldValue[j])
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
                                chars[i + j] = (byte)newValue[j];
                            }
                        }
                        else
                        {
                            uint difference = newValue.Length - oldValue.Length;
                            if (difference > 0)
                            {
                                ThrowIfLengthExceedsMax(length + difference);
                            }

                            for (uint j = length; j > i + oldValue.Length; j--)
                            {
                                chars[j + difference - 1] = chars[j - 1];
                            }

                            for (uint j = 0; j < newValue.Length; j++)
                            {
                                chars[i + j] = (byte)newValue[j];
                            }

                            chars[255] = (byte)(length + difference);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryReplace(FixedString oldValue, FixedString newValue)
        {
            uint length = Length;
            for (uint i = 0; i < length; i++)
            {
                if (chars[i] == oldValue[0])
                {
                    bool found = true;
                    for (uint j = 1; j < oldValue.Length; j++)
                    {
                        if (i + j >= length || chars[i + j] != oldValue[j])
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
                                chars[i + j] = (byte)newValue[j];
                            }
                        }
                        else
                        {
                            uint difference = newValue.Length - oldValue.Length;
                            if (difference > 0)
                            {
                                ThrowIfLengthExceedsMax(length + difference);
                            }

                            for (uint j = length; j > i + oldValue.Length; j--)
                            {
                                chars[j + difference - 1] = chars[j - 1];
                            }

                            for (uint j = 0; j < newValue.Length; j++)
                            {
                                chars[i + j] = (byte)newValue[j];
                            }

                            chars[255] = (byte)(length + difference);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public readonly bool Equals(FixedString other)
        {
            uint length = Length;
            if (length != other.Length)
            {
                return false;
            }

            for (uint i = 0; i < length; i++)
            {
                if (chars[i] != other[i])
                {
                    return false;
                }
            }

            return true;
        }

        public readonly bool Equals(USpan<char> other)
        {
            uint length = Length;
            if (length != other.Length)
            {
                return false;
            }

            for (uint i = 0; i < length; i++)
            {
                if (chars[i] != other[i])
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
            USpan<char> temp = stackalloc char[(int)Length];
            CopyTo(temp);
            return Djb2Hash.Get(temp);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfLengthExceedsMax(uint length)
        {
            if (length > MaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"Length exceeds the maximum of {MaxLength}.");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfIndexOutOfRange(uint index)
        {
            if (index >= Length)
            {
                throw new IndexOutOfRangeException();
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfCharIsOutOfRange(char c)
        {
            if (c >= MaxCharValue)
            {
                throw new ArgumentOutOfRangeException(nameof(c), $"Character value exceeds the maximum of {MaxCharValue}.");
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
