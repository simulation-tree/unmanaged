using System;
using System.Numerics;

namespace Unmanaged
{
    /// <summary>
    /// Extension functions for <see cref="USpan{T}"/>.
    /// </summary>
    public unsafe static class USpanExtensions
    {
        private const char BOM = (char)65279;

        /// <summary>
        /// Fills the given <paramref name="destination"/> with the string representation of the <typeparamref name="T"/> value.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
#if NET
        public static uint ToString<T>(this T formattable, USpan<char> destination) where T : unmanaged, ISpanFormattable
        {
            return formattable.TryFormat(destination, out int charsWritten, default, default) ? (uint)charsWritten : 0;
        }
#else
        public static uint ToString<T>(this T formattable, USpan<char> destination) where T : unmanaged, IFormattable
        {
            string? result = formattable.ToString(default, default);
            if (result is null)
            {
                return 0;
            }

            result.CopyTo(destination);
            return (uint)result.Length;
        }
#endif

        /// <summary>
        /// Fills the given <paramref name="destination"/> with the string representation of the given <see cref="Vector2"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this Vector2 vector, USpan<char> destination)
        {
            uint length = 0;
            destination[length++] = '<';
            length += vector.X.ToString(destination.Slice(length));
            destination[length++] = ',';
            destination[length++] = ' ';
            length += vector.Y.ToString(destination.Slice(length));
            destination[length++] = '>';
            return length;
        }

        /// <summary>
        /// Fills the given <paramref name="destination"/> with the string representation of the given <see cref="Vector3"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this Vector3 vector, USpan<char> destination)
        {
            uint length = 0;
            destination[length++] = '<';
            length += vector.X.ToString(destination.Slice(length));
            destination[length++] = ',';
            destination[length++] = ' ';
            length += vector.Y.ToString(destination.Slice(length));
            destination[length++] = ',';
            destination[length++] = ' ';
            length += vector.Z.ToString(destination.Slice(length));
            destination[length++] = '>';
            return length;
        }

        /// <summary>
        /// Fills the given <paramref name="destination"/> with the string representation of the given <see cref="Vector4"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this Vector4 vector, USpan<char> destination)
        {
            uint length = 0;
            destination[length++] = '<';
            length += vector.X.ToString(destination.Slice(length));
            destination[length++] = ',';
            destination[length++] = ' ';
            length += vector.Y.ToString(destination.Slice(length));
            destination[length++] = ',';
            destination[length++] = ' ';
            length += vector.Z.ToString(destination.Slice(length));
            destination[length++] = ',';
            destination[length++] = ' ';
            length += vector.W.ToString(destination.Slice(length));
            destination[length++] = '>';
            return length;
        }

        /// <summary>
        /// Fills the given <paramref name="destination"/> with the string representation of the given <see cref="Quaternion"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this Quaternion quaternion, USpan<char> destination)
        {
            uint length = 0;
            destination[length++] = '{';
            destination[length++] = 'X';
            destination[length++] = ':';
            length += quaternion.X.ToString(destination.Slice(length));
            destination[length++] = ' ';
            destination[length++] = 'Y';
            destination[length++] = ':';
            length += quaternion.Y.ToString(destination.Slice(length));
            destination[length++] = ' ';
            destination[length++] = 'Z';
            destination[length++] = ':';
            length += quaternion.Z.ToString(destination.Slice(length));
            destination[length++] = ' ';
            destination[length++] = 'W';
            destination[length++] = ':';
            length += quaternion.W.ToString(destination.Slice(length));
            destination[length++] = '}';
            return length;
        }

        /// <summary>
        /// Retrieves the index of the first occurrence of the given <paramref name="value"/>.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public static uint IndexOf<T>(this USpan<T> span, T value) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
            int index = values.IndexOf(value);
            unchecked
            {
                return (uint)index;
            }
        }

