using System;
using System.Numerics;

namespace Unmanaged
{
    /// <summary>
    /// Extension functions for <see cref="USpan{T}"/>.
    /// </summary>
    public unsafe static class USpanFunctions
    {
        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="byte"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this byte value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="sbyte"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this sbyte value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="short"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this short value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="ushort"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this ushort value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="int"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this int value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="uint"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this uint value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="long"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this long value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="ulong"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this ulong value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="float"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this float value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="double"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this double value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="decimal"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this decimal value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="bool"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this bool value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="nint"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this nint value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
#if NET
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
#else
            return ToString((long)value, buffer);
#endif
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="nuint"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this nuint value, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
#if NET
            return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
#else
            return ToString((ulong)value, buffer);
#endif
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="DateTime"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this DateTime dateTime, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return dateTime.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="TimeSpan"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this TimeSpan timeSpan, USpan<char> buffer)
        {
            Span<char> systemSpan = buffer;
            return timeSpan.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="Vector2"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this Vector2 vector, USpan<char> buffer)
        {
            uint length = 0;
            length += vector.X.ToString(buffer.Slice(length));
            buffer[length++] = ',';
            buffer[length++] = ' ';
            length += vector.Y.ToString(buffer.Slice(length));
            return length;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="Vector3"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this Vector3 vector, USpan<char> buffer)
        {
            uint length = 0;
            length += vector.X.ToString(buffer.Slice(length));
            buffer[length++] = ',';
            buffer[length++] = ' ';
            length += vector.Y.ToString(buffer.Slice(length));
            buffer[length++] = ',';
            buffer[length++] = ' ';
            length += vector.Z.ToString(buffer.Slice(length));
            return length;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="Vector4"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this Vector4 vector, USpan<char> buffer)
        {
            uint length = 0;
            length += vector.X.ToString(buffer.Slice(length));
            buffer[length++] = ',';
            buffer[length++] = ' ';
            length += vector.Y.ToString(buffer.Slice(length));
            buffer[length++] = ',';
            buffer[length++] = ' ';
            length += vector.Z.ToString(buffer.Slice(length));
            buffer[length++] = ',';
            buffer[length++] = ' ';
            length += vector.W.ToString(buffer.Slice(length));
            return length;
        }

        /// <summary>
        /// Fills the given buffer with the string representation of the given <see cref="Quaternion"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public static uint ToString(this Quaternion quaternion, USpan<char> buffer)
        {
            uint length = 0;
            length += quaternion.X.ToString(buffer.Slice(length));
            buffer[length++] = ',';
            buffer[length++] = ' ';
            length += quaternion.Y.ToString(buffer.Slice(length));
            buffer[length++] = ',';
            buffer[length++] = ' ';
            length += quaternion.Z.ToString(buffer.Slice(length));
            buffer[length++] = ',';
            buffer[length++] = ' ';
            length += quaternion.W.ToString(buffer.Slice(length));
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
    }
}