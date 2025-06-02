using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged.Pointers;

namespace Unmanaged
{
    /// <summary>
    /// Container of variable length text.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct Text : IDisposable, IEquatable<Text>, IList<char>, IReadOnlyList<char>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TextPointer* text;

        /// <summary>
        /// Length of the text.
        /// </summary>
        public readonly int Length
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
        public readonly ref char this[int index]
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
            get => this[index];
            set => this[index] = value;
        }

        readonly char IReadOnlyList<char>.this[int index]
        {
            get
            {
                ThrowIfOutOfRange(index);

                return text->buffer.ReadElement<char>(index);
            }
        }

#if NET
        /// <summary>
        /// Creates an empty text container.
        /// </summary>
        public Text()
        {
            text = MemoryAddress.AllocatePointer<TextPointer>();
            text->length = 0;
            text->buffer = MemoryAddress.AllocateEmpty();
        }
#endif

        /// <summary>
        /// Creates a text container with the given <paramref name="length"/>.
        /// </summary>
        public Text(int length, char defaultCharacter = ' ')
        {
            text = MemoryAddress.AllocatePointer<TextPointer>();
            text->length = length;
            text->buffer = MemoryAddress.Allocate(length * sizeof(char));
            new Span<char>(text->buffer.Pointer, length).Fill(defaultCharacter);
        }

        /// <summary>
        /// Creates a container of the given <paramref name="content"/>.
        /// </summary>
        public Text(ReadOnlySpan<char> content)
        {
            text = MemoryAddress.AllocatePointer<TextPointer>();
            text->length = content.Length;
            text->buffer = MemoryAddress.Allocate(content);
        }

        /// <summary>
        /// Creates a container of the given <paramref name="content"/>.
        /// </summary>
        public Text(IReadOnlyCollection<char> content)
        {
            text = MemoryAddress.AllocatePointer<TextPointer>();
            text->length = content.Count;
            text->buffer = MemoryAddress.Allocate(content.Count * sizeof(char));
            Append(content);
        }

        /// <summary>
        /// Creates a container of the given <paramref name="content"/>.
        /// </summary>
        public Text(string content)
        {
            text = MemoryAddress.AllocatePointer<TextPointer>();
            text->length = content.Length;
            text->buffer = MemoryAddress.Allocate(content.AsSpan());
        }

        /// <summary>
        /// Creates an instance from an existing <paramref name="pointer"/>.
        /// </summary>
        public Text(void* pointer)
        {
            text = (TextPointer*)pointer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(text);

            text->buffer.Dispose();
            MemoryAddress.Free(ref text);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(int index)
        {
            if (index >= Length)
            {
                throw new ArgumentOutOfRangeException($"Index {index} is out of range for text with length {Length}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfGreaterThanRange(int index)
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
        public readonly Span<char> AsSpan()
        {
            MemoryAddress.ThrowIfDefault(text);

            return new Span<char>(text->buffer.Pointer, text->length);
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
        public readonly void CopyTo(Span<char> destination)
        {
            MemoryAddress.ThrowIfDefault(text);

            new Span<char>(text->buffer.Pointer, text->length).CopyTo(destination);
        }

        /// <summary>
        /// Makes this text match <paramref name="source"/> exactly.
        /// </summary>
        public readonly void CopyFrom(ReadOnlySpan<char> source)
        {
            MemoryAddress.ThrowIfDefault(text);

            if (text->length != source.Length)
            {
                text->length = source.Length;
                MemoryAddress.Resize(ref text->buffer, source.Length * sizeof(char));
            }

            source.CopyTo(new Span<char>(text->buffer.Pointer, source.Length));
        }

        /// <summary>
        /// Makes this text match <paramref name="source"/> exactly.
        /// </summary>
        public readonly void CopyFrom(string source)
        {
            MemoryAddress.ThrowIfDefault(text);

            if (text->length != source.Length)
            {
                text->length = source.Length;
                MemoryAddress.Resize(ref text->buffer, source.Length * sizeof(char));
            }

            source.AsSpan().CopyTo(new Span<char>(text->buffer.Pointer, source.Length));
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
                MemoryAddress.Resize(ref text->buffer, source.Length * sizeof(char));
            }

            source.CopyTo(new Span<char>(text->buffer.Pointer, source.Length));
        }

        /// <summary>
        /// Makes this text match <paramref name="source"/> exactly.
        /// </summary>
        public readonly void CopyFrom(IReadOnlyCollection<char> source)
        {
            MemoryAddress.ThrowIfDefault(text);

            int length = source.Count;
            if (text->length != length)
            {
                text->length = length;
                MemoryAddress.Resize(ref text->buffer, length * sizeof(char));
            }

            Span<char> buffer = AsSpan();
            length = 0;
            foreach (char character in source)
            {
                buffer[length++] = character;
            }
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            if (IsDisposed)
            {
                return "(Disposed)";
            }
            else
            {
                return AsSpan().ToString();
            }
        }

        /// <summary>
        /// Copies this text into the given <paramref name="destination"/>.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            MemoryAddress.ThrowIfDefault(text);

            Span<char> source = AsSpan();
            int copyLength = Math.Min(text->length, destination.Length);
            source.Slice(0, copyLength).CopyTo(destination);
            return copyLength;
        }

        /// <summary>
        /// Modifies the length of this text.
        /// </summary>
        public readonly void SetLength(int newLength, char defaultCharacter = ' ')
        {
            MemoryAddress.ThrowIfDefault(text);

            if (newLength == text->length)
            {
                return;
            }

            int oldLength = text->length;
            text->length = newLength;
            if (newLength > oldLength)
            {
                int copyLength = newLength - oldLength;
                MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
                text->buffer.AsSpan<char>(oldLength * sizeof(char), copyLength).Fill(defaultCharacter);
            }
        }

        /// <summary>
        /// Retrieves a slice of the remaining text starting at <paramref name="start"/>.
        /// </summary>
        public readonly Span<char> Slice(int start)
        {
            MemoryAddress.ThrowIfDefault(text);

            return new Span<char>(text->buffer.Pointer + start * sizeof(char), text->length - start);
        }

        /// <summary>
        /// Retrieves a slice with <paramref name="length"/> starting at <paramref name="start"/>.
        /// </summary>
        public readonly Span<char> Slice(int start, int length)
        {
            MemoryAddress.ThrowIfDefault(text);

            return new Span<char>(text->buffer.Pointer + start * sizeof(char), length);
        }

        /// <summary>
        /// Appends a single <paramref name="character"/>.
        /// </summary>
        public readonly void Append(char character)
        {
            MemoryAddress.ThrowIfDefault(text);

            int newLength = text->length + 1;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            text->buffer.WriteElement(text->length, character);
            text->length = newLength;
        }

        /// <summary>
        /// Appends a single <paramref name="character"/>.
        /// </summary>
        public readonly void Append(char character, int repeat)
        {
            MemoryAddress.ThrowIfDefault(text);

            if (repeat == 0) return;
            int newLength = text->length + repeat;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            Slice(text->length, repeat).Fill(character);
            text->length = newLength;
        }

        /// <summary>
        /// Appends the given <paramref name="otherText"/>.
        /// </summary>
        public readonly void Append(string otherText)
        {
            MemoryAddress.ThrowIfDefault(text);

            int newLength = text->length + otherText.Length;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            otherText.AsSpan().CopyTo(Slice(text->length, otherText.Length));
            text->length = newLength;
        }

        /// <summary>
        /// Appends the given <paramref name="otherText"/>.
        /// </summary>
        public readonly void Append(ReadOnlySpan<char> otherText)
        {
            MemoryAddress.ThrowIfDefault(text);

            int newLength = text->length + otherText.Length;
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

            int newLength = text->length + otherText.Count;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            Span<char> buffer = text->buffer.GetSpan<char>(newLength);
            int index = text->length;
            foreach (char character in otherText)
            {
                buffer[index++] = character;
            }

            text->length = newLength;
        }

#if NET
        /// <summary>
        /// Appends a single <paramref name="value"/>.
        /// </summary>
        public readonly void Append<T>(T value) where T : ISpanFormattable
        {
            MemoryAddress.ThrowIfDefault(text);

            Span<char> buffer = stackalloc char[1024];
            value.TryFormat(buffer, out int charsWritten, default, default);
            int newLength = text->length + charsWritten;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            text->buffer.CopyFrom(buffer.Slice(0, charsWritten), text->length * sizeof(char));
            text->length = newLength;
        }
#else
        /// <summary>
        /// Appends a single <paramref name="value"/>.
        /// </summary>
        public readonly void Append<T>(T value) where T : IFormattable
        {
            MemoryAddress.ThrowIfDefault(text);

            string buffer = value.ToString(default, default);
            int newLength = text->length + buffer.Length;
            MemoryAddress.Resize(ref text->buffer, newLength * sizeof(char));
            text->buffer.CopyFrom(buffer.AsSpan(), text->length * sizeof(char));
            text->length = newLength;
        }
#endif

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
        public readonly void AppendLine(ReadOnlySpan<char> text)
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
        public readonly void RemoveAt(int index)
        {
            ThrowIfOutOfRange(index);

            int newLength = text->length - 1;
            if (index < newLength)
            {
                Slice(index + 1).CopyTo(Slice(index));
            }

            text->length = newLength;
        }

        /// <summary>
        /// Removes a range of text starting at <paramref name="start"/>.
        /// </summary>
        public readonly void Remove(int start, int length)
        {
            ThrowIfOutOfRange(start);
            ThrowIfGreaterThanRange(start + length);

            Span<char> buffer = stackalloc char[text->length];
            CopyTo(buffer);
            buffer.Slice(start + length).CopyTo(buffer.Slice(start));
            CopyFrom(buffer.Slice(0, text->length - length));
        }

        /// <summary>
        /// Retrieves the index for the first occurance of the given <paramref name="character"/>.
        /// </summary>
        public readonly int IndexOf(char character)
        {
            return AsSpan().IndexOf(character);
        }

        /// <summary>
        /// Retrieves the index for the last occurance of the given <paramref name="character"/>.
        /// </summary>
        public readonly int LastIndexOf(char character)
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
        public readonly void Insert(int index, char character)
        {
            MemoryAddress.ThrowIfDefault(text);
            ThrowIfGreaterThanRange(index);

            Span<char> buffer = stackalloc char[text->length + 1];
            CopyTo(buffer);
            int shiftAmount = text->length - index;
            buffer.Slice(index, shiftAmount).CopyTo(buffer.Slice(index + 1, shiftAmount));
            buffer[index] = character;
            MemoryAddress.Resize(ref text->buffer, buffer.Length * sizeof(char));
            text->length = buffer.Length;
            text->buffer.CopyFrom(buffer);
        }

        /// <summary>
        /// Inserts the <paramref name="otherText"/> at the given <paramref name="index"/>.
        /// </summary>
        public readonly void Insert(int index, ReadOnlySpan<char> otherText)
        {
            MemoryAddress.ThrowIfDefault(text);
            ThrowIfGreaterThanRange(index);

            Span<char> buffer = stackalloc char[text->length + otherText.Length];
            CopyTo(buffer);
            int shiftAmount = text->length - index;
            buffer.Slice(index, shiftAmount).CopyTo(buffer.Slice(index + otherText.Length, shiftAmount));
            otherText.CopyTo(buffer.Slice(index));
            MemoryAddress.Resize(ref text->buffer, buffer.Length * sizeof(char));
            text->length = buffer.Length;
            text->buffer.CopyFrom(buffer);
        }

        /// <summary>
        /// Inserts the given <paramref name="otherText"/> at the <paramref name="index"/>.
        /// </summary>
        public readonly void Insert(int index, string otherText)
        {
            MemoryAddress.ThrowIfDefault(text);
            ThrowIfGreaterThanRange(index);

            Span<char> buffer = stackalloc char[text->length + otherText.Length];
            CopyTo(buffer);
            int shiftAmount = text->length - index;
            buffer.Slice(index, shiftAmount).CopyTo(buffer.Slice(index + otherText.Length, shiftAmount));
            otherText.AsSpan().CopyTo(buffer.Slice(index));
            MemoryAddress.Resize(ref text->buffer, buffer.Length * sizeof(char));
            text->length = buffer.Length;
            text->buffer.CopyFrom(buffer);
        }

#if NET
        /// <summary>
        /// Inserts the given <paramref name="otherValue"/> at the <paramref name="index"/>.
        /// </summary>
        public readonly void Insert<T>(int index, T otherValue) where T : ISpanFormattable
        {
            MemoryAddress.ThrowIfDefault(text);
            ThrowIfGreaterThanRange(index);

            Span<char> valueBuffer = stackalloc char[1024];
            otherValue.TryFormat(valueBuffer, out int otherTextLength, default, default);

            Span<char> buffer = stackalloc char[text->length + otherTextLength];
            CopyTo(buffer);
            int shiftAmount = text->length - index;
            buffer.Slice(index, shiftAmount).CopyTo(buffer.Slice(index + otherTextLength, shiftAmount));
            valueBuffer.Slice(0, otherTextLength).CopyTo(buffer.Slice(index));
            MemoryAddress.Resize(ref text->buffer, buffer.Length * sizeof(char));
            text->length = buffer.Length;
            text->buffer.CopyFrom(buffer);
        }
#else
        /// <summary>
        /// Inserts the given <paramref name="otherValue"/> at the <paramref name="index"/>.
        /// </summary>
        public readonly void Insert<T>(int index, T otherValue) where T : IFormattable
        {
            MemoryAddress.ThrowIfDefault(text);
            ThrowIfGreaterThanRange(index);

            string valueBuffer = otherValue.ToString(default, default);
            int otherTextLength = valueBuffer.Length;
            Span<char> buffer = stackalloc char[text->length + otherTextLength];
            CopyTo(buffer);
            int shiftAmount = text->length - index;
            buffer.Slice(index, shiftAmount).CopyTo(buffer.Slice(index + otherTextLength, shiftAmount));
            valueBuffer.AsSpan().CopyTo(buffer.Slice(index));
            MemoryAddress.Resize(ref text->buffer, buffer.Length * sizeof(char));
            text->length = buffer.Length;
            text->buffer.CopyFrom(buffer);
        }
#endif

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
        public readonly bool EndsWith(ReadOnlySpan<char> otherText)
        {
            int length = Length;
            int textLength = otherText.Length;
            if (length < textLength)
            {
                return false;
            }

            Span<char> buffer = AsSpan();
            return buffer.Slice(length - textLength).SequenceEqual(otherText);
        }

        /// <summary>
        /// Checks if this text starts with <paramref name="otherText"/>.
        /// </summary>
        public readonly bool StartsWith(ReadOnlySpan<char> otherText)
        {
            int length = Length;
            int textLength = otherText.Length;
            if (length < textLength)
            {
                return false;
            }

            Span<char> buffer = AsSpan();
            return buffer.Slice(0, textLength).SequenceEqual(otherText);
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
        public readonly bool Equals(ReadOnlySpan<char> otherText)
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
                int length = Length;
                hash = hash * 23 + length.GetHashCode();
                for (int i = 0; i < length; i++)
                {
                    char c = text->buffer.ReadElement<char>(i);
                    hash = hash * 23 + c.GetHashCode();
                }

                return hash;
            }
        }

        /// <summary>
        /// Retrieves a double precision hash code.
        /// </summary>
        public readonly long GetLongHashCode()
        {
            unchecked
            {
                long hash = 3074457345618258791;
                Span<char> text = new(this.text->buffer.Pointer, this.text->length);
                for (int i = 0; i < text.Length; i++)
                {
                    hash += text[i];
                    hash *= 3074457345618258799;
                }

                return hash;
            }
        }

        /// <summary>
        /// Appends the given <paramref name="character"/> to <paramref name="originalText"/>
        /// and copies the result to the <paramref name="destination"/>.
        /// </summary>
        public static void Append(ReadOnlySpan<char> originalText, char character, Span<char> destination)
        {
            originalText.CopyTo(destination.Slice(0, originalText.Length));
            destination[originalText.Length] = character;
        }

        /// <summary>
        /// Appends the given <paramref name="newText"/> to <paramref name="originalText"/>
        /// and copies the result to the <paramref name="destination"/>.
        /// </summary>
        public static void Append(ReadOnlySpan<char> originalText, string newText, Span<char> destination)
        {
            Append(originalText, newText.AsSpan(), destination);
        }

        /// <summary>
        /// Appends the given <paramref name="newText"/> to <paramref name="originalText"/>
        /// and copies the result to the <paramref name="destination"/>.
        /// </summary>
        public static void Append(ReadOnlySpan<char> originalText, ReadOnlySpan<char> newText, Span<char> destination)
        {
            int originalLength = originalText.Length;
            int newLength = newText.Length;
            int destinationLength = destination.Length;
            int copyLength = Math.Min(originalLength + newLength, destinationLength);
            originalText.CopyTo(destination.Slice(0, originalLength));
            newText.Slice(0, copyLength - originalLength).CopyTo(destination.Slice(originalLength));
        }

        /// <summary>
        /// Replaces all occurrences of <paramref name="target"/> with <paramref name="replacement"/>,
        /// and copies the result to the <paramref name="destination"/>.
        /// </summary>
        /// <returns>The length copied into the <paramref name="destination"/>.</returns>
        public static int Replace(ReadOnlySpan<char> originalText, string target, string replacement, Span<char> destination)
        {
            return Replace(originalText, target.AsSpan(), replacement.AsSpan(), destination);
        }

        /// <summary>
        /// Replaces all occurrences of <paramref name="target"/> with <paramref name="replacement"/>,
        /// and copies the result to the <paramref name="destination"/>.
        /// </summary>
        public static int Replace(ReadOnlySpan<char> originalText, ReadOnlySpan<char> target, ReadOnlySpan<char> replacement, Span<char> destination)
        {
            int length = 0;
            int originalStart = 0;
            int destinationStart = 0;
            while (true)
            {
                if (originalText.Slice(originalStart).TryIndexOf(target, out int index))
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

        readonly void ICollection<char>.Add(char item)
        {
            Append(item);
        }

        readonly void ICollection<char>.CopyTo(char[] array, int arrayIndex)
        {
            AsSpan().CopyTo(array.AsSpan(arrayIndex));
        }

        readonly bool ICollection<char>.Remove(char item)
        {
            int index = IndexOf(item);
            if (index != -1)
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
            public readonly char Current => text[index];

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
            public readonly int Length => text.Length;

            /// <summary>
            /// Checks if the text is empty.
            /// </summary>
            public readonly bool IsEmpty => text.IsEmpty;

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

            /// <inheritdoc/>
            public readonly override bool Equals(object? obj)
            {
                return obj is Borrowed && Equals((Borrowed)obj);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return text.GetHashCode();
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
            public readonly bool Equals(Span<char> other)
            {
                return text.Equals(other);
            }

            /// <summary>
            /// Checks if this text equals to the <paramref name="other"/> text.
            /// </summary>
            public readonly bool Equals(ReadOnlySpan<char> other)
            {
                return text.Equals(other);
            }

            /// <summary>
            /// Retrieves the text as a span of <see cref="char"/> values.
            /// </summary>
            public readonly Span<char> AsSpan()
            {
                return text.AsSpan();
            }

            /// <summary>
            /// Makes this text match <paramref name="otherText"/> exactly.
            /// </summary>
            public readonly void CopyFrom(ReadOnlySpan<char> otherText)
            {
                text.CopyFrom(otherText);
            }

            /// <summary>
            /// Makes this text match <paramref name="otherText"/> exactly.
            /// </summary>
            public readonly void CopyFrom(ASCIIText256 otherText)
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