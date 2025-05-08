using System.Diagnostics;
using System.Numerics;

namespace System
{
    /// <summary>
    /// Extension functions for <see cref="Span{T}"/>.
    /// </summary>
    public unsafe static class SpanExtensions
    {
        private const char BOM = (char)65279;

        [Conditional("DEBUG")]
        private static void ThrowIfSizeMismatch<X, T>() where T : unmanaged where X : unmanaged
        {
            if (sizeof(X) != sizeof(T))
            {
                throw new InvalidCastException("Size mismatch between types");
            }
        }

        /// <summary>
        /// Retrieves the pointer to this span.
        /// </summary>
        public static T* GetPointer<T>(this ReadOnlySpan<T> span) where T : unmanaged
        {
            fixed (T* pointer = span)
            {
                return pointer;
            }
        }

        /// <summary>
        /// Retrieves the pointer to this span.
        /// </summary>
        public static T* GetPointer<T>(this Span<T> span) where T : unmanaged
        {
            fixed (T* pointer = span)
            {
                return pointer;
            }
        }

        /// <summary>
        /// Retrieves the native address of the given <paramref name="span"/>.
        /// </summary>
        public static nint GetAddress<T>(this ReadOnlySpan<T> span) where T : unmanaged
        {
            fixed (T* pointer = span)
            {
                return (nint)pointer;
            }
        }

        /// <summary>
        /// Retrieves the native address of the given <paramref name="span"/>.
        /// </summary>
        public static nint GetAddress<T>(this Span<T> span) where T : unmanaged
        {
            fixed (T* pointer = span)
            {
                return (nint)pointer;
            }
        }

        /// <summary>
        /// Reinterprets the input <paramref name="span"/> as a <see cref="Span{T}"/> of a different type.
        /// </summary>
        public static Span<X> Reinterpret<T, X>(this Span<T> span) where T : unmanaged where X : unmanaged
        {
            fixed (T* pointer = span)
            {
                return new(pointer, span.Length * sizeof(T) / sizeof(X));
            }
        }

        /// <summary>
        /// Reinterprets the input <paramref name="span"/> as a <see cref="ReadOnlySpan{T}"/> of a different type.
        /// </summary>
        public static ReadOnlySpan<X> Reinterpret<T, X>(this ReadOnlySpan<T> span) where T : unmanaged where X : unmanaged
        {
            fixed (T* pointer = span)
            {
                return new(pointer, span.Length * sizeof(T) / sizeof(X));
            }
        }

        /// <summary>
        /// Casts the input <paramref name="span"/> as a <see cref="Span{T}"/> of a different type.
        /// </summary>
        public static Span<X> As<T, X>(this Span<T> span) where T : unmanaged where X : unmanaged
        {
            ThrowIfSizeMismatch<X, T>();

            fixed (T* pointer = span)
            {
                return new(pointer, span.Length);
            }
        }

        /// <summary>
        /// Casts the input <paramref name="span"/> as a <see cref="Span{T}"/> of a different type.
        /// </summary>
        public static ReadOnlySpan<X> As<T, X>(this ReadOnlySpan<T> span) where T : unmanaged where X : unmanaged
        {
            ThrowIfSizeMismatch<X, T>();

            fixed (T* pointer = span)
            {
                return new(pointer, span.Length);
            }
        }

        /// <summary>
        /// Fills the given <paramref name="destination"/> with the string representation of the <typeparamref name="T"/> value.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
#if NET
        public static int ToString<T>(this T formattable, Span<char> destination) where T : unmanaged, ISpanFormattable
        {
            return formattable.TryFormat(destination, out int charsWritten, default, default) ? charsWritten : 0;
        }
#else
        public static int ToString<T>(this T formattable, Span<char> destination) where T : unmanaged, IFormattable
        {
            string result = formattable.ToString(default, default);
            if (string.IsNullOrEmpty(result))
            {
                return 0;
            }

            result.AsSpan().CopyTo(destination);
            return result.Length;
        }

        public static int ToString(this nint value, Span<char> destination)
        {
            string result = value.ToString();
            result.AsSpan().CopyTo(destination);
            return result.Length;
        }

        public static int ToString(this nuint value, Span<char> destination)
        {
            string result = value.ToString();
            result.AsSpan().CopyTo(destination);
            return result.Length;
        }
#endif

