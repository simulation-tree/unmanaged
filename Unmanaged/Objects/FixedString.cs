using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Unmanaged
{
    /// <summary>
    /// A fixed string that can be used in unmanaged code.
    /// Able to contain up to 290 characters within 256 bytes (7 bits per character).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = Size)]
    public struct FixedString : IEquatable<FixedString>, IEnumerable<char>
    {
        public const int Size = 256;
        public const int MaxCharValue = 128;

        /// <summary>
        /// Maximum amount of <see cref="char"/> that can fit inside.
        /// </summary>
        public const int MaxLength = (int)((Size - 2f) / 7 * 8);

        private unsafe fixed byte data[Size - 2];
        private ushort length;

        /// <summary>
        /// Length of the text.
        /// </summary>
        public int Length
        {
            readonly get => length;
            set
            {
                if (value < 0 || value > MaxLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"Length must be between 0 and {MaxLength}.");
                }

                length = (ushort)value;
            }
        }

        /// <summary>
        /// Access the character at the index.
        /// </summary>
        public unsafe char this[int index]
        {
            readonly get
            {
                if (index < 0 || index >= length)
                {
                    throw new IndexOutOfRangeException();
                }

                Span<char> span = stackalloc char[length];
                CopyTo(span);
                return span[index];
            }
            set
            {
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
            Read(value.AsSpan());
        }

        public FixedString(ReadOnlySpan<char> path)
        {
            Read(path);
        }

        public unsafe FixedString(sbyte* value)
        {
            this = FromUTF8Bytes(new ReadOnlySpan<byte>(value, Size));
        }

        /// <summary>
        /// Creates a fixed string from UTF8 encoded bytes.
        /// </summary>
        public unsafe FixedString(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length > MaxLength)
            {
                throw new InvalidOperationException($"Path length exceeds maximum length of {MaxLength}.");
            }

            Span<char> buffer = stackalloc char[bytes.Length];
            length = (ushort)Encoding.UTF8.GetChars(bytes, buffer);
            Read(buffer[..length]);
        }

        private unsafe void Read(ReadOnlySpan<char> text)
        {
            length = (ushort)text.Length;
            if (length > MaxLength)
            {
                throw new InvalidOperationException($"Path length exceeds maximum length of {MaxLength}.");
            }

            int outputIndex = 0;
            ulong temp = 0;
            int bitsCollected = 0;
            foreach (char c in text)
            {
                temp |= (ulong)(c & 0x7F) << bitsCollected;
                bitsCollected += 7;
                if (bitsCollected >= 8)
                {
                    data[outputIndex++] = (byte)(temp & 0xFF);
                    temp >>= 8;
                    bitsCollected -= 8;
                }
            }

            if (bitsCollected > 0)
            {
                data[outputIndex] = (byte)(temp & 0xFF);
            }
        }

        public unsafe void Append(ReadOnlySpan<char> text)
        {
            ushort newLength = (ushort)(text.Length + length);
            if (newLength > MaxLength)
            {
                throw new InvalidOperationException($"Path length exceeds maximum length of {MaxLength}.");
            }

            Span<char> buffer = stackalloc char[newLength];
            CopyTo(buffer);
            text.CopyTo(buffer[length..]);
            Read(buffer);
        }

        public unsafe void Append(char value)
        {
            if (length + 1 > MaxLength)
            {
                throw new InvalidOperationException($"Path length exceeds maximum length of {MaxLength}.");
            }

            Span<char> buffer = stackalloc char[length + 1];
            CopyTo(buffer);
            buffer[length] = value;
            Read(buffer);
        }

        public readonly unsafe int IndexOf(char value)
        {
            Span<char> buffer = stackalloc char[length];
            int outputIndex = 0;
            ulong temp = 0;
            int bitsCollected = 0;
            for (int i = 0; i < length; i++)
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

                    buffer[outputIndex] = c;
                    temp >>= 7;
                    bitsCollected -= 7;
                    outputIndex++;
                    if (outputIndex >= buffer.Length)
                    {
                        return -1;
                    }
                }
            }

            return -1;
        }

        public readonly unsafe int IndexOf(ReadOnlySpan<char> value, StringComparison comparison = StringComparison.Ordinal)
        {
            Span<char> buffer = stackalloc char[length];
            CopyTo(buffer);
            return buffer.IndexOf(value);
        }

        public readonly unsafe int LastIndexOf(char value)
        {
            Span<char> buffer = stackalloc char[length];
            CopyTo(buffer);
            return buffer.LastIndexOf(value);
        }

        public readonly unsafe FixedString Substring(int start)
        {
            return Substring(start, length - start);
        }

        public readonly unsafe FixedString Substring(int start, int length)
        {
            if (start + length > this.length)
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

        public readonly bool Contains(ReadOnlySpan<char> text, StringComparison comparison = StringComparison.Ordinal)
        {
            Span<char> temp = stackalloc char[length];
            CopyTo(temp);
            ReadOnlySpan<char> span = temp[..length];
            return span.Contains(text, comparison);
        }

        public readonly bool EndsWith(ReadOnlySpan<char> text, StringComparison comparison = StringComparison.Ordinal)
        {
            Span<char> temp = stackalloc char[length];
            CopyTo(temp);
            ReadOnlySpan<char> span = temp[..length];
            return span.EndsWith(text, comparison);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            Span<char> temp = stackalloc char[length];
            CopyTo(temp);
            return temp.ToString();
        }

        /// <summary>
        /// Returns the hash code for this text, based on the Djb2 algorithm.
        /// </summary>
        public readonly override int GetHashCode()
        {
            Span<char> temp = stackalloc char[length];
            CopyTo(temp);
            return Djb2.GetDjb2HashCode(temp);
        }

        /// <inheritdoc/>
        public readonly unsafe void CopyTo(Span<char> buffer)
        {
            CopyTo(buffer, 0, length);
        }

        /// <inheritdoc/>
        public readonly unsafe void CopyTo(Span<char> buffer, int start, int length)
        {
            if (buffer.Length < length)
            {
                throw new ArgumentException("Buffer is too small.", nameof(buffer));
            }

            if (start < 0 || start + length > this.length)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            int outputIndex = 0;
            ulong temp = 0;
            int bitsCollected = 0;
            for (int i = 0; i < length; i++)
            {
                byte b = data[i + start];
                temp |= (ulong)b << bitsCollected;
                bitsCollected += 8;

                while (bitsCollected >= 7)
                {
                    buffer[outputIndex] = (char)(temp & 0x7F);
                    temp >>= 7;
                    bitsCollected -= 7;
                    outputIndex++;
                    if (outputIndex >= buffer.Length)
                    {
                        return;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public readonly override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is FixedString address && Equals(address);
        }

        /// <inheritdoc/>
        public readonly bool Equals(FixedString other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        /// <inheritdoc/>
        public readonly bool Equals(string? other)
        {
            if (other is null)
            {
                return length == 0;
            }

            if (other.Length != length)
            {
                return false;
            }

            for (var i = 0; i < length; i++)
            {
                if (this[i] != other[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public readonly bool Equals(ReadOnlySpan<char> other)
        {
            if (other.Length != length)
            {
                return false;
            }

            for (var i = 0; i < length; i++)
            {
                if (this[i] != other[i])
                {
                    return false;
                }
            }

            return true;
        }

        IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public unsafe readonly int CopyUTF8Bytes(Span<byte> bytes)
        {
            Span<char> span = stackalloc char[length];
            CopyTo(span);
            return Encoding.UTF8.GetBytes(span, bytes);
        }

        public static FixedString FromUTF8Bytes(ReadOnlySpan<byte> bytes)
        {
            Span<char> buffer = stackalloc char[bytes.Length];
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
            if (left.length + right.length > MaxLength)
            {
                throw new ArgumentException($"Path length exceeds maximum length of {MaxLength}.", nameof(right));
            }

            Span<char> temp = stackalloc char[left.length + right.length];
            left.CopyTo(temp);
            right.CopyTo(temp[left.length..]);
            return new FixedString(temp);
        }

        public static FixedString operator +(FixedString left, char right)
        {
            if (left.length + 1 > MaxLength)
            {
                throw new ArgumentException($"Path length exceeds maximum length of {MaxLength}.", nameof(right));
            }

            Span<char> temp = stackalloc char[left.length + 1];
            left.CopyTo(temp);
            temp[left.length] = right;
            return new FixedString(temp);
        }

        public static FixedString operator +(FixedString left, string right)
        {
            if (left.length + right.Length > MaxLength)
            {
                throw new ArgumentException($"Path length exceeds maximum length of {MaxLength}.", nameof(right));
            }

            Span<char> temp = stackalloc char[left.length + right.Length];
            left.CopyTo(temp);
            right.AsSpan().CopyTo(temp[left.length..]);
            return new FixedString(temp);
        }

        /// <inheritdoc/>
        public static implicit operator FixedString(string path)
        {
            return new(path);
        }

        /// <inheritdoc/>
        public static implicit operator FixedString(ReadOnlySpan<char> path)
        {
            return new(path);
        }

        /// <inheritdoc/>
        public static implicit operator FixedString(Span<char> path)
        {
            return new(path);
        }

        public struct Enumerator : IEnumerator<char>
        {
            private readonly FixedString address;
            private int index;

            public readonly char Current => address[index];

            readonly object? IEnumerator.Current => Current;

            public Enumerator(FixedString address)
            {
                this.address = address;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < address.length;
            }

            public void Reset()
            {
                index = -1;
            }

            public readonly void Dispose()
            {
            }
        }

        public struct DoubleEnumerator : IEnumerator<(char, char)>
        {
            private readonly FixedString a;
            private readonly FixedString b;
            private int index;

            public readonly (char, char) Current => (a[index], b[index]);

            readonly object? IEnumerator.Current => Current;

            public DoubleEnumerator(FixedString a, FixedString b)
            {
                this.a = a;
                this.b = b;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < a.length && index < b.length;
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