        /// <summary>
        /// Retrieves the index of the last occurrence of the given <paramref name="value"/>.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public static uint LastIndexOf<T>(this USpan<T> span, T value) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
            int index = values.LastIndexOf(value);
            unchecked
            {
                return (uint)index;
            }
        }

        /// <summary>
        /// Checks if the span contains <paramref name="value"/>.
        /// </summary>
        public static bool Contains<T>(this USpan<T> span, T value) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
            return values.Contains(value);
        }

        /// <summary>
        /// Retrieves the index of the first occurrence of the given <paramref name="value"/>.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public static uint IndexOf<T>(this USpan<T> span, USpan<T> value) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
            Span<T> search = value;
            int index = values.IndexOf(search);
            unchecked
            {
                return (uint)index;
            }
        }

        /// <summary>
        /// Retrieves the index of the last occurrence of the given <paramref name="value"/>.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public static uint LastIndexOf<T>(this USpan<T> span, USpan<T> value) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
            Span<T> search = value;
            int index = values.LastIndexOf(search);
            unchecked
            {
                return (uint)index;
            }
        }

        /// <summary>
        /// Checks if the span contains <paramref name="value"/>.
        /// </summary>
        public static bool Contains<T>(this USpan<T> span, USpan<T> value) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
            Span<T> search = value;
            return values.IndexOf(search) != -1;
        }

        /// <summary>
        /// Attempts to retrieve the index of the first occurrence of the given <paramref name="value"/>.
        /// </summary>
        /// <returns><c>true</c> if contained.</returns>
        public static bool TryIndexOf<T>(this USpan<T> span, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
            unchecked
            {
                index = (uint)values.IndexOf(value);
                return index != uint.MaxValue;
            }
        }

        /// <summary>
        /// Attempts to retrieve the index of the last occurrence of the given <paramref name="value"/>.
        /// </summary>
        /// <returns><c>true</c> if contained.</returns>
        public static bool TryLastIndexOf<T>(this USpan<T> span, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
            unchecked
            {
                index = (uint)values.LastIndexOf(value);
                return index != uint.MaxValue;
            }
        }

        /// <summary>
        /// Attempts to retrieve the index of the first occurrence of the given <paramref name="value"/>.
        /// </summary>
        /// <returns><c>true</c> if contained.</returns>
        public static bool TryIndexOf<T>(this USpan<T> span, USpan<T> value, out uint index) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
            unchecked
            {
                index = (uint)values.IndexOf(value);
                return index != uint.MaxValue;
            }
        }

        /// <summary>
        /// Attempts to retrieve the index of the last occurrence of the given <paramref name="value"/>.
        /// </summary>
        /// <returns><c>true</c> if contained.</returns>
        public static bool TryLastIndexOf<T>(this USpan<T> span, USpan<T> value, out uint index) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
            unchecked
            {
                index = (uint)values.LastIndexOf(value);
                return index != uint.MaxValue;
            }
        }

        /// <summary>
        /// Checks if the span starts with <paramref name="value"/>.
        /// </summary>
        public static bool StartsWith<T>(this USpan<T> span, T value) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
            return values.IndexOf(value) == 0;
        }

        /// <summary>
        /// Checks if the span ends with <paramref name="value"/>.
        /// </summary>
        public static bool EndsWith<T>(this USpan<T> span, T value) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
            return values.LastIndexOf(value) == values.Length - 1;
        }

        /// <summary>
        /// Removes all occurrences of <paramref name="value"/> from the beginning.
        /// </summary>
        public static USpan<T> TrimStart<T>(this USpan<T> span, T value) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
#if NET
            return values.TrimStart(value);
#else
            int start = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (!values[i].Equals(value))
                {
                    start = i;
                    break;
                }
            }

            return values.Slice(start);
#endif
        }

        /// <summary>
        /// Removes all trailing occurrences of <paramref name="value"/>.
        /// </summary>
        public static USpan<T> TrimEnd<T>(this USpan<T> span, T value) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
#if NET
            return values.TrimEnd(value);