        /// <summary>
        /// Fills the given <paramref name="destination"/> with the string representation of the given <see cref="Vector2"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static int ToString(this Vector2 vector, Span<char> destination)
        {
            int length = 0;
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
        public static int ToString(this Vector3 vector, Span<char> destination)
        {
            int length = 0;
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
        public static int ToString(this Vector4 vector, Span<char> destination)
        {
            int length = 0;
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
        public static int ToString(this Quaternion quaternion, Span<char> destination)
        {
            int length = 0;
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
        /// Attempts to retrieve the index of the first occurrence of the given <paramref name="value"/>.
        /// </summary>
        /// <returns><see langword="true"/> if contained.</returns>
        public static bool TryIndexOf<T>(this Span<T> span, T value, out int index) where T : unmanaged, IEquatable<T>
        {
            index = span.IndexOf(value);
            return index != -1;
        }

        /// <summary>
        /// Attempts to retrieve the index of the first occurrence of the given <paramref name="value"/>.
        /// </summary>
        /// <returns><see langword="true"/> if contained.</returns>
        public static bool TryIndexOf<T>(this ReadOnlySpan<T> span, T value, out int index) where T : unmanaged, IEquatable<T>
        {
            index = span.IndexOf(value);
            return index != -1;
        }

        /// <summary>
        /// Attempts to retrieve the index of the last occurrence of the given <paramref name="value"/>.
        /// </summary>
        /// <returns><see langword="true"/> if contained.</returns>
        public static bool TryLastIndexOf<T>(this Span<T> span, T value, out int index) where T : unmanaged, IEquatable<T>
        {
            index = span.LastIndexOf(value);
            return index != -1;
        }

        /// <summary>
        /// Attempts to retrieve the index of the last occurrence of the given <paramref name="value"/>.
        /// </summary>
        /// <returns><see langword="true"/> if contained.</returns>
        public static bool TryLastIndexOf<T>(this ReadOnlySpan<T> span, T value, out int index) where T : unmanaged, IEquatable<T>
        {
            index = span.LastIndexOf(value);
            return index != -1;
        }

        /// <summary>
        /// Attempts to retrieve the index of the first occurrence of the given <paramref name="value"/>.
        /// </summary>
        /// <returns><see langword="true"/> if contained.</returns>
        public static bool TryIndexOf<T>(this Span<T> span, ReadOnlySpan<T> value, out int index) where T : unmanaged, IEquatable<T>
        {
            index = span.IndexOf(value);
            return index != -1;
        }

        /// <summary>
        /// Attempts to retrieve the index of the first occurrence of the given <paramref name="value"/>.
        /// </summary>
        /// <returns><see langword="true"/> if contained.</returns>
        public static bool TryIndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value, out int index) where T : unmanaged, IEquatable<T>
        {
            index = span.IndexOf(value);
            return index != -1;
        }

        /// <summary>
        /// Attempts to retrieve the index of the last occurrence of the given <paramref name="value"/>.
        /// </summary>
        /// <returns><see langword="true"/> if contained.</returns>
        public static bool TryLastIndexOf<T>(this Span<T> span, ReadOnlySpan<T> value, out int index) where T : unmanaged, IEquatable<T>
        {
            index = span.LastIndexOf(value);
            return index != -1;
        }

        /// <summary>
        /// Attempts to retrieve the index of the last occurrence of the given <paramref name="value"/>.
        /// </summary>
        /// <returns><see langword="true"/> if contained.</returns>
        public static bool TryLastIndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value, out int index) where T : unmanaged, IEquatable<T>
        {
            index = span.LastIndexOf(value);
            return index != -1;
        }

        /// <summary>
        /// Gets a single UTF8 character at the given <paramref name="bytePosition"/>.
        /// </summary>
        /// <returns>Amount of <see cref="byte"/> values read.</returns>
        public static byte GetUTF8Character(this Span<byte> bytes, int bytePosition, out char low, out char high)
        {
            return GetUTF8Character((ReadOnlySpan<byte>)bytes, bytePosition, out low, out high);
        }

        /// <summary>
        /// Gets a single UTF8 character at the given <paramref name="bytePosition"/>.
        /// </summary>
        /// <returns>Amount of <see cref="byte"/> values read.</returns>
        public static byte GetUTF8Character(this ReadOnlySpan<byte> bytes, int bytePosition, out char low, out char high)
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

            for (int j = 1; j <= additional; j++)
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
        /// <returns>Amount of <see cref="char"/> values read.</returns>
        public static int GetUTF8Characters(this Span<byte> bytes, int bytePosition, int length, Span<char> destination)
        {
            return GetUTF8Characters((ReadOnlySpan<byte>)bytes, bytePosition, length, destination);
        }

        /// <summary>
        /// Reads the bytes formatted as UTF8 into the given <paramref name="destination"/>.
        /// <para>Reads until until a <see langword="default"/> character is found,
        /// or when the <paramref name="length"/> amount of <see cref="char"/> values have been read.</para>
        /// </summary>
        /// <returns>Amount of <see cref="char"/> values read.</returns>
        public static int GetUTF8Characters(this ReadOnlySpan<byte> bytes, int bytePosition, int length, Span<char> destination)
        {
            int charactersRead = 0;
            int byteIndex = 0;
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
        public static int GetUTF8Length(this Span<byte> bytes)
        {
            int bytePosition = 0;
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