using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Pointer = Unmanaged.Pointers.Text;

namespace Unmanaged
{
    /// <summary>
    /// Container of variable length text.
    /// </summary>
    public unsafe struct Text : IDisposable, IEquatable<Text>, IEnumerable<char>, IReadOnlyList<char>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Pointer* text;

        /// <summary>
        /// Length of the text.
        /// </summary>
        public readonly uint Length
        {
            get
            {
                MemoryAddress.ThrowIfDefault(text);

                return text->length;
            }
        }

        /// <summary>
        /// Checks if this text has been disposed.
        /// </summary>
        public readonly bool IsDisposed => text is null;

        /// <summary>
        /// Checks if the text is empty.
        /// </summary>
        public readonly bool IsEmpty
        {
            get
            {
                MemoryAddress.ThrowIfDefault(text);

                return text->length == 0;
            }
        }

        /// <summary>
        /// Indexer for the text.
        /// </summary>
        public readonly ref char this[uint index]
        {
            get
            {
                MemoryAddress.ThrowIfDefault(text);
                ThrowIfOutOfRange(index);

                return ref text->buffer.ReadElement<char>(index);
            }
        }

        /// <summary>
        /// Native address of the text.
        /// </summary>
        public readonly nint Address => (nint)text;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly string Value => ToString();

        readonly int IReadOnlyCollection<char>.Count => (int)Length;

        readonly char IReadOnlyList<char>.this[int index]
        {
            get
            {
                ThrowIfOutOfRange((uint)index);

                return text->buffer.ReadElement<char>((uint)index);
            }
        }

#if NET
        /// <summary>
        /// Creates an empty text container.
        /// </summary>
        public Text()
        {
            ref Pointer text = ref MemoryAddress.Allocate<Pointer>();
            text.length = 0;
            text.buffer = MemoryAddress.Allocate((uint)0);
            fixed (Pointer* pointer = &text)
            {
                this.text = pointer;
            }
        }
#endif

        /// <summary>
        /// Creates a text container with the given <paramref name="length"/>.
        /// </summary>
        public Text(uint length, char defaultCharacter = ' ')
        {
            ref Pointer text = ref MemoryAddress.Allocate<Pointer>();
            text.length = length;
            text.buffer = MemoryAddress.Allocate(length * sizeof(char));
            fixed (Pointer* pointer = &text)
            {
                this.text = pointer;
            }

            new USpan<char>(text.buffer.Pointer, length).Fill(defaultCharacter);
        }

        /// <summary>
        /// Creates a container of the given <paramref name="content"/>.
        /// </summary>
        public Text(USpan<char> content)
        {
            ref Pointer text = ref MemoryAddress.Allocate<Pointer>();
            text.length = content.Length;
            text.buffer = MemoryAddress.Allocate(content);
            fixed (Pointer* pointer = &text)
            {
                this.text = pointer;
            }
        }

        /// <summary>
        /// Creates a container of the given <paramref name="content"/>.
        /// </summary>
        public Text(IEnumerable<char> content)
        {
            ref Pointer text = ref MemoryAddress.Allocate<Pointer>();
            text.length = 0;
            text.buffer = MemoryAddress.Allocate((uint)0);
            fixed (Pointer* pointer = &text)
            {
                this.text = pointer;
            }

            Append(content);
        }

        /// <summary>
        /// Creates a container of the given <paramref name="content"/>.
        /// </summary>
        public Text(string content)
        {
            ref Pointer text = ref MemoryAddress.Allocate<Pointer>();
            text.length = (uint)content.Length;
            USpan<char> contentSpan = content.AsSpan();
            text.buffer = MemoryAddress.Allocate(contentSpan);
            fixed (Pointer* pointer = &text)
            {
                this.text = pointer;
            }
        }

