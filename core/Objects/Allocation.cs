#if DEBUG
#define TRACK
#endif

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// An unmanaged allocation of memory.
    /// </summary>
    [DebuggerTypeProxy(typeof(AllocationDebugView))]
    public unsafe struct Allocation : IDisposable, IEquatable<Allocation>
    {
        private void* pointer;

        /// <summary>
        /// Has this allocation been disposed? Also counts for instances that weren't allocated.
        /// </summary>
        public readonly bool IsDisposed => pointer is null;

        /// <summary>
        /// Native address of this memory.
        /// </summary>
        public readonly nint Address => (nint)pointer;

        /// <summary>
        /// Native pointer of this memory.
        /// </summary>
        public readonly void* Pointer => pointer;

        /// <summary>
        /// Gets or sets a byte at the given index.
        /// </summary>
        public readonly ref byte this[uint index]
        {
            get
            {
                Allocations.ThrowIfNull(pointer);
                ThrowIfIndexOutOfRange(index);

                return ref *(byte*)((nint)pointer + index);
            }
        }

        /// <summary>
        /// Creates an existing allocation from the given <paramref name="pointer"/>.
        /// </summary>
        public Allocation(void* pointer)
        {
            this.pointer = pointer;
        }

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public Allocation()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Frees the allocation.
        /// </summary>
        public void Dispose()
        {
            Allocations.ThrowIfNull(pointer);
            Allocations.Free(ref pointer);
        }

        [Conditional("TRACK")]
        private readonly void ThrowIfIndexOutOfRange(uint index)
        {
            if (Allocations.Tracker.TryGetSize(Address, out uint byteLength) && index >= byteLength)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for allocation of size {byteLength}");
            }
        }

        [Conditional("TRACK")]
        private readonly void ThrowIfPastRange(uint index)
        {
            if (Allocations.Tracker.TryGetSize(Address, out uint byteLength) && index > byteLength)
            {
                throw new IndexOutOfRangeException($"Index {index} is past the range of allocation of size {byteLength}");
            }
        }

        /// <summary>
        /// String representation of this allocation value.
        /// </summary>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[16];
            uint length = ToString(buffer);
            return buffer.GetSpan(length).ToString();
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
            *(T*)((nint)pointer + bytePosition) = value;
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
                Span<T> thisSpan = new((void*)((nint)pointer + bytePosition), (int)span.Length);
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
                Span<T> thisSpan = new(pointer, (int)span.Length * sizeof(T));
                span.CopyTo(thisSpan);
            }
        }

        /// <summary>
        /// Writes the given data with a custom length into memory starting at this position in bytes.
        /// </summary>
        public readonly void Write(uint bytePosition, uint byteLength, Allocation data)
        {
            unchecked
            {
                Span<byte> bytes = new(data, (int)byteLength);
                bytes.CopyTo(new Span<byte>((byte*)pointer + (int)bytePosition, (int)byteLength));
            }
        }

        /// <summary>
        /// Retrieves a span slice of the bytes in this allocation.
        /// </summary>
        public readonly USpan<byte> AsSpan(uint bytePosition, uint byteLength)
        {
            return new USpan<byte>((void*)((nint)pointer + bytePosition), byteLength);
        }

        /// <summary>
        /// Gets a span of bytes from the memory with the <paramref name="byteLength"/>.
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
                return new USpan<T>((void*)((nint)pointer + start * (uint)sizeof(T)), length);
            }
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the memory starting from the given byte position.
        /// </summary>
        public readonly ref T Read<T>(uint bytePosition) where T : unmanaged
        {
            return ref *(T*)((nint)pointer + bytePosition);
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
        public readonly Allocation Read(uint bytePosition)
        {
            return new((void*)((nint)pointer + bytePosition));
        }

        /// <summary>
        /// Resets the memory to <c>default</c> state.
        /// </summary>
        public readonly void Clear(uint byteLength)
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfPastRange(byteLength);

            NativeMemory.Clear(pointer, byteLength);
        }

        /// <summary>
        /// Resets a range of memory to <c>default</c> state.
        /// </summary>
        public readonly void Clear(uint bytePosition, uint byteLength)
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfPastRange(bytePosition + byteLength);

            nint address = (nint)((nint)pointer + bytePosition);
            NativeMemory.Clear((void*)address, byteLength);
        }

        /// <summary>
        /// Fills the memory with the given byte value.
        /// </summary>
        public readonly void Fill(uint byteLength, byte value)
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfPastRange(byteLength);

            NativeMemory.Fill(pointer, byteLength, value);
        }

        /// <summary>
        /// Fills the memory with the given byte value.
        /// </summary>
        public readonly void Fill(uint bytePosition, uint byteLength, byte value)
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfPastRange(bytePosition + byteLength);

            nint address = (nint)((nint)pointer + bytePosition);
            NativeMemory.Fill((void*)address, byteLength, value);
        }

        /// <summary>
        /// Copies bytes of this allocation into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(Allocation destination, uint sourceBytePosition, uint destinationBytePosition, uint byteLength)
        {
            unchecked
            {
                Span<byte> source = new((byte*)pointer + (int)sourceBytePosition, (int)byteLength);
                Span<byte> dest = new((byte*)destination.pointer + (int)destinationBytePosition, (int)byteLength);
                source.CopyTo(dest);
            }
        }

        /// <summary>
        /// Copies bytes of this allocation into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(Allocation destination, uint byteLength)
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
        public readonly void CopyFrom(Allocation source, uint byteLength)
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
            return obj is Allocation allocation && Equals(allocation);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Allocation other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return pointer == other.pointer;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine((nint)pointer);
        }

        /// <summary>
        /// Moves existing memory into a new allocation of the given size.
        /// </summary>
        public static void Resize(ref Allocation allocation, uint newLength)
        {
            Allocations.ThrowIfNull(allocation.pointer);

            allocation = new(Allocations.Reallocate(allocation.pointer, newLength));
        }

        /// <summary>
        /// Moves existing memory into a new allocation that is able to
        /// fit the given type.
        /// </summary>
        public static void Resize<T>(ref Allocation allocation) where T : unmanaged
        {
            Resize(ref allocation, (uint)sizeof(T));
        }

        /// <summary>
        /// Creates an empty allocation of size 0.
        /// </summary>
        public static Allocation CreateEmpty()
        {
            void* pointer = Allocations.Allocate(0);
            return new(pointer);
        }

        /// <summary>
        /// Creates an allocation of size <paramref name="byteLength"/>, initialized
        /// to <see langword="default"/> memory.
        /// </summary>
        public static Allocation CreateZeroed(uint byteLength)
        {
            void* pointer = Allocations.AllocateZeroed(byteLength);
            return new(pointer);
        }

        /// <summary>
        /// Creates a new non-zeroed allocation of size <paramref name="byteLength"/>.
        /// </summary>
        public static Allocation Create(uint byteLength)
        {
            void* pointer = Allocations.Allocate(byteLength);
            return new(pointer);
        }

        /// <summary>
        /// Creates a new allocation that contains the data of the given <paramref name="value"/>.
        /// </summary>
        public static Allocation CreateFromValue<T>(T value) where T : unmanaged
        {
            ref T reference = ref Allocations.Allocate<T>();
            reference = value;
            fixed (T* pointer = &reference)
            {
                return new(pointer);
            }
        }

        /// <summary>
        /// Creates a new uninitialized allocation that can contain a(n) <typeparamref name="T"/>
        /// </summary>
        public static Allocation Create<T>() where T : unmanaged
        {
            void* pointer = Allocations.Allocate((uint)sizeof(T));
            return new(pointer);
        }

        /// <summary>
        /// Creates a new allocation containg the given <paramref name="span"/>.
        /// </summary>
        public static Allocation Create<T>(USpan<T> span) where T : unmanaged
        {
            void* pointer = Allocations.Allocate(span.Length * (uint)sizeof(T));
            Allocation allocation = new(pointer);
            if (span.Length > 0)
            {
                span.CopyTo(allocation.GetSpan<T>(span.Length));
            }

            return allocation;
        }

        /// <summary>
        /// Retrieves an existing allocation from the reference to <paramref name="value"/>.
        /// </summary>
        public static Allocation Get<T>(ref T value) where T : unmanaged
        {
            fixed (T* pointer = &value)
            {
                return new(pointer);
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(Allocation left, Allocation right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Allocation left, Allocation right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static implicit operator void*(Allocation allocation)
        {
            return allocation.pointer;
        }

        /// <inheritdoc/>
        public static implicit operator Allocation*(Allocation allocation)
        {
            return (Allocation*)allocation.pointer;
        }

        /// <inheritdoc/>
        public static implicit operator nint(Allocation allocation)
        {
            return allocation.Address;
        }

        internal class AllocationDebugView
        {
#if DEBUG
            public readonly Allocation allocation;
            public readonly nint address;
            public readonly uint byteLength;

            public AllocationDebugView(Allocation allocation)
            {
                this.allocation = allocation;
                address = allocation.Address;
                byteLength = Allocations.Tracker.TryGetSize(address, out uint size) ? size : 0;
            }
#endif
        }
    }
}