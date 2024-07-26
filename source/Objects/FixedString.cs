using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Unmanaged
{
    /// <summary>
    /// A value container of up to 290 characters, each 7 bits.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 256)]
    public unsafe struct FixedString : IEquatable<FixedString>, IEnumerable<char>
    {
        public const int MaxCharValue = 128;
        public const char Terminator = '\0';

        /// <summary>
        /// Maximum amount of <see cref="char"/> that can be contained.
        /// </summary>
        public const int MaxLength = 291;

        private fixed byte data[256];

        /// <summary>
        /// Length of the text.
        /// </summary>
        public int Length
        {
            readonly get
            {
                int length = 0;
                ulong temp = 0;
                int bitsCollected = 0;
                for (int i = 0; i < MaxLength; i++)
                {
                    byte b = data[i];
                    temp |= (ulong)b << bitsCollected;
                    bitsCollected += 8;

                    while (bitsCollected >= 7)
                    {
                        char c = (char)(temp & 0x7F);
                        if (c == Terminator)
                        {
                            return length;
                        }

                        temp >>= 7;
                        bitsCollected -= 7;
                        length++;
                    }
                }

                return length;
            }
            set
            {
                if (value < 0 || value > MaxLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"Length must be between 0 and {MaxLength}.");
                }

                Span<char> buffer = stackalloc char[MaxLength];
                int length = CopyTo(buffer);
                if (value > length)
                {
                    for (int i = length; i < value; i++)
                    {
                        buffer[i] = ' ';
                    }
                }
                else if (value < length)
                {
                    for (int i = value; i < length; i++)
                    {
                        buffer[i] = Terminator;
                    }
                }

                buffer[value] = Terminator;
                Build(buffer);
            }
        }

        public readonly bool IsEmpty => (data[0] & 0x7F) == Terminator;

        public char this[uint index]
        {
            readonly get => this[(int)index];
            set => this[(int)index] = value;
        }

        /// <summary>
        /// Access the character at the index.
        /// </summary>
        public char this[int index]
        {
            readonly get
            {
                Span<char> span = stackalloc char[MaxLength];
                int length = CopyTo(span);
                if (index < 0 || index >= length)
                {
                    throw new IndexOutOfRangeException();
                }

                return span[index];
            }
            set
            {
                int length = Length;
                if (index < 0 || index >= length)
                {
                    throw new IndexOutOfRangeException();
                }

                int byteIndex = 0;
                ulong temp = 0;
                int bitsCollected = 0;
                for (int i = 0; i < length; i++)
                {
                    if (i == index)
                    {
                        temp |= (ulong)value << bitsCollected;
                    }
                    else
                    {
                        temp |= (ulong)this[i] << bitsCollected;
                    }

                    bitsCollected += 7;
                    if (bitsCollected >= 8)
                    {
                        data[byteIndex++] = (byte)(temp & 0xFF);
                        temp >>= 8;
                        bitsCollected -= 8;
                    }
                }

                if (bitsCollected > 0)
                {
                    data[byteIndex] = (byte)(temp & 0xFF);
                }
            }
        }

        public FixedString(string value)
        {
            Build(value.AsSpan());
        }

        public FixedString(ReadOnlySpan<char> path)
        {
            Build(path);
        }

        public FixedString(sbyte* value)
        {
            this = CreateFromUTF8Bytes(new ReadOnlySpan<byte>(value, sizeof(FixedString)));
        }

        public FixedString(byte* value)
        {
            this = CreateFromUTF8Bytes(new ReadOnlySpan<byte>(value, sizeof(FixedString)));
        }

        /// <summary>
        /// Creates a fixed string from UTF8 encoded bytes.
        /// </summary>
        public FixedString(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length > MaxLength)
            {
                throw new InvalidOperationException($"Path length exceeds maximum length of {MaxLength}.");
            }

            Span<char> buffer = stackalloc char[bytes.Length];
            var length = (ushort)Encoding.UTF8.GetChars(bytes, buffer);
            Build(buffer[..length]);
        }

        private void Build(ReadOnlySpan<char> text)
        {
            var length = (ushort)text.Length;
            if (length > MaxLength)
            {
                throw new InvalidOperationException($"Path length exceeds maximum length of {MaxLength}.");
            }

            Clear();
            int outputIndex = 0;
            ulong temp = 0;
            int bitsCollected = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                temp |= (ulong)(c & 0x7F) << bitsCollected;
                bitsCollected += 7;
                if (bitsCollected >= 8)
                {
                    data[outputIndex++] = (byte)(temp & 0xFF);
                    temp >>= 8;
                    bitsCollected -= 8;
                }

                if (c == Terminator)
                {
                    return;
                }
            }

            if (bitsCollected > 0)
            {
                data[outputIndex] = (byte)(temp & 0xFF);
            }
        }

        /// <summary>
        /// Clears the text content.
        /// </summary>
        public void Clear()
        {
            ulong temp = 0;
            int bitsCollected = 0;
            for (int i = 0; i < MaxLength; i++)
            {
                byte b = data[i];
                temp |= (ulong)b << bitsCollected;
                bitsCollected += 8;

                while (bitsCollected >= 7)
                {
                    char c = (char)(temp & 0x7F);
                    temp >>= 7;
                    bitsCollected -= 7;
                    if (c == Terminator)
                    {
                        return;
                    }
                }

                data[i] = 0;
            }
        }

        public void Append(ReadOnlySpan<char> text)
        {
            Span<char> buffer = stackalloc char[MaxLength];
            int length = CopyTo(buffer);
            if (length + text.Length > MaxLength)
            {
                throw new InvalidOperationException($"Text exceeds maximum length of {MaxLength} after operation.");
            }

            Span<char> destinationBuffer = stackalloc char[length + text.Length];
            buffer[..length].CopyTo(destinationBuffer);
            text.CopyTo(destinationBuffer[length..]);
            Build(destinationBuffer);
        }

        public void Append(char value)
        {
            Span<char> buffer = stackalloc char[MaxLength];
            int length = CopyTo(buffer);
            if (length + 1 > MaxLength)
            {
                throw new InvalidOperationException($"Text exceeds maximum length of {MaxLength} after operation.");
            }

            buffer[length] = value;
            Build(buffer[..(length + 1)]);
        }

