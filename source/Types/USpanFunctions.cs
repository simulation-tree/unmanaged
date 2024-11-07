using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unmanaged;

public unsafe static class USpanFunctions
{
    [Conditional("DEBUG")]
    private static void ThrowIfStringObjectIsNull(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }
    }

    /// <summary>
    /// Retrieves a span containing the given text.
    /// </summary>
    public static USpan<char> AsUSpan(this string text)
    {
        ThrowIfStringObjectIsNull(text);
        fixed (char* pointer = text)
        {
            return new USpan<char>(pointer, (uint)text.Length);
        }
    }

    public static uint ToString(this byte value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this sbyte value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this short value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this ushort value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this int value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this uint value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this long value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this ulong value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this float value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this double value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this decimal value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this bool value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this nint value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
#if NET
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
#else
        return ToString((long)value, buffer);
#endif
    }

    public static uint ToString(this nuint value, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
#if NET
        return value.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
#else
        return ToString((ulong)value, buffer);
#endif
    }

    public static uint ToString(this DateTime dateTime, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return dateTime.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this TimeSpan timeSpan, USpan<char> buffer)
    {
        Span<char> systemSpan = buffer.AsSystemSpan();
        return timeSpan.TryFormat(systemSpan, out int charsWritten) ? (uint)charsWritten : 0;
    }

    public static uint ToString(this Vector2 vector, USpan<char> buffer)
    {
        uint length = 0;
        length += vector.X.ToString(buffer.Slice(length));
        buffer[length++] = ',';
        buffer[length++] = ' ';
        length += vector.Y.ToString(buffer.Slice(length));
        return length;
    }

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
}