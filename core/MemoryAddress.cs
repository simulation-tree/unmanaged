#if DEBUG
#define TRACK
#endif

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// An address referring to either stack or heap memory.
    /// </summary>
    public unsafe struct MemoryAddress : IDisposable, IEquatable<MemoryAddress>
    {
        private byte* pointer;

        /// <summary>
        /// Native address of this memory.
        /// </summary>
        public readonly nint Address => (nint)pointer;

        /// <summary>
        /// Native pointer of this memory.
        /// </summary>
        public readonly byte* Pointer => pointer;

        /// <summary>
        /// Gets or sets a byte at the given index.
        /// </summary>
        public readonly ref byte this[int index]
        {
            get
            {
                ThrowIfDefault(pointer);
                MemoryTracker.ThrowIfOutOfBounds(pointer, index);

                return ref pointer[index];
            }
        }

        /// <summary>
        /// Initializes an existing allocation from the given <paramref name="pointer"/>.
        /// </summary>
        public MemoryAddress(void* pointer)
        {
            this.pointer = (byte*)pointer;
        }

        /// <summary>
        /// Initializes an existing allocation from the given native <paramref name="address"/>.
        /// </summary>
        public MemoryAddress(nint address)
        {
            this.pointer = (byte*)address;
        }

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public MemoryAddress()
        {
        }
#endif

        /// <summary>
        /// Frees the allocation.
        /// </summary>
        public void Dispose()
        {
            Free(ref pointer);
        }

        /// <summary>
        /// String representation of this allocation value.
        /// </summary>
        public readonly override string ToString()
        {
            ThrowIfDefault(pointer);

            return ((nint)pointer).ToString();
        }

        /// <summary>
        /// String representation of this allocation value.
        /// </summary>
        public readonly int ToString(Span<char> buffer)
        {
            ThrowIfDefault(pointer);

            return ((nint)pointer).ToString(buffer);
        }

        /// <summary>
        /// Retrieves a span of bytes with <paramref name="byteLength"/>.
        /// </summary>
        public readonly Span<byte> GetSpan(int byteLength)
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, byteLength);

            return new(pointer, byteLength);
        }


        /// <summary>
        /// Retrieves a span of <typeparamref name="T"/> elements with <paramref name="length"/>.
        /// </summary>
        public readonly Span<T> GetSpan<T>(int length) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, length * sizeof(T));

            return new(pointer, length);
        }

        /// <summary>
        /// Retrieves a slice of bytes starting at <paramref name="bytePosition"/> with
        /// <paramref name="byteLength"/>.
        /// </summary>
        public readonly Span<byte> AsSpan(int bytePosition, int byteLength)
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, bytePosition + byteLength);

            return new Span<byte>(pointer + bytePosition, byteLength);
        }

        /// <summary>
        /// Gets a span of elements from the memory.
        /// <para>Both <paramref name="start"/> and <paramref name="length"/> are expected
        /// to be in <typeparamref name="T"/> elements.</para>
        /// </summary>
        public readonly Span<T> AsSpan<T>(int start, int length) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, (start + length) * sizeof(T));

            return new Span<T>(pointer + start * sizeof(T), length);
        }

        /// <summary>
        /// Writes a single given value into the memory into this byte position.
        /// </summary>
        public readonly void Write<T>(int bytePosition, T value) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, bytePosition + sizeof(T));

            unchecked
            {
                *(T*)(pointer + (uint)bytePosition) = value;
            }
        }

        /// <summary>
        /// Writes a single given value into the memory into this byte position.
        /// </summary>
        public readonly void Write<T>(uint bytePosition, T value) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, (int)bytePosition + sizeof(T));

            *(T*)(pointer + bytePosition) = value;
        }

        /// <summary>
        /// Writes a single given value into the memory to the beginning.
        /// </summary>
        public readonly void Write<T>(T value) where T : unmanaged
        {
            ThrowIfDefault(pointer);

            *(T*)pointer = value;
        }

        /// <summary>
        /// Writes the given <paramref name="value"/> to the element <paramref name="index"/>.
        /// </summary>
        public readonly void WriteElement<T>(int index, T value) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, (index + 1) * sizeof(T));

            unchecked
            {
                ((T*)pointer)[(uint)index] = value;
            }
        }

        /// <summary>
        /// Writes the given <paramref name="value"/> to the element <paramref name="index"/>.
        /// </summary>
        public readonly void WriteElement<T>(uint index, T value) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, (int)(index + 1) * sizeof(T));

            ((T*)pointer)[index] = value;
        }

        /// <summary>
        /// Writes the given <paramref name="span"/> into memory starting at <paramref name="bytePosition"/>.
        /// </summary>
        public readonly void Write<T>(int bytePosition, Span<T> span) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, bytePosition + sizeof(T) * span.Length);

            span.CopyTo(new(pointer + bytePosition, span.Length));
        }

        /// <summary>
        /// Writes the given <paramref name="span"/> into memory starting at <paramref name="bytePosition"/>.
        /// </summary>
        public readonly void Write<T>(int bytePosition, ReadOnlySpan<T> span) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, bytePosition + sizeof(T) * span.Length);

            span.CopyTo(new(pointer + bytePosition, span.Length));
        }

        /// <summary>
        /// Writes the given <paramref name="span"/> into memory from the beginning.
        /// </summary>
        public readonly void Write<T>(Span<T> span) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, sizeof(T) * span.Length);

            span.CopyTo(new(pointer, span.Length));
        }

        /// <summary>
        /// Writes the given <paramref name="span"/> into memory from the beginning.
        /// </summary>
        public readonly void Write<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, sizeof(T) * span.Length);

            span.CopyTo(new(pointer, span.Length));
        }

        /// <summary>
        /// Writes <paramref name="otherData"/> with a custom <paramref name="byteLength"/> into memory starting 
        /// at <paramref name="bytePosition"/>.
        /// </summary>
        public readonly void Write(int bytePosition, int byteLength, MemoryAddress otherData)
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, bytePosition + byteLength);

            new Span<byte>(otherData, byteLength).CopyTo(new(pointer + bytePosition, byteLength));
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the memory starting from the given byte position.
        /// </summary>
        public readonly ref T Read<T>(int bytePosition) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, bytePosition + sizeof(T));

            unchecked
            {
                return ref *(T*)(pointer + (uint)bytePosition);
            }
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the memory starting from the given byte position.
        /// </summary>
        public readonly ref T Read<T>(uint bytePosition) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, (int)bytePosition + sizeof(T));

            return ref *(T*)(pointer + bytePosition);
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> at the given element <paramref name="index"/>.
        /// </summary>
        public readonly ref T ReadElement<T>(int index) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, (index + 1) * sizeof(T));

            unchecked
            {
                return ref ((T*)pointer)[(uint)index];
            }
        }


        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> at the given element <paramref name="index"/>.
        /// </summary>
        public readonly ref T ReadElement<T>(uint index) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, (int)(index + 1) * sizeof(T));

            return ref ((T*)pointer)[index];
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the memory starting from the given byte position.
        /// </summary>
        public readonly ref T Read<T>() where T : unmanaged
        {
            ThrowIfDefault(pointer);

            return ref *(T*)pointer;
        }

        /// <summary>
        /// Reads data from the memory starting from the given <paramref name="bytePosition"/>.
        /// </summary>
        public readonly MemoryAddress Read(int bytePosition)
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfOutOfBounds(pointer, bytePosition);

            unchecked
            {
                return new(pointer + (uint)bytePosition);
            }
        }

        /// <summary>
        /// Reads data from the memory starting from the given <paramref name="bytePosition"/>.
        /// </summary>
        public readonly MemoryAddress Read(uint bytePosition)
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfOutOfBounds(pointer, (int)bytePosition);

            return new(pointer + bytePosition);
        }

        /// <summary>
        /// Resets the memory to <see langword="default"/> state.
        /// </summary>
        public readonly void Clear(int byteLength)
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, byteLength);

            new Span<byte>(pointer, byteLength).Clear();
        }

        /// <summary>
        /// Resets a range of memory to <see langword="default"/> state.
        /// </summary>
        public readonly void Clear(int bytePosition, int byteLength)
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, bytePosition + byteLength);

            new Span<byte>(pointer + bytePosition, byteLength).Clear();
        }

        /// <summary>
        /// Resets a range of memory to <see langword="default"/> state.
        /// </summary>
        public readonly void Clear<T>(int elementStart, int elementLength) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, (elementStart + elementLength) * sizeof(T));

            new Span<T>(pointer + elementStart, elementLength).Clear();
        }

        /// <summary>
        /// Fills the memory with the given byte value.
        /// </summary>
        public readonly void Fill(int byteLength, byte value)
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, byteLength);

            new Span<byte>(pointer, byteLength).Fill(value);
        }

        /// <summary>
        /// Fills the memory with the given byte value.
        /// </summary>
        public readonly void Fill(int bytePosition, int byteLength, byte value)
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, bytePosition + byteLength);

            new Span<byte>(pointer + bytePosition, byteLength).Fill(value);
        }

        /// <summary>
        /// Copies bytes of this allocation into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(MemoryAddress destination, int sourceBytePosition, int destinationBytePosition, int byteLength)
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, sourceBytePosition + byteLength);

            new Span<byte>(pointer + sourceBytePosition, byteLength).CopyTo(new(destination.pointer + destinationBytePosition, byteLength));
        }

        /// <summary>
        /// Copies bytes of this allocation into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(MemoryAddress destination, int byteLength)
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, byteLength);
            MemoryTracker.ThrowIfGreaterThanLength(destination.pointer, byteLength);

            new Span<byte>(pointer, byteLength).CopyTo(new(destination.pointer, byteLength));
        }

        /// <summary>
        /// Copies bytes of this allocation into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(void* destination, int byteLength)
        {
            ThrowIfDefault(pointer);

            new Span<byte>(pointer, byteLength).CopyTo(new(destination, byteLength));
        }

        /// <summary>
        /// Copies elements from this memory to fit into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo<T>(Span<T> destination) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, sizeof(T) * destination.Length);

            new Span<T>(pointer, destination.Length).CopyTo(destination);
        }

        /// <summary>
        /// Copies the bytes from <paramref name="source"/> and writes them into this allocation.
        /// </summary>
        public readonly void CopyFrom(MemoryAddress source, int byteLength)
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, byteLength);
            MemoryTracker.ThrowIfGreaterThanLength(source.pointer, byteLength);

            new Span<byte>(source.pointer, byteLength).CopyTo(new(pointer, byteLength));
        }

        /// <summary>
        /// Copies the bytes from <paramref name="source"/> and writes them into this allocation.
        /// </summary>
        public readonly void CopyFrom<T>(Span<T> source) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, sizeof(T) * source.Length);

            source.CopyTo(new(pointer, source.Length));
        }

        /// <summary>
        /// Copies the bytes from <paramref name="source"/> and writes them into this allocation.
        /// </summary>
        public readonly void CopyFrom<T>(ReadOnlySpan<T> source) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, sizeof(T) * source.Length);

            source.CopyTo(new(pointer, source.Length));
        }

        /// <summary>
        /// Copies the bytes from <paramref name="source"/> and writes them into this allocation
        /// starting at <paramref name="byteStart"/>.
        /// </summary>
        public readonly void CopyFrom<T>(Span<T> source, int byteStart) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, byteStart + sizeof(T) * source.Length);

            source.CopyTo(new(pointer + byteStart, source.Length));
        }

        /// <summary>
        /// Copies the bytes from <paramref name="source"/> and writes them into this allocation
        /// starting at <paramref name="byteStart"/>.
        /// </summary>
        public readonly void CopyFrom<T>(ReadOnlySpan<T> source, int byteStart) where T : unmanaged
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, byteStart + sizeof(T) * source.Length);

            source.CopyTo(new(pointer + byteStart, source.Length));
        }

        /// <summary>
        /// Copies the bytes from <paramref name="source"/> and writes them into this allocation.
        /// </summary>
        public readonly void CopyFrom(void* source, int byteLength)
        {
            ThrowIfDefault(pointer);
            MemoryTracker.ThrowIfGreaterThanLength(pointer, byteLength);

            new Span<byte>(source, byteLength).CopyTo(new(pointer, byteLength));
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is MemoryAddress allocation && Equals(allocation);
        }

        /// <inheritdoc/>
        public readonly bool Equals(MemoryAddress other)
        {
            return pointer == other.pointer;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            unchecked
            {
                return (int)pointer;
            }
        }

        /// <summary>
        /// Moves existing memory into a new allocation of the given <paramref name="newByteLength"/>.
        /// <para>
        /// New bytes are uninitialized and not guaranteed to be <see langword="default"/>/
        /// </para>
        /// </summary>
        public static void Resize(ref MemoryAddress allocation, int newByteLength)
        {
            ThrowIfDefault(allocation.pointer);

#if TRACE
            void* previousPointer = allocation.pointer;
            allocation.pointer = (byte*)NativeMemory.Realloc(previousPointer, (uint)newByteLength);
            MemoryTracker.Move(previousPointer, allocation.pointer, newByteLength);
#else
            allocation.pointer = (byte*)NativeMemory.Realloc(allocation.pointer, (uint)newByteLength);
#endif
        }

        /// <summary>
        /// Moves existing memory into a new allocation of the given <paramref name="newByteLength"/>,
        /// and clears new bytes to <see langword="default"/>.
        /// </summary>
        public static void ResizeAndClear(ref MemoryAddress allocation, int currentByteLength, int newByteLength)
        {
            ThrowIfDefault(allocation.pointer);

#if TRACE
            void* previousPointer = allocation.pointer;
            allocation.pointer = (byte*)NativeMemory.Realloc(previousPointer, (uint)newByteLength);
            NativeMemory.Clear(allocation.pointer + currentByteLength, (uint)(newByteLength - currentByteLength));
            MemoryTracker.Move(previousPointer, allocation.pointer, newByteLength);
#else
            allocation.pointer = (byte*)NativeMemory.Realloc(allocation.pointer, (uint)newByteLength);
            NativeMemory.Clear(allocation.pointer + currentByteLength, (uint)(newByteLength - currentByteLength));
#endif
        }

        /// <summary>
        /// Moves existing memory into a new allocation that is twice as large as <paramref name="currentByteLength"/>,
        /// and clears new bytes to <see langword="default"/>.
        /// </summary>
        public static void ResizePowerOf2AndClear(ref MemoryAddress allocation, int currentByteLength)
        {
            ThrowIfDefault(allocation.pointer);

#if TRACE
            void* previousPointer = allocation.pointer;
            allocation.pointer = (byte*)NativeMemory.Realloc(previousPointer, (uint)currentByteLength * 2);
            NativeMemory.Clear(allocation.pointer + currentByteLength, (uint)currentByteLength);
            MemoryTracker.Move(previousPointer, allocation.pointer, currentByteLength * 2);
#else
            allocation.pointer = (byte*)NativeMemory.Realloc(allocation.pointer, (uint)currentByteLength * 2);
            NativeMemory.Clear(allocation.pointer + currentByteLength, (uint)currentByteLength);
#endif
        }

        /// <summary>
        /// Moves existing memory into a new allocation that is able to
        /// fit <typeparamref name="T"/>.
        /// <para>
        /// New bytes are uninitialized and not guaranteed to be <see langword="default"/>/
        /// </para>
        /// </summary>
        public static void Resize<T>(ref MemoryAddress allocation) where T : unmanaged
        {
            ThrowIfDefault(allocation.pointer);

#if TRACE
            void* previousPointer = allocation.pointer;
            allocation.pointer = (byte*)NativeMemory.Realloc(previousPointer, (uint)sizeof(T));
            MemoryTracker.Move(previousPointer, allocation.pointer, sizeof(T));
#else
            allocation.pointer = (byte*)NativeMemory.Realloc(allocation.pointer, (uint)sizeof(T));
#endif
        }

        /// <summary>
        /// Creates an empty allocation of size 0.
        /// </summary>
        public static MemoryAddress AllocateEmpty()
        {
#if TRACE
            void* pointer = NativeMemory.Alloc(0);
            MemoryTracker.Track(pointer, 0);
            return new(pointer);
#else
            return new(NativeMemory.Alloc(0));
#endif
        }

        /// <summary>
        /// Creates an allocation of size <paramref name="byteLength"/>, initialized
        /// to <see langword="default"/> memory.
        /// </summary>
        public static MemoryAddress AllocateZeroed(int byteLength)
        {
#if TRACK
            void* pointer = NativeMemory.AllocZeroed((uint)byteLength);
            MemoryTracker.Track(pointer, byteLength);
            return new(pointer);
#else
            return new(NativeMemory.AllocZeroed((uint)byteLength));
#endif
        }

        /// <summary>
        /// Creates an allocation of size <paramref name="byteLength"/>, initialized
        /// to <see langword="default"/> memory.
        /// </summary>
        public static MemoryAddress AllocateZeroed(uint byteLength)
        {
#if TRACK
            void* pointer = NativeMemory.AllocZeroed(byteLength);
            MemoryTracker.Track(pointer, (int)byteLength);
            return new(pointer);
#else
            return new(NativeMemory.AllocZeroed(byteLength));
#endif
        }

        /// <summary>
        /// Creates a new non-zeroed allocation of size <paramref name="byteLength"/>.
        /// </summary>
        public static MemoryAddress Allocate(int byteLength)
        {
#if TRACK
            void* pointer = NativeMemory.Alloc((uint)byteLength);
            MemoryTracker.Track(pointer, byteLength);
            return new(pointer);
#else
            return new(NativeMemory.Alloc((uint)byteLength));
#endif
        }

        /// <summary>
        /// Creates a new non-zeroed allocation of size <paramref name="byteLength"/>.
        /// </summary>
        public static MemoryAddress Allocate(uint byteLength)
        {
#if TRACK
            void* pointer = NativeMemory.Alloc(byteLength);
            MemoryTracker.Track(pointer, (int)byteLength);
            return new(pointer);
#else
            return new(NativeMemory.Alloc(byteLength));
#endif
        }

        /// <summary>
        /// Creates a new allocation that contains the data of the given <paramref name="value"/>.
        /// </summary>
        public static MemoryAddress AllocateValue<T>(T value) where T : unmanaged
        {
#if TRACK
            void* pointer = NativeMemory.Alloc((uint)sizeof(T));
            MemoryTracker.Track(pointer, sizeof(T));
            *(T*)pointer = value;
            return new(pointer);
#else
            void* pointer = NativeMemory.Alloc((uint)sizeof(T));
            *(T*)pointer = value;
            return new(pointer);
#endif
        }

        /// <summary>
        /// Creates a new allocation that contains the data of the given <paramref name="value"/>.
        /// </summary>
        public static MemoryAddress AllocateValue<T>(T value, out int size) where T : unmanaged
        {
            size = sizeof(T);
#if TRACK
            void* pointer = NativeMemory.Alloc((uint)size);
            MemoryTracker.Track(pointer, size);
            *(T*)pointer = value;
            return new(pointer);
#else
            void* pointer = NativeMemory.Alloc((uint)size);
            *(T*)pointer = value;
            return new(pointer);
#endif
        }

        /// <summary>
        /// Creates a new uninitialized allocation that can contain a(n) <typeparamref name="T"/>
        /// </summary>
        public static ref T Allocate<T>() where T : unmanaged
        {
#if TRACK
            void* pointer = NativeMemory.Alloc((uint)sizeof(T));
            MemoryTracker.Track(pointer, sizeof(T));
            return ref *(T*)pointer;
#else
            return ref *(T*)NativeMemory.Alloc((uint)sizeof(T));
#endif
        }

        /// <summary>
        /// Creates a new uninitialized allocation that can contain a(n) <typeparamref name="T"/>
        /// </summary>
        public static T* AllocatePointer<T>() where T : unmanaged
        {
#if TRACK
            void* pointer = NativeMemory.Alloc((uint)sizeof(T));
            MemoryTracker.Track(pointer, sizeof(T));
            return (T*)pointer;
#else
            return (T*)NativeMemory.Alloc((uint)sizeof(T));
#endif
        }

        /// <summary>
        /// Creates a new allocation containg the given <paramref name="source"/>.
        /// </summary>
        public static MemoryAddress Allocate<T>(Span<T> source) where T : unmanaged
        {
#if TRACK
            int byteLength = sizeof(T) * source.Length;
            void* pointer = NativeMemory.Alloc((uint)byteLength);
            MemoryTracker.Track(pointer, byteLength);
            source.CopyTo(new(pointer, source.Length));
            return new(pointer);
#else
            void* pointer = NativeMemory.Alloc((uint)(sizeof(T) * source.Length));
            source.CopyTo(new(pointer, source.Length));
            return new(pointer);
#endif
        }

        /// <summary>
        /// Creates a new allocation containg the given <paramref name="source"/>.
        /// </summary>
        public static MemoryAddress Allocate<T>(ReadOnlySpan<T> source) where T : unmanaged
        {
#if TRACK
            int byteLength = sizeof(T) * source.Length;
            void* pointer = NativeMemory.Alloc((uint)byteLength);
            MemoryTracker.Track(pointer, byteLength);
            source.CopyTo(new(pointer, source.Length));
            return new(pointer);
#else
            void* pointer = NativeMemory.Alloc((uint)(sizeof(T) * source.Length));
            source.CopyTo(new(pointer, source.Length));
            return new(pointer);
#endif
        }

        /// <summary>
        /// Retrieves an allocation containing <paramref name="value"/> on the stack.
        /// <para>
        /// Doesn't allocate memory on the heap.
        /// </para>
        /// </summary>
        public static MemoryAddress Get<T>(ref T value) where T : unmanaged
        {
            fixed (T* pointer = &value)
            {
                return new(pointer);
            }
        }

        /// <summary>
        /// Frees a previously created <paramref name="allocation"/> and 
        /// assigns it to <see langword="default"/>.
        /// </summary>
        public static void Free<T>(ref T* allocation) where T : unmanaged
        {
            ThrowIfDefault(allocation);
            MemoryTracker.ThrowIfDisposed(allocation);

            NativeMemory.Free(allocation);
            MemoryTracker.Untrack(allocation);
            allocation = default;
        }

        /// <summary>
        /// Frees a previously created <paramref name="allocation"/> and 
        /// assigns it to <see langword="default"/>.
        /// </summary>
        public static void Free(ref void* allocation)
        {
            ThrowIfDefault(allocation);
            MemoryTracker.ThrowIfDisposed(allocation);

            NativeMemory.Free(allocation);
            MemoryTracker.Untrack(allocation);
            allocation = default;
        }

        /// <summary>
        /// In debug mode, throws an exception if the given <paramref name="address"/> is <see langword="default"/>.
        /// </summary>
        [Conditional("DEBUG")]
        public static void ThrowIfDefault(MemoryAddress address)
        {
            if (address.pointer is null)
            {
                throw new InvalidOperationException("Memory address is default");
            }
        }

        /// <summary>
        /// In debug mode, throws an exception if the given <paramref name="pointer"/> is <see langword="default"/>.
        /// </summary>
        [Conditional("DEBUG")]
        public static void ThrowIfDefault(void* pointer)
        {
            if (pointer is null)
            {
                throw new InvalidOperationException("Memory address is default");
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(MemoryAddress left, MemoryAddress right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(MemoryAddress left, MemoryAddress right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static implicit operator void*(MemoryAddress allocation)
        {
            return allocation.pointer;
        }
    }
}