#if CSHARP_9_OR_LATER
        public void Append<T>(T value) where T : ISpanFormattable
        {
            Span<char> valueBuffer = stackalloc char[MaxLength];
            value.TryFormat(valueBuffer, out int valueLength, default, null);

            Append(valueBuffer[..valueLength]);
        }
#else
        public void Append<T>(T value) where T : IFormattable
        {
            Span<char> valueBuffer = stackalloc char[MaxLength];
            string str = value.ToString(default, null);

            Append(valueBuffer[..str.Length]);
        }
#endif

        public void Append(FixedString text)
        {
            Span<char> buffer = stackalloc char[MaxLength];
            int length = CopyTo(buffer);
            if (length + text.Length > MaxLength)
            {
                throw new InvalidOperationException($"Text exceeds maximum length of {MaxLength} after operation.");
            }

            Span<char> destinationBuffer = stackalloc char[length + text.Length];
            buffer[..length].CopyTo(destinationBuffer);
            text.CopyTo(destinationBuffer[length..]);
            Build(destinationBuffer);
        }

        public readonly unsafe int IndexOf(char value)
        {
            Span<char> buffer = stackalloc char[MaxLength];
            int outputIndex = 0;
            ulong temp = 0;
            int bitsCollected = 0;
            for (int i = 0; i < MaxLength; i++)
            {
                byte b = data[i];
                temp |= (ulong)b << bitsCollected;
                bitsCollected += 8;

                while (bitsCollected >= 7)
                {
                    char c = (char)(temp & 0x7F);
                    if (c == value)
                    {
                        return outputIndex;
                    }
                    else if (c == Terminator)
                    {
                        return -1;
                    }

                    buffer[outputIndex] = c;
                    temp >>= 7;
                    bitsCollected -= 7;
                    outputIndex++;
                }
            }

            return -1;
        }

        public readonly unsafe int IndexOf(ReadOnlySpan<char> value, StringComparison comparison = StringComparison.Ordinal)
        {
            Span<char> buffer = stackalloc char[MaxLength];
            int length = CopyTo(buffer);
            return buffer[..length].IndexOf(value);
        }

        public readonly unsafe int LastIndexOf(char value)
        {
            Span<char> buffer = stackalloc char[MaxLength];
            int length = CopyTo(buffer);
            return buffer[..length].LastIndexOf(value);
        }

        public readonly unsafe FixedString Substring(int start)
        {
            Span<char> temp = stackalloc char[MaxLength];
            int thisLength = CopyTo(temp);
            if (start > thisLength)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            int length = thisLength - start;
            Span<char> buffer = stackalloc char[length];
            CopyTo(buffer, start, length);
            return new FixedString(buffer);
        }

        public readonly unsafe FixedString Substring(int start, int length)
        {
            Span<char> temp = stackalloc char[MaxLength];
            int thisLength = CopyTo(temp);
            if (start + length > thisLength)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            Span<char> buffer = stackalloc char[length];
            CopyTo(buffer, start, length);
            return new FixedString(buffer);
        }

        public readonly bool Contains(char value)
        {
            return IndexOf(value) != -1;
        }

        /// <summary>
        /// Returns true if the text context contains the other
        /// given text.
        /// </summary>
        public readonly bool Contains(ReadOnlySpan<char> text, StringComparison comparison = StringComparison.Ordinal)
        {
            Span<char> temp = stackalloc char[MaxLength];
            int length = CopyTo(temp);
            ReadOnlySpan<char> span = temp[..length];
            return span.Contains(text, comparison);
        }

        public readonly bool EndsWith(ReadOnlySpan<char> text, StringComparison comparison = StringComparison.Ordinal)
        {
            Span<char> temp = stackalloc char[MaxLength];
            int length = CopyTo(temp);
            ReadOnlySpan<char> span = temp[..length];
            return span.EndsWith(text, comparison);
        }

        public void RemoveAt(int index)
        {
            RemoveAt(index, 1);
        }

        public void RemoveAt(int start, int length)
        {
            Span<char> temp = stackalloc char[MaxLength];
            int thisLength = CopyTo(temp);
            Span<char> buffer = stackalloc char[thisLength - length + 1];
            temp[..start].CopyTo(buffer[..start]);
            temp[(start + length)..thisLength].CopyTo(buffer[start..]);
            buffer[^1] = Terminator;
            Build(buffer);
            int lasd = Length;
        }

        public bool Replace(ReadOnlySpan<char> target, ReadOnlySpan<char> replacement, StringComparison comparison = StringComparison.Ordinal)
        {
            int index = IndexOf(target, comparison);
            if (index != -1)
            {
                RemoveAt(index, target.Length);
                Insert(index, replacement);
                return true;
            }
            else return false;
        }

        public void Insert(int position, char c)
        {
            Span<char> temp = stackalloc char[1];
            temp[0] = c;
            Insert(position, temp);
        }

        public void Insert(int position, ReadOnlySpan<char> text)
        {
            Span<char> temp = stackalloc char[MaxLength];
            int thisLength = CopyTo(temp);

            if (text.Length + thisLength > MaxLength)
            {
                throw new InvalidOperationException($"Text exceeds maximum length of {MaxLength} after operation.");
            }

            Span<char> buffer = stackalloc char[thisLength + text.Length];
            temp[..position].CopyTo(buffer[..position]);
            text.CopyTo(buffer[position..(position + text.Length)]);
            temp[position..thisLength].CopyTo(buffer[(position + text.Length)..]);
            Build(buffer);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            Span<char> temp = stackalloc char[MaxLength];
            int length = CopyTo(temp);
            return temp[..length].ToString();
        }

        /// <summary>
        /// Returns the hash code for this text, based on the Djb2 algorithm.
        /// </summary>
        public readonly override int GetHashCode()
        {
            Span<char> temp = stackalloc char[MaxLength];
            int length = CopyTo(temp);
            return Djb2Hash.Get(temp[..length]);
        }

        /// <summary>
        /// Copies all characters into the destination <see cref="char"/> buffer.
        /// </summary>
        /// <returns>Amount of characters copied, the greatest between buffer length and text content length.</returns>
        public readonly int CopyTo(Span<char> buffer)
        {
            fixed (char* bufferPtr = buffer)
            {
                return CopyTo(bufferPtr, 0, buffer.Length);
            }
        }

        public readonly int CopyTo(Span<byte> buffer)
        {
            fixed (byte* bufferPtr = buffer)
            {
                return CopyTo(bufferPtr, 0, buffer.Length);
            }
        }

        /// <summary>
        /// Copies the text content into the destination <see cref="char"/> buffer.
        /// </summary>
        public readonly int CopyTo(char* destinationBuffer, int start, int length)
        {
            int thisLength = 0;
            ulong temp = 0;
            int bitsCollected = 0;
            for (int i = 0; i < MaxLength; i++)
            {
                byte b = data[i];
                temp |= (ulong)b << bitsCollected;
                bitsCollected += 8;

                while (bitsCollected >= 7)
                {
                    char c = (char)(temp & 0x7F);
                    if (c == Terminator)
                    {
                        return thisLength;
                    }

                    destinationBuffer[thisLength++] = c;
                    temp >>= 7;
                    bitsCollected -= 7;
                    if (thisLength >= length)
                    {
                        return length;
                    }
                }
            }

            return thisLength;
        }

        public readonly int CopyTo(byte* destinationBuffer, int start, int length)
        {
            int thisLength = 0;
            ulong temp = 0;
            int bitsCollected = 0;
            for (int i = 0; i < MaxLength; i++)
            {
                byte b = data[i];
                temp |= (ulong)b << bitsCollected;
                bitsCollected += 8;

                while (bitsCollected >= 7)
                {
                    char c = (char)(temp & 0x7F);
                    if (c == Terminator)
                    {
                        return thisLength;
                    }

                    destinationBuffer[thisLength++] = (byte)c;
                    temp >>= 7;
                    bitsCollected -= 7;
                    if (thisLength >= length)
                    {
                        return length;
                    }
                }
            }

            return thisLength;
        }

        /// <summary>
        /// Copies the characters within the specified range into the destination buffer.
        /// </summary>
        public readonly void CopyTo(Span<char> destinationBuffer, int start, int length)
        {
            if (destinationBuffer.Length < length)
            {
                throw new ArgumentException("Buffer length is not able to contain the characters to copy.", nameof(destinationBuffer));
            }

            Span<char> temp = stackalloc char[MaxLength];
            int thisLength = CopyTo(temp);
            if (start < 0 || start + length > thisLength)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            temp.Slice(start, length).CopyTo(destinationBuffer);
        }

        public readonly void CopyTo(Span<byte> destinationBuffer, int start, int length)
        {
            if (destinationBuffer.Length < length)
            {
                throw new ArgumentException("Buffer length is not able to contain the characters to copy.", nameof(destinationBuffer));
            }

            Span<byte> temp = stackalloc byte[MaxLength];
            int thisLength = CopyTo(temp);
            if (start < 0 || start + length > thisLength)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            temp.Slice(start, length).CopyTo(destinationBuffer);
        }

        /// <inheritdoc/>
        public readonly override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is FixedString address && Equals(address);
        }

        /// <summary>
        /// Compares the given text.
        /// </summary>
        /// <returns><c>true</c> if equal to given text.</returns>
        public readonly bool Equals(FixedString other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        /// <summary>
        /// Compares the given text.
        /// </summary>
        /// <returns><c>true</c> if equal to given text.</returns>
        public readonly bool Equals(string? other)
        {
            if (other is null)
            {
                byte firstByte = data[0];
                return (firstByte & 0x7F) == 0; //length == 0
            }

            return Equals(other.AsSpan());
        }

        /// <summary>
        /// Compares the given text.
        /// </summary>
        /// <returns><c>true</c> if equal to given text.</returns>
        public readonly bool Equals(ReadOnlySpan<char> other)
        {
            int outputIndex = 0;
            ulong temp = 0;
            int bitsCollected = 0;
            for (int i = 0; i < MaxLength; i++)
            {
                byte b = data[i];
                temp |= (ulong)b << bitsCollected;
                bitsCollected += 8;

                while (bitsCollected >= 7)
                {
                    char c = (char)(temp & 0x7F);
                    if (c != other[outputIndex])
                    {
                        return false;
                    }

                    temp >>= 7;
                    bitsCollected -= 7;
                    outputIndex++;
                    if (outputIndex == other.Length)
                    {
                        return true;
                    }
                }
            }

            return true;
        }

        public readonly Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        readonly IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            return GetEnumerator();
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static FixedString CreateFromUTF8Bytes(ReadOnlySpan<byte> bytes)
        {
            Span<char> buffer = stackalloc char[bytes.Length];
            //todo: manually iterate through utf8 stream
            ushort length = (ushort)Encoding.UTF8.GetChars(bytes, buffer);
            if (length > MaxLength)
            {
                throw new ArgumentException($"Path length exceeds maximum length of {MaxLength}.", nameof(length));
            }

            return new FixedString(buffer[..length]);
        }

        public static FixedString ToString<T>(T value) where T : notnull
        {
            if (value is byte b)
            {
                return new FixedString(b.ToString());
            }
            else if (value is uint uintValue)
            {
                return new FixedString(uintValue.ToString());
            }

            string str = value.ToString() ?? string.Empty;
            return new FixedString(str);
        }

        /// <inheritdoc/>
        public static bool operator ==(FixedString left, FixedString right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(FixedString left, FixedString right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc/>
        public static FixedString operator +(FixedString left, FixedString right)
        {
            FixedString result = new();
            result.Append(left);
            result.Append(right);
            return result;
        }

        public static FixedString operator +(FixedString left, char right)
        {
            FixedString result = new();
            result.Append(left);
            result.Append(right);
            return result;
        }

        public static FixedString operator +(char left, FixedString right)
        {
            FixedString result = new();
            result.Append(left);
            result.Append(right);
            return result;
        }

        public static FixedString operator +(FixedString left, string right)
        {
            FixedString result = new();
            result.Append(left);
            result.Append(right);
            return result;
        }

        public static FixedString operator +(string left, FixedString right)
        {
            FixedString result = new();
            result.Append(left);
            result.Append(right);
            return result;
        }

        public static implicit operator FixedString(string value)
        {
            return new FixedString(value);
        }

        public static implicit operator FixedString(ReadOnlySpan<char> value)
        {
            return new FixedString(value);
        }

        public struct Enumerator : IEnumerator<char>
        {
            private readonly FixedString address;
            private readonly int length;
            private int index;

            public readonly char Current => address[index];

            readonly object? IEnumerator.Current => Current;

            public Enumerator(FixedString address)
            {
                this.address = address;
                length = address.Length;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < length;
            }

            public void Reset()
            {
                index = -1;
            }

            public readonly void Dispose()
            {
            }
        }
    }
}
