﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Unmanaged
{
    /// <summary>
    /// Container of variable length text.
    /// </summary>
    public unsafe struct Text : IDisposable, IEquatable<Text>, IEnumerable<char>, IReadOnlyList<char>
    {
        private const uint Stride = 2;

        private Implementation* value;

        /// <summary>
        /// Length of the text.
        /// </summary>
        public readonly uint Length => value->length;

        /// <summary>
        /// Indexer for the text.
        /// </summary>
        public readonly ref char this[uint index] => ref value->buffer.Read<char>(index * Stride);

        /// <summary>
        /// Native address of the text.
        /// </summary>
        public readonly nint Address => (nint)value;

        readonly int IReadOnlyCollection<char>.Count => (int)Length;
        readonly char IReadOnlyList<char>.this[int index] => value->buffer.Read<char>((uint)index * Stride);

#if NET
        /// <summary>
        /// Creates an empty text container.
        /// </summary>
        public Text()
        {
            value = Implementation.Allocate(0);
        }
#endif

        /// <summary>
        /// Creates a text container with the given <paramref name="length"/>.
        /// </summary>
        public Text(uint length, char defaultCharacter = ' ')
        {
            value = Implementation.Allocate(length);
            value->buffer.AsSpan<char>(0, length).Fill(defaultCharacter);
        }

        /// <summary>
        /// Creates a container of the given <paramref name="text"/>.
        /// </summary>
        public Text(USpan<char> text)
        {
            value = Implementation.Allocate(text.Length);
            text.CopyTo(value->buffer.AsSpan<char>(0, text.Length));
        }

        /// <summary>
        /// Creates a container of the given <paramref name="text"/>.
        /// </summary>
        public Text(IEnumerable<char> text)
        {
            value = Implementation.Allocate(0);
            Append(text);
        }

        /// <summary>
        /// Creates an instance from an existing native <paramref name="address"/>.
        /// </summary>
        public Text(nint address)
        {
            value = (Implementation*)address;
        }

        /// <summary>
        /// Creates an instance from an existing <paramref name="pointer"/>.
        /// </summary>
        public Text(void* pointer)
        {
            value = (Implementation*)pointer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Implementation.Free(ref value);
            value = default;
        }

        /// <summary>
        /// Retrieves this text as a span.
        /// </summary>
        public readonly USpan<char> AsSpan()
        {
            return value->buffer.AsSpan<char>(0, Length);
        }

        /// <summary>
        /// Clears the text.
        /// </summary>
        public readonly void Clear()
        {
            value->length = 0;
        }

        /// <summary>
        /// Copies this text into the given <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(USpan<char> destination)
        {
            value->buffer.AsSpan<char>(0, Length).CopyTo(destination);
        }

        /// <summary>
        /// Copies <paramref name="source"/> into this text.
        /// </summary>
        public readonly void CopyFrom(USpan<char> source)
        {
            if (value->length != source.Length)
            {
                value->length = source.Length;
                Allocation.Resize(ref value->buffer, source.Length * Stride);
            }

            source.CopyTo(value->buffer.AsSpan<char>(0, source.Length));
        }

        /// <summary>
        /// Copies <paramref name="source"/> into this text.
        /// </summary>
        public readonly void CopyFrom(IEnumerable<char> source)
        {
            uint length = 0;
            foreach (char character in source)
            {
                length++;
            }

            if (value->length != length)
            {
                value->length = length;
                Allocation.Resize(ref value->buffer, length * Stride);
            }

            USpan<char> buffer = AsSpan();
            length = 0;
            foreach (char character in source)
            {
                buffer[length++] = character;
            }
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            uint length = Length;
            USpan<char> buffer = stackalloc char[(int)length];
            ToString(buffer);
            return buffer.ToString();
        }

        /// <summary>
        /// Copies this text into the given <paramref name="destination"/>.
        /// </summary>
        public readonly uint ToString(USpan<char> destination)
        {
            uint length = Length;
            USpan<char> source = AsSpan();
            uint copyLength = Math.Min(length, destination.Length);
            source.Slice(0, copyLength).CopyTo(destination);
            return copyLength;
        }

        /// <summary>
        /// Modifies the length of this text.
        /// </summary>
        public readonly void SetLength(uint newLength, char defaultCharacter = ' ')
        {
            uint oldLength = Length;
            if (newLength == oldLength)
            {
                return;
            }

            value->length = newLength;
            if (newLength > oldLength)
            {
                uint copyLength = newLength - oldLength;
                Allocation.Resize(ref value->buffer, newLength * Stride);
                value->buffer.AsSpan<char>(oldLength, copyLength).Fill(defaultCharacter);
            }
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Text text && Equals(text);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Text other)
        {
            return value == other.value;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        /// <summary>
        /// Appends the given <paramref name="character"/> to <paramref name="originalText"/>
        /// and copies the result to the <paramref name="destination"/>.
        /// </summary>
        public static void Append(USpan<char> originalText, char character, USpan<char> destination)
        {
            originalText.CopyTo(destination.Slice(0, originalText.Length));
            destination[originalText.Length] = character;
        }

        /// <summary>
        /// Appends the given <paramref name="newText"/> to <paramref name="originalText"/>
        /// and copies the result to the <paramref name="destination"/>.
        /// </summary>
        public static void Append(USpan<char> originalText, string newText, USpan<char> destination)
        {
            Append(originalText, newText.AsUSpan(), destination);
        }

        /// <summary>
        /// Appends the given <paramref name="newText"/> to <paramref name="originalText"/>
        /// and copies the result to the <paramref name="destination"/>.
        /// </summary>
        public static void Append(USpan<char> originalText, USpan<char> newText, USpan<char> destination)
        {
            uint originalLength = originalText.Length;
            uint newLength = newText.Length;
            uint destinationLength = destination.Length;
            uint copyLength = Math.Min(originalLength + newLength, destinationLength);
            originalText.CopyTo(destination.Slice(0, originalLength));
            newText.Slice(0, copyLength - originalLength).CopyTo(destination.Slice(originalLength));
        }

        /// <summary>
        /// Replaces all occurrences of <paramref name="target"/> with <paramref name="replacement"/>,
        /// and copies the result to the <paramref name="destination"/>.
        /// </summary>
        public static uint Replace(USpan<char> originalText, string target, string replacement, USpan<char> destination)
        {
            return Replace(originalText, target.AsUSpan(), replacement.AsUSpan(), destination);
        }

        /// <summary>
        /// Replaces all occurrences of <paramref name="target"/> with <paramref name="replacement"/>,
        /// and copies the result to the <paramref name="destination"/>.
        /// </summary>
        public static uint Replace(USpan<char> originalText, USpan<char> target, USpan<char> replacement, USpan<char> destination)
        {
            uint length = 0;
            uint originalStart = 0;
            uint destinationStart = 0;
            while (true)
            {
                if (originalText.Slice(originalStart).TryIndexOf(target, out uint index))
                {
                    originalText.Slice(originalStart).CopyTo(destination.Slice(destinationStart));
                    replacement.CopyTo(destination.Slice(destinationStart + index));
                    originalStart += index + target.Length;
                    destinationStart += index + replacement.Length;
                    length += index + replacement.Length;
                }
                else
                {
                    originalText.Slice(originalStart).CopyTo(destination.Slice(destinationStart));
                    length += originalText.Length - originalStart;
                    return length;
                }
            }
        }

        /// <summary>
        /// Appends the given <paramref name="text"/>.
        /// </summary>
        public readonly void Append(IEnumerable<char> text)
        {
            uint length = Length;
            foreach (char character in text)
            {
                SetLength(length + 1, character);
                length++;
            }
        }

        /// <inheritdoc/>
        public readonly Enumerator GetEnumerator()
        {
            return new(value);
        }

        readonly IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            return GetEnumerator();
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        public struct Implementation
        {
            /// <inheritdoc/>
            public uint length;

            /// <inheritdoc/>
            public Allocation buffer;

            /// <summary>
            /// Allocates new text with the given <paramref name="length"/>.
            /// </summary>
            public static Implementation* Allocate(uint length)
            {
                Implementation* text = Allocations.Allocate<Implementation>();
                text->length = length;
                text->buffer = new(length * Stride);
                return text;
            }

            /// <summary>
            /// Frees the given <paramref name="text"/>.
            /// </summary>
            public static void Free(ref Implementation* text)
            {
                Allocations.ThrowIfNull(text);

                text->buffer.Dispose();
                Allocations.Free(ref text);
            }
        }

        /// <inheritdoc/>
        public struct Enumerator : IEnumerator<char>
        {
            private readonly Implementation* text;
            private int index;

            /// <inheritdoc/>
            public readonly char Current => text->buffer.AsSpan<char>(0, text->length)[(uint)index];

            readonly object IEnumerator.Current => Current;

            /// <inheritdoc/>
            public Enumerator(Implementation* text)
            {
                this.text = text;
                index = -1;
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                return ++index < text->length;
            }

            /// <inheritdoc/>
            public void Reset()
            {
                index = -1;
            }

            /// <inheritdoc/>
            public readonly void Dispose()
            {
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(Text left, Text right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Text left, Text right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Appends text.
        /// </summary>
        public static Text operator +(Text left, Text right)
        {
            Text result = new(left);
            result.Append(right);
            return result;
        }
    }
}