        /// <summary>
        /// Creates an instance from an existing <paramref name="pointer"/>.
        /// </summary>
        public Text(void* pointer)
        {
            text = (Pointer*)pointer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(text);

            text->buffer.Dispose();
            MemoryAddress.Free(ref text);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(uint index)
        {
            if (index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <summary>
        /// Retrieves this text as a span.
        /// </summary>
        public readonly USpan<char> AsSpan()
        {
            MemoryAddress.ThrowIfDefault(text);

            return new USpan<char>(text->buffer.Pointer, text->length);
        }

        /// <summary>
        /// Clears the text.
        /// </summary>
        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(text);

            text->length = 0;
        }

        /// <summary>
        /// Copies this text into the given <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(USpan<char> destination)
        {
            MemoryAddress.ThrowIfDefault(text);

            new USpan<char>(text->buffer.Pointer, text->length).CopyTo(destination);
        }

        /// <summary>
        /// Copies <paramref name="source"/> into this text.
        /// </summary>
        public readonly void CopyFrom(USpan<char> source)
        {
            MemoryAddress.ThrowIfDefault(text);

            if (text->length != source.Length)
            {
                text->length = source.Length;
                MemoryAddress.Resize(ref text->buffer, source.Length * sizeof(char));
            }

            source.CopyTo(new USpan<char>(text->buffer.Pointer, source.Length));
        }

        /// <summary>
        /// Copies <paramref name="source"/> into this text.
        /// </summary>
        public readonly void CopyFrom(IEnumerable<char> source)
        {
            MemoryAddress.ThrowIfDefault(text);

            uint length = 0;
            foreach (char character in source)
            {
                length++;
            }

            if (text->length != length)
            {
                text->length = length;
                MemoryAddress.Resize(ref text->buffer, length * sizeof(char));
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
            MemoryAddress.ThrowIfDefault(text);

            USpan<char> buffer = stackalloc char[(int)text->length];
            ToString(buffer);
            return buffer.ToString();
        }

        /// <summary>
        /// Copies this text into the given <paramref name="destination"/>.
        /// </summary>
        public readonly uint ToString(USpan<char> destination)
        {
            MemoryAddress.ThrowIfDefault(text);

            USpan<char> source = AsSpan();
            uint copyLength = Math.Min(text->length, destination.Length);
            source.GetSpan(copyLength).CopyTo(destination);
            return copyLength;
        }

        /// <summary>
        /// Modifies the length of this text.
        /// </summary>
        public readonly void SetLength(uint newLength, char defaultCharacter = ' ')
        {
            MemoryAddress.ThrowIfDefault(text);

            if (newLength == text->length)
            {
                return;
            }

            uint oldLength = text->length;
            text->length = newLength;
            if (newLength > oldLength)
            {
                uint copyLength = newLength - oldLength;
                MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
                text->buffer.AsSpan<char>(oldLength, copyLength).Fill(defaultCharacter);
            }
        }

        /// <summary>
        /// Appends a single <paramref name="character"/>.
        /// </summary>
        public readonly void Append(char character, uint repeat = 1)
        {
            uint length = Length;
            SetLength(length + repeat, character);
        }

        /// <summary>
        /// Appends the given <paramref name="text"/>.
        /// </summary>
        public readonly void Append(string text)
        {
            Append(text.AsSpan());
        }

        /// <summary>
        /// Appends the given <paramref name="text"/>.
        /// </summary>
        public readonly void Append(USpan<char> text)
        {
            uint length = Length;
            uint newLength = length + text.Length;
            SetLength(newLength);
            text.CopyTo(this.text->buffer.AsSpan<char>(length, text.Length));
        }

        /// <summary>
        /// Removes the character at <paramref name="index"/>.
        /// </summary>
        public readonly void RemoveAt(uint index)
        {
            ThrowIfOutOfRange(index);

            ref uint length = ref text->length;
            if (index < length - 1)
            {
                USpan<char> buffer = AsSpan();
                for (uint i = index; i < length - 1; i++)
                {
                    buffer[i] = buffer[i + 1];
                }
            }

            length--;
        }

        /// <summary>
        /// Checks if this text ends with <paramref name="otherText"/>.
        /// </summary>
        public readonly bool EndsWith(string otherText)
        {
            return EndsWith(otherText.AsSpan());
        }

        /// <summary>
        /// Checks if this text ends with <paramref name="otherText"/>.
        /// </summary>
        public readonly bool EndsWith(Text otherText)
        {
            return EndsWith(otherText.AsSpan());
        }

        /// <summary>
        /// Checks if this text starts with <paramref name="otherText"/>.
        /// </summary>
        public readonly bool StartsWith(string otherText)
        {
            return StartsWith(otherText.AsSpan());
        }

        /// <summary>
        /// Checks if this text starts with <paramref name="otherText"/>.
        /// </summary>
        public readonly bool StartsWith(Text otherText)
        {
            return StartsWith(otherText.AsSpan());
        }

        /// <summary>
        /// Checks if this text ends with <paramref name="otherText"/>.
        /// </summary>
        public readonly bool EndsWith(USpan<char> otherText)
        {
            uint length = Length;
            uint textLength = otherText.Length;
            if (length < textLength)
            {
                return false;
            }

            USpan<char> buffer = AsSpan();
            return buffer.Slice(length - textLength).SequenceEqual(otherText);
        }

        /// <summary>
        /// Checks if this text starts with <paramref name="otherText"/>.
        /// </summary>
        public readonly bool StartsWith(USpan<char> otherText)
        {
            uint length = Length;
            uint textLength = otherText.Length;
            if (length < textLength)
            {
                return false;
            }

            USpan<char> buffer = AsSpan();
            return buffer.GetSpan(textLength).SequenceEqual(otherText);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Text text && Equals(text);
        }

        /// <summary>
        /// Checks if this text equals to <paramref name="otherText"/>.
        /// </summary>
        public readonly bool Equals(Text otherText)
        {
            if (Length != otherText.Length)
            {
                return false;
            }

            return AsSpan().SequenceEqual(otherText.AsSpan());
        }

        /// <summary>
        /// Checks if this text equals to <paramref name="otherText"/>.
        /// </summary>
        public readonly bool Equals(string otherText)
        {
            if (Length != otherText.Length)
            {
                return false;
            }

            return AsSpan().SequenceEqual(otherText.AsSpan());
        }

        /// <summary>
        /// Checks if this text equals to <paramref name="otherText"/>.
        /// </summary>
        public readonly bool Equals(USpan<char> otherText)
        {
            if (Length != otherText.Length)
            {
                return false;
            }

            return AsSpan().SequenceEqual(otherText);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                uint length = Length;
                hash = hash * 23 + length.GetHashCode();
                for (uint i = 0; i < length; i++)
                {
                    char c = text->buffer.ReadElement<char>(i);
                    hash = hash * 23 + c.GetHashCode();
                }

                return hash;
            }
        }

        /// <summary>
        /// Appends the given <paramref name="character"/> to <paramref name="originalText"/>
        /// and copies the result to the <paramref name="destination"/>.
        /// </summary>
        public static void Append(USpan<char> originalText, char character, USpan<char> destination)
        {
            originalText.CopyTo(destination.GetSpan(originalText.Length));
            destination[originalText.Length] = character;
        }

        /// <summary>
        /// Appends the given <paramref name="newText"/> to <paramref name="originalText"/>
        /// and copies the result to the <paramref name="destination"/>.
        /// </summary>
        public static void Append(USpan<char> originalText, string newText, USpan<char> destination)
        {
            Append(originalText, newText.AsSpan(), destination);
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
            originalText.CopyTo(destination.GetSpan(originalLength));
            newText.GetSpan(copyLength - originalLength).CopyTo(destination.Slice(originalLength));
        }

        /// <summary>
        /// Replaces all occurrences of <paramref name="target"/> with <paramref name="replacement"/>,
        /// and copies the result to the <paramref name="destination"/>.
        /// </summary>
        /// <returns>The length copied into the <paramref name="destination"/>.</returns>
        public static uint Replace(USpan<char> originalText, string target, string replacement, USpan<char> destination)
        {
            return Replace(originalText, target.AsSpan(), replacement.AsSpan(), destination);
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
        public readonly Span<char>.Enumerator GetEnumerator()
        {
            return AsSpan().GetEnumerator();
        }

        readonly IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <inheritdoc/>
        public struct Enumerator : IEnumerator<char>
        {
            private readonly Text text;
            private int index;

            /// <inheritdoc/>
            public readonly char Current => text[(uint)index];

            readonly object IEnumerator.Current => Current;

            /// <inheritdoc/>
            public Enumerator(Text text)
            {
                this.text = text;
                index = -1;
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                return ++index < text.Length;
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

        /// <inheritdoc/>
        public static bool operator ==(Text left, string right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Text left, string right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static bool operator ==(string left, Text right)
        {
            return right.Equals(left);
        }

        /// <inheritdoc/>
        public static bool operator !=(string left, Text right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Concatenates <paramref name="left"/> and <paramref name="right"/> texts
        /// into a new instance.
        /// </summary>
        public static Text operator +(Text left, Text right)
        {
            Text result = new(left.AsSpan());
            result.Append(right.AsSpan());
            return result;
        }
    }
}