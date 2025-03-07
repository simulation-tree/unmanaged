﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Pointer = Unmanaged.Pointers.Text;

namespace Unmanaged
{
    /// <summary>
    /// Container of variable length text.
    /// </summary>
    public unsafe struct Text : IDisposable, IEquatable<Text>, IList<char>, IReadOnlyList<char>
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
        readonly int ICollection<char>.Count => (int)Length;
        readonly bool ICollection<char>.IsReadOnly => false;
        readonly char IList<char>.this[int index]
        {
            get => this[(uint)index];
            set => this[(uint)index] = value;
        }

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
        public Text(IReadOnlyCollection<char> content)
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
                throw new ArgumentOutOfRangeException($"Index {index} is out of range for text with length {Length}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfGreaterThanRange(uint index)
        {
            if (index > Length)
            {
                throw new ArgumentOutOfRangeException($"Index {index} is greater than the length of the text {Length}");
            }
        }

        /// <summary>
        /// Borrows a copy of this text.
        /// </summary>
        public readonly Borrowed Borrow()
        {
            return new(this);
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
        /// Makes this text match <paramref name="source"/> exactly.
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
        /// Makes this text match <paramref name="source"/> exactly.
        /// </summary>
        public readonly void CopyFrom(string source)
        {
            MemoryAddress.ThrowIfDefault(text);

            if (text->length != source.Length)
            {
                text->length = (uint)source.Length;
                MemoryAddress.Resize(ref text->buffer, (uint)source.Length * sizeof(char));
            }

            source.CopyTo(new USpan<char>(text->buffer.Pointer, (uint)source.Length));
        }

        /// <summary>
        /// Makes this text match <paramref name="source"/> exactly.
        /// </summary>
        public readonly void CopyFrom(ASCIIText256 source)
        {
            MemoryAddress.ThrowIfDefault(text);

            if (text->length != source.Length)
            {
                text->length = source.Length;
                MemoryAddress.Resize(ref text->buffer, (uint)source.Length * sizeof(char));
            }

            source.CopyTo(new USpan<char>(text->buffer.Pointer, source.Length));
        }

        /// <summary>
        /// Makes this text match <paramref name="source"/> exactly.
        /// </summary>
        public readonly void CopyFrom(IReadOnlyCollection<char> source)
        {
            MemoryAddress.ThrowIfDefault(text);

            uint length = (uint)source.Count;
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
        /// Retrieves a slice of the remaining text starting at <paramref name="start"/>.
        /// </summary>
        public readonly USpan<char> Slice(uint start)
        {
            MemoryAddress.ThrowIfDefault(text);

            return new USpan<char>(text->buffer.Pointer + start * sizeof(char), text->length - start);
        }

        /// <summary>
        /// Retrieves a slice with <paramref name="length"/> starting at <paramref name="start"/>.
        /// </summary>
        public readonly USpan<char> Slice(uint start, uint length)
        {
            MemoryAddress.ThrowIfDefault(text);

            return new USpan<char>(text->buffer.Pointer + start * sizeof(char), length);
        }

        /// <summary>
        /// Appends a single <paramref name="character"/>.
        /// </summary>
        public readonly void Append(char character)
        {
            MemoryAddress.ThrowIfDefault(text);

            uint newLength = text->length + 1;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            text->buffer.WriteElement(text->length, character);
            text->length = newLength;
        }

        /// <summary>
        /// Appends a single <paramref name="character"/>.
        /// </summary>
        public readonly void Append(char character, uint repeat)
        {
            MemoryAddress.ThrowIfDefault(text);

            uint newLength = text->length + repeat;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            Slice(text->length).Fill(character);
            text->length = newLength;
        }

        /// <summary>
        /// Appends the given <paramref name="otherText"/>.
        /// </summary>
        public readonly void Append(string otherText)
        {
            MemoryAddress.ThrowIfDefault(text);

            uint newLength = text->length + (uint)otherText.Length;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            otherText.CopyTo(Slice(text->length, (uint)otherText.Length));
            text->length = newLength;
        }

        /// <summary>
        /// Appends the given <paramref name="otherText"/>.
        /// </summary>
        public readonly void Append(USpan<char> otherText)
        {
            MemoryAddress.ThrowIfDefault(text);

            uint newLength = text->length + otherText.Length;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            otherText.CopyTo(Slice(text->length, otherText.Length));
            text->length = newLength;
        }

        /// <summary>
        /// Appends the given <paramref name="otherText"/>.
        /// </summary>
        public readonly void Append(IReadOnlyCollection<char> otherText)
        {
            MemoryAddress.ThrowIfDefault(text);

            uint newLength = text->length + (uint)otherText.Count;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            USpan<char> buffer = AsSpan();
            uint index = text->length;
            foreach (char character in otherText)
            {
                buffer[index++] = character;
            }

            text->length = newLength;
        }

        /// <summary>
        /// Appends a new line feed character.
        /// </summary>
        public readonly void AppendLine()
        {
            Append('\n');
        }

        /// <summary>
        /// Appends the given <paramref name="text"/> and a new line feed character.
        /// </summary>
        public readonly void AppendLine(USpan<char> text)
        {
            Append(text);
            AppendLine();
        }

        /// <summary>
        /// Appends the given <paramref name="text"/> and a new line feed character.
        /// </summary>
        public readonly void AppendLine(string text)
        {
            Append(text);
            AppendLine();
        }

        /// <summary>
        /// Appends the given <paramref name="text"/> and a new line feed character.
        /// </summary>
        public readonly void AppendLine(IReadOnlyCollection<char> text)
        {
            Append(text);
            AppendLine();
        }

        /// <summary>
        /// Removes the character at <paramref name="index"/>.
        /// </summary>
        public readonly void RemoveAt(uint index)
        {
            ThrowIfOutOfRange(index);

            uint newLength = text->length - 1;
            if (index < newLength)
            {
                Slice(index + 1).CopyTo(Slice(index));
            }

            text->length = newLength;
        }

        /// <summary>
        /// Retrieves the index for the first occurance of the given <paramref name="character"/>.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public readonly uint IndexOf(char character)
        {
            return AsSpan().IndexOf(character);
        }

        /// <summary>
        /// Retrieves the index for the last occurance of the given <paramref name="character"/>.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public readonly uint LastIndexOf(char character)
        {
            return AsSpan().LastIndexOf(character);
        }

        /// <summary>
        /// Checks if this text ends with <paramref name="otherText"/>.
        /// </summary>
        public readonly bool EndsWith(string otherText)
        {
            return EndsWith(otherText.AsSpan());
        }

        /// <summary>
        /// Checks if the text contains the given <paramref name="character"/>.
        /// </summary>
        public readonly bool Contains(char character)
        {
            return AsSpan().Contains(character);
        }

        /// <summary>
        /// Inserts the <paramref name="character"/> at the given <paramref name="index"/>.
        /// </summary>
        public readonly void Insert(uint index, char character)
        {
            MemoryAddress.ThrowIfDefault(text);
            ThrowIfGreaterThanRange(index);

            uint newLength = text->length + 1;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            if (text->length > index)
            {
                USpan<char> left = Slice(index);
                left.CopyTo(Slice(index + 1, left.Length));
            }

            text->buffer.WriteElement(index, character);
            text->length = newLength;
        }

        /// <summary>
        /// Inserts the <paramref name="otherText"/> at the given <paramref name="index"/>.
        /// </summary>
        public readonly void Insert(uint index, USpan<char> otherText)
        {
            MemoryAddress.ThrowIfDefault(text);
            ThrowIfGreaterThanRange(index);

            uint newLength = text->length + otherText.Length;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            USpan<char> left = Slice(index, otherText.Length);
            if (text->length > index)
            {
                left.CopyTo(Slice(index + otherText.Length, otherText.Length));
            }

            otherText.CopyTo(left);
            text->length = newLength;
        }

        /// <summary>
        /// Inserts the <paramref name="otherText"/> at the given <paramref name="index"/>.
        /// </summary>
        public readonly void Insert(uint index, string otherText)
        {
            MemoryAddress.ThrowIfDefault(text);
            ThrowIfGreaterThanRange(index);

            uint otherTextLength = (uint)otherText.Length;
            uint newLength = text->length + otherTextLength;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            USpan<char> left = Slice(index, otherTextLength);
            if (text->length > index)
            {
                left.CopyTo(Slice(index + otherTextLength, otherTextLength));
            }

            otherText.CopyTo(left);
            text->length = newLength;
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

        readonly int IList<char>.IndexOf(char item)
        {
            unchecked
            {
                return (int)AsSpan().IndexOf(item);
            }
        }

        readonly void IList<char>.Insert(int index, char item)
        {
            Insert((uint)index, item);
        }

        readonly void IList<char>.RemoveAt(int index)
        {
            RemoveAt((uint)index);
        }

        readonly void ICollection<char>.Add(char item)
        {
            Append(item);
        }

        readonly bool ICollection<char>.Contains(char item)
        {
            return Contains(item);
        }

        readonly void ICollection<char>.CopyTo(char[] array, int arrayIndex)
        {
            AsSpan().CopyTo(array.AsSpan(arrayIndex));
        }

        readonly bool ICollection<char>.Remove(char item)
        {
            uint index = IndexOf(item);
            if (index != uint.MaxValue)
            {
                RemoveAt(index);
                return true;
            }

            return false;
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

        /// <summary>
        /// A borrowed copy of a text.
        /// </summary>
        public readonly struct Borrowed : IReadOnlyCollection<char>, IEquatable<Borrowed>
        {
            private readonly Text text;

            /// <summary>
            /// Length of the text.
            /// </summary>
            public readonly uint Length => text.Length;

            int IReadOnlyCollection<char>.Count => (int)text.Length;

            internal Borrowed(Text text)
            {
                this.text = text;
            }

            /// <summary>
            /// Content of the text.
            /// </summary>
            public readonly override string ToString()
            {
                return text.ToString();
            }

            /// <summary>
            /// Checks if this text equals to the <paramref name="other"/> text.
            /// </summary>
            public readonly bool Equals(string other)
            {
                return text.Equals(other);
            }

            /// <summary>
            /// Checks if this text equals to the <paramref name="other"/> text.
            /// </summary>
            public readonly bool Equals(Borrowed other)
            {
                return text.Equals(other.text);
            }

            /// <summary>
            /// Checks if this text equals to the <paramref name="other"/> text.
            /// </summary>
            public readonly bool Equals(USpan<char> other)
            {
                return text.Equals(other);
            }

            /// <summary>
            /// Retrieves the text as a span of <see cref="char"/> values.
            /// </summary>
            public readonly USpan<char> AsSpan()
            {
                return text.AsSpan();
            }

            /// <summary>
            /// Makes this text match <paramref name="otherText"/> exactly.
            /// </summary>
            public readonly void CopyFrom(USpan<char> otherText)
            {
                text.CopyFrom(otherText);
            }

            /// <summary>
            /// Makes this text match <paramref name="otherText"/> exactly.
            /// </summary>
            public readonly void CopyFrom(string otherText)
            {
                text.CopyFrom(otherText);
            }

            readonly IEnumerator<char> IEnumerable<char>.GetEnumerator()
            {
                return ((IEnumerable<char>)text).GetEnumerator();
            }

            readonly IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)text).GetEnumerator();
            }
        }
    }
}