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
        public readonly ref byte this[uint index] => ref pointer[index];

        /// <summary>
        /// Initializes an existing allocation from the given <paramref name="pointer"/>.
        /// </summary>
        public MemoryAddress(void* pointer)
        {
            this.pointer = (byte*)pointer;
        }

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public MemoryAddress()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Frees the allocation.
        /// </summary>
        public void Dispose()
        {
            ThrowIfDefault();

            NativeMemory.Free(pointer);
            pointer = default;
        }

        /// <summary>
        /// In debug mode, throws an exception if this is <see langword="default"/>.
        /// </summary>
        [Conditional("DEBUG")]
        public readonly void ThrowIfDefault()
        {
            if (pointer is null)
            {
                throw new InvalidOperationException("Memory address is default");
            }
        }

        /// <summary>
        /// String representation of this allocation value.
        /// </summary>
        public readonly override string ToString()
        {
            return Address.ToString();
        }

        /// <summary>
        /// String representation of this allocation value.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            return Address.ToString(buffer);
        }

        /// <summary>
        /// Writes a single given value into the memory into this byte position.
        /// </summary>
        public readonly void Write<T>(uint bytePosition, T value) where T : unmanaged
        {
            *(T*)(pointer + bytePosition) = value;
        }

        /// <summary>
        /// Writes a single given value into the memory to the beginning.
        /// </summary>
        public readonly void Write<T>(T value) where T : unmanaged
        {
            *(T*)pointer = value;
        }

        /// <summary>
        /// Writes the given <paramref name="value"/> to the element <paramref name="index"/>.
        /// </summary>
        public readonly void WriteElement<T>(uint index, T value) where T : unmanaged
        {
            unchecked
            {
                ((T*)pointer)[index] = value;
            }
        }

        /// <summary>
        /// Writes the given <paramref name="span"/> into memory starting at <paramref name="bytePosition"/>.
        /// </summary>
        public readonly void Write<T>(uint bytePosition, USpan<T> span) where T : unmanaged
        {
            unchecked
            {
                Span<T> thisSpan = new(pointer + bytePosition, (int)span.Length);
                span.CopyTo(thisSpan);
            }
        }

        /// <summary>
        /// Writes the given <paramref name="span"/> into memory from the beginning.
        /// </summary>
        public readonly void Write<T>(USpan<T> span) where T : unmanaged
        {
            unchecked
            {
                Span<T> thisSpan = new(pointer, (int)span.Length);
                span.CopyTo(thisSpan);
            }
        }

        /// <summary>
        /// Writes <paramref name="otherData"/> with a custom <paramref name="byteLength"/> into memory starting 
        /// at <paramref name="bytePosition"/>.
        /// </summary>
        public readonly void Write(uint bytePosition, uint byteLength, MemoryAddress otherData)
        {
            unchecked
            {
                Span<byte> bytes = new(otherData, (int)byteLength);
                bytes.CopyTo(new Span<byte>(pointer + (int)bytePosition, (int)byteLength));
            }
        }

        /// <summary>
        /// Retrieves a span slice of the bytes in this allocation.
        /// </summary>
        public readonly USpan<byte> AsSpan(uint bytePosition, uint byteLength)
        {
            return new USpan<byte>(pointer + bytePosition, byteLength);
        }

        /// <summary>
        /// Gets a span of bytes from the start of the memory with the specified <paramref name="byteLength"/>.
        /// </summary>
        public readonly USpan<byte> GetSpan(uint byteLength)
        {
            return new USpan<byte>(pointer, byteLength);
        }

        /// <summary>
        /// Gets a span of elements from the memory with the given <paramref name="length"/>
        /// in <typeparamref name="T"/> elements.
        /// </summary>
        public readonly USpan<T> GetSpan<T>(uint length) where T : unmanaged
        {
            return new USpan<T>(pointer, length);
        }

        /// <summary>
        /// Gets a span of elements from the memory.
        /// <para>Both <paramref name="start"/> and <paramref name="length"/> are expected
        /// to be in <typeparamref name="T"/> elements.</para>
        /// </summary>
        public readonly USpan<T> AsSpan<T>(uint start, uint length) where T : unmanaged
        {
            unchecked
            {
                return new USpan<T>(pointer + start * (uint)sizeof(T), length);
            }
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the memory starting from the given byte position.
        /// </summary>
        public readonly ref T Read<T>(uint bytePosition) where T : unmanaged
        {
            return ref *(T*)(pointer + bytePosition);
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> at the given element <paramref name="index"/>.
        /// </summary>
        public readonly ref T ReadElement<T>(uint index) where T : unmanaged
        {
            unchecked
            {
                return ref ((T*)pointer)[index];
            }
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the memory starting from the given byte position.
        /// </summary>
        public readonly ref T Read<T>() where T : unmanaged
        {
            return ref *(T*)pointer;
        }

        /// <summary>
        /// Reads data from the memory starting from the given <paramref name="bytePosition"/>.
        /// </summary>
        public readonly MemoryAddress Read(uint bytePosition)
        {
            return new(pointer + bytePosition);
        }

        /// <summary>
        /// Resets the memory to <c>default</c> state.
        /// </summary>
        public readonly void Clear(uint byteLength)
        {
            NativeMemory.Clear(pointer, byteLength);
        }

        /// <summary>
        /// Resets a range of memory to <c>default</c> state.
        /// </summary>
        public readonly void Clear(uint bytePosition, uint byteLength)
        {
            NativeMemory.Clear(pointer + bytePosition, byteLength);
        }

        /// <summary>
        /// Fills the memory with the given byte value.
        /// </summary>
        public readonly void Fill(uint byteLength, byte value)
        {
            NativeMemory.Fill(pointer, byteLength, value);
        }

        /// <summary>
        /// Fills the memory with the given byte value.
        /// </summary>
        public readonly void Fill(uint bytePosition, uint byteLength, byte value)
        {
            NativeMemory.Fill(pointer + bytePosition, byteLength, value);
        }

        /// <summary>
        /// Copies bytes of this allocation into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(MemoryAddress destination, uint sourceBytePosition, uint destinationBytePosition, uint byteLength)
        {
            unchecked
            {
                Span<byte> source = new(pointer + (int)sourceBytePosition, (int)byteLength);
                Span<byte> dest = new(destination.pointer + (int)destinationBytePosition, (int)byteLength);
                source.CopyTo(dest);
            }
        }

        /// <summary>
        /// Copies bytes of this allocation into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(MemoryAddress destination, uint byteLength)
        {
            unchecked
            {
                Span<byte> source = new(pointer, (int)byteLength);
                Span<byte> dest = new(destination.pointer, (int)byteLength);
                source.CopyTo(dest);
            }
        }

        /// <summary>
        /// Copies bytes of this allocation into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(void* destination, uint byteLength)
        {
            unchecked
            {
                Span<byte> source = new(pointer, (int)byteLength);
                Span<byte> dest = new(destination, (int)byteLength);
                source.CopyTo(dest);
            }
        }

        /// <summary>
        /// Copies the bytes from <paramref name="source"/> and writes them into this allocation.
        /// </summary>
        public readonly void CopyFrom(MemoryAddress source, uint byteLength)
        {
            unchecked
            {
                Span<byte> sourceSpan = new(source.pointer, (int)byteLength);
                Span<byte> destSpan = new(pointer, (int)byteLength);
                sourceSpan.CopyTo(destSpan);
            }
        }

        /// <summary>
        /// Copies the bytes from <paramref name="source"/> and writes them into this allocation.
        /// </summary>
        public readonly void CopyFrom(void* source, uint byteLength)
        {
            unchecked
            {
                Span<byte> sourceSpan = new(source, (int)byteLength);
                Span<byte> destSpan = new(pointer, (int)byteLength);
                sourceSpan.CopyTo(destSpan);
            }
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
                nint address = (nint)pointer;
                return (int)address;
            }
        }

        /// <summary>
        /// Moves existing memory into a new allocation of the given <paramref name="newByteLength"/>.
        /// </summary>
        public static void Resize(ref MemoryAddress allocation, uint newByteLength)
        {
            allocation.ThrowIfDefault();

            allocation.pointer = (byte*)NativeMemory.Realloc(allocation.pointer, newByteLength);
        }

        /// <summary>
        /// Moves existing memory into a new allocation that is able to
        /// fit <typeparamref name="T"/>.
        /// </summary>
        public static void Resize<T>(ref MemoryAddress allocation) where T : unmanaged
        {
            Resize(ref allocation, (uint)sizeof(T));
        }

        /// <summary>
        /// Creates an empty allocation of size 0.
        /// </summary>
        public static MemoryAddress AllocateEmpty()
        {
            return new(NativeMemory.Alloc(0));
        }

        /// <summary>
        /// Creates an allocation of size <paramref name="byteLength"/>, initialized
        /// to <see langword="default"/> memory.
        /// </summary>
        public static MemoryAddress AllocateZeroed(uint byteLength)
        {
            return new(NativeMemory.AllocZeroed(byteLength));
        }

        /// <summary>
        /// Creates a new non-zeroed allocation of size <paramref name="byteLength"/>.
        /// </summary>
        public static MemoryAddress Allocate(uint byteLength)
        {
            return new(NativeMemory.Alloc(byteLength));
        }

        /// <summary>
        /// Creates a new allocation that contains the data of the given <paramref name="value"/>.
        /// </summary>
        public static MemoryAddress Allocate<T>(T value) where T : unmanaged
        {
            void* pointer = NativeMemory.Alloc((uint)sizeof(T));
            *(T*)pointer = value;
            return new(pointer);
        }

        /// <summary>
        /// Creates a new uninitialized allocation that can contain a(n) <typeparamref name="T"/>
        /// </summary>
        public static MemoryAddress Allocate<T>() where T : unmanaged
        {
            return new(NativeMemory.Alloc((uint)sizeof(T)));
        }

        /// <summary>
        /// Creates a new allocation containg the given <paramref name="span"/>.
        /// </summary>
        public static MemoryAddress Allocate<T>(USpan<T> span) where T : unmanaged
        {
            uint byteLength = (uint)sizeof(T) * span.Length;
            void* pointer = NativeMemory.Alloc(byteLength);
            span.CopyTo(pointer, byteLength);
            return new(pointer);
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