#else
            int end = values.Length - 1;
            for (int i = values.Length - 1; i >= 0; i--)
            {
                if (!values[i].Equals(value))
                {
                    end = i;
                    break;
                }
            }

            return values.Slice(0, end + 1);
#endif
        }

        /// <summary>
        /// Removes all occurrences of <paramref name="value"/> from the beginning.
        /// </summary>
        public static USpan<T> TrimStart<T>(this USpan<T> span, USpan<T> value) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
#if NET
            return values.TrimStart(value);
#else
            int start = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (!values.Slice(i).SequenceEqual(value))
                {
                    start = i;
                    break;
                }
            }

            return values.Slice(start);
#endif
        }

        /// <summary>
        /// Removes all trailing occurrences of <paramref name="value"/>.
        /// </summary>
        public static USpan<T> TrimEnd<T>(this USpan<T> span, USpan<T> value) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
#if NET
            return values.TrimEnd(value);
#else
            int end = values.Length - 1;
            for (int i = values.Length - 1; i >= 0; i--)
            {
                if (!values.Slice(i).SequenceEqual(value))
                {
                    end = i;
                    break;
                }
            }

            return values.Slice(0, end + 1);
#endif
        }

        /// <summary>
        /// Checks if the entire sequence equals the given <paramref name="other"/>.
        /// </summary>
        public static bool SequenceEqual<T>(this USpan<T> span, USpan<T> other) where T : unmanaged, IEquatable<T>
        {
            Span<T> values = span;
            Span<T> compare = other;
            return values.SequenceEqual(compare);
        }

        /// <summary>
        /// Gets a single UTF8 character at the given <paramref name="bytePosition"/>.
        /// </summary>
        /// <returns>Amount of <see cref="byte"/> values read.</returns>
        public static byte GetUTF8Character(this USpan<byte> bytes, uint bytePosition, out char low, out char high)
        {
            high = default;
            byte firstByte = bytes[bytePosition];
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
                throw new FormatException("Invalid UTF8 byte sequence");
            }

            for (uint j = 1; j <= additional; j++)
            {
                byte next = bytes[bytePosition + j];
                if ((next & 0xC0) != 0x80)
                {
                    throw new FormatException("Invalid UTF8 continuation byte");
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
        /// Reads the bytes formatted as UTF8 into the given <paramref name="destination"/>.
        /// <para>Reads until until a <see langword="default"/> character is found,
        /// or when the <paramref name="length"/> amount of <see cref="char"/> values have been read.</para>
        /// </summary>
        /// <returns>Amount of <see cref="byte"/> values read.</returns>
        public static uint GetUTF8Characters(this USpan<byte> bytes, uint bytePosition, uint length, USpan<char> destination)
        {
            uint charactersRead = 0;
            uint byteIndex = 0;
            while (byteIndex < bytes.Length)
            {
                byte bytesRead = bytes.GetUTF8Character(bytePosition + byteIndex, out char low, out char high);
                if (low == default)
                {
                    destination[charactersRead++] = default;
                    return charactersRead;
                }

                if (high != default)
                {
                    destination[charactersRead++] = high;
                    if (charactersRead == length)
                    {
                        break;
                    }

                    destination[charactersRead++] = low;
                    if (charactersRead == length)
                    {
                        break;
                    }
                }
                else
                {
                    if (low != BOM)
                    {
                        destination[charactersRead++] = low;
                        if (charactersRead == length)
                        {
                            break;
                        }
                    }
                }

                byteIndex += bytesRead;
            }

            return charactersRead;
        }

        /// <summary>
        /// Reads how long the UTF8 text is by counting characters
        /// until a terminator or no more bytes to read.
        /// </summary>
        public static uint GetUTF8Length(this USpan<byte> bytes)
        {
            uint bytePosition = 0;
            while (bytePosition < bytes.Length)
            {
                byte next = bytes[bytePosition];
                if (next == default)
                {
                    break;
                }

                bytePosition += bytes.GetUTF8Character(bytePosition, out _, out _);
            }

            return bytePosition;
        }
    }
}