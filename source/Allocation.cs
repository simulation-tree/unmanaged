#if DEBUG
#define TRACK
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// An unmanaged allocation of memory.
    /// </summary>
    public unsafe struct Allocation<T> : IDisposable, IEquatable<Allocation<T>>, IEquatable<Allocation> where T : unmanaged
    {
        private T* pointer;

        /// <summary>
        /// Has this allocation been disposed? Also counts for instances that weren't allocated.
        /// </summary>
        public readonly bool IsDisposed => pointer is null;

        /// <summary>
        /// Native address of this allocated memory.
        /// </summary>
        public readonly nint Address => (nint)pointer;

        /// <summary>
        /// The value of this allocation.
        /// </summary>
        public readonly ref T Value => ref *pointer;

        /// <summary>
        /// Gets or sets a byte at the given index.
        /// </summary>
        public readonly ref byte this[uint index]
        {
            get
            {
                Allocations.ThrowIfNull(pointer);
                ThrowIfIndexOutOfRange(index);
#if NET
                return ref Unsafe.Add(ref Unsafe.AsRef<byte>(pointer), index);
#else
                return ref Unsafe.Add(ref Unsafe.AsRef<byte>(pointer), (int)index);
#endif
            }
        }

        /// <summary>
        /// Creates an existing allocation from the given pointer.
        /// </summary>
        public Allocation(T* pointer)
        {
            this.pointer = pointer;
        }

        /// <summary>
        /// Creates a new uninitialized allocation with the given size.
        /// </summary>
        public Allocation(bool clear)
        {
            pointer = Allocations.AllocatePointer<T>();
            if (clear)
            {
                Clear((uint)sizeof(T));
            }
        }

#if NET
        /// <summary>
        /// Creates a new empty allocation.
        /// </summary>
        public Allocation()
        {
            pointer = Allocations.AllocatePointer<T>();
        }
#endif

        /// <summary>
        /// Creates a new allocation containing the given value.
        /// </summary>
        public Allocation(T value)
        {
            pointer = Allocations.AllocatePointer<T>();
            ref T destination = ref *pointer;
            destination = value;
        }

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
            return Value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// String representation of this allocation value.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            string str = Value.ToString() ?? string.Empty;
            USpan<char> span = str.AsSpan();
            uint length = Math.Min(span.Length, buffer.Length);
            span.Slice(0, length).CopyTo(buffer);
            return length;
        }

        /// <summary>
        /// Writes the given data with a custom length into memory starting at this position in bytes.
        /// </summary>
        public readonly void Write(uint bytePosition, uint byteLength, void* data)
        {
            Unsafe.CopyBlock((void*)((nint)pointer + bytePosition), data, byteLength);
        }

        /// <summary>
        /// Retrieves a span slice of the bytes in this allocation.
        /// </summary>
        public readonly USpan<byte> AsSpan(uint bytePosition, uint byteLength)
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfPastRange(bytePosition + byteLength);

            return new USpan<byte>((void*)((nint)pointer + bytePosition), byteLength);
        }

        /// <summary>
        /// Reads data from the memory starting from the given <paramref name="bytePosition"/>.
        /// </summary>
        public readonly void* Read(uint bytePosition)
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfIndexOutOfRange(bytePosition);

            return (void*)((nint)pointer + bytePosition);
        }

        /// <summary>
        /// Resets the memory to <c>default</c> state.
        /// </summary>
        public readonly void Clear()
        {
            Allocations.ThrowIfNull(pointer);

            NativeMemory.Clear(pointer, (uint)sizeof(T));
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
        public readonly void CopyTo(Allocation<T> destination)
        {
            void* destinationStart = (void*)((nint)destination.pointer);
            void* sourceStart = (void*)((nint)pointer);
            Unsafe.CopyBlock(destinationStart, sourceStart, (uint)sizeof(T));
        }

        /// <summary>
        /// Copies bytes of this allocation into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(Allocation<T> destination, uint byteLength)
        {
            Unsafe.CopyBlock(destination.pointer, pointer, byteLength);
        }

        /// <summary>
        /// Copies the bytes from <paramref name="source"/> and writes them into this allocation.
        /// </summary>
        public readonly void CopyFrom(Allocation<T> source, uint byteLength)
        {
            Unsafe.CopyBlock(pointer, source.pointer, byteLength);
        }

        /// <summary>
        /// Copies the bytes from <paramref name="source"/> and writes them into this allocation.
        /// </summary>
        public readonly void CopyFrom(void* source, uint byteLength)
        {
            Unsafe.CopyBlock(pointer, source, byteLength);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Allocation<T> allocation && Equals(allocation);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Allocation<T> other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return pointer == other.pointer;
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
        /// Creates an empty allocation of size 0.
        /// </summary>
        public static Allocation<T> Create()
        {
            return new();
        }

        /// <summary>
        /// Creates an allocation that contains the data of the given value.
        /// </summary>
        public static Allocation<T> Create(T value)
        {
            return new(value);
        }

        /// <inheritdoc/>
        public static bool operator ==(Allocation<T> left, Allocation<T> right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Allocation<T> left, Allocation<T> right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static implicit operator void*(Allocation<T> allocation)
        {
            return allocation.pointer;
        }

        /// <inheritdoc/>
        public static implicit operator T(Allocation<T> allocation)
        {
            return allocation.Value;
        }

        /// <inheritdoc/>
        public static implicit operator Allocation<T>*(Allocation<T> allocation)
        {
            return (Allocation<T>*)allocation.pointer;
        }

        /// <inheritdoc/>
        public static implicit operator nint(Allocation<T> allocation)
        {
            return allocation.Address;
        }
    }

    /// <summary>
    /// An unmanaged allocation of memory.
    /// </summary>
    [DebuggerTypeProxy(typeof(AllocationDebugView))]
    public unsafe struct Allocation : IDisposable, IEquatable<Allocation>
    {
        internal void* pointer;

        /// <summary>
        /// Has this allocation been disposed? Also counts for instances that weren't allocated.
        /// </summary>
        public readonly bool IsDisposed => pointer is null;

        /// <summary>
        /// Native address of this allocated memory.
        /// </summary>
        public readonly nint Address => (nint)pointer;

        /// <summary>
        /// Gets or sets a byte at the given index.
        /// </summary>
        public readonly ref byte this[uint index]
        {
            get
            {
                Allocations.ThrowIfNull(pointer);
                ThrowIfIndexOutOfRange(index);
#if NET
                return ref Unsafe.Add(ref Unsafe.AsRef<byte>(pointer), index);
#else
                return ref Unsafe.Add(ref Unsafe.AsRef<byte>(pointer), (int)index);
#endif
            }
        }

        /// <summary>
        /// Creates an existing allocation from the given <paramref name="pointer"/>.
        /// </summary>
        public Allocation(void* pointer)
        {
            this.pointer = pointer;
        }

        /// <summary>
        /// Creates a new uninitialized allocation with the given size.
        /// </summary>
        public Allocation(uint size, bool clear = false)
        {
            pointer = Allocations.Allocate(size);
            if (clear)
            {
                Clear(size);
            }
        }

#if NET
        /// <summary>
        /// Creates a new empty allocation.
        /// </summary>
        public Allocation()
        {
            pointer = Allocations.Allocate(0);
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
            return buffer.Slice(0, length).ToString();
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
            uint stride = (uint)sizeof(T);
            Write(bytePosition, stride, &value);
        }

        /// <summary>
        /// Writes a single given value into the memory starting at the beginning.
        /// </summary>
        public readonly void Write<T>(T value) where T : unmanaged
        {
            Write(0, value);
        }

        /// <summary>
        /// Writes the given span into memory starting at this position in bytes.
        /// </summary>
        public readonly void Write<T>(uint bytePosition, USpan<T> span) where T : unmanaged
        {
            uint byteLength = span.Length * (uint)sizeof(T);
            Allocations.ThrowIfNull(pointer);
            ThrowIfPastRange(bytePosition + byteLength);

            Write(bytePosition, byteLength, span.Pointer);
        }

        /// <summary>
        /// Writes the given data with a custom length into memory starting at this position in bytes.
        /// </summary>
        public readonly void Write(uint bytePosition, uint byteLength, void* data)
        {
            Unsafe.CopyBlock((void*)((nint)pointer + bytePosition), data, byteLength);
        }

        /// <summary>
        /// Retrieves a span slice of the bytes in this allocation.
        /// </summary>
        public readonly USpan<byte> AsSpan(uint bytePosition, uint byteLength)
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfPastRange(bytePosition + byteLength);

            return new USpan<byte>((void*)((nint)pointer + bytePosition), byteLength);
        }

        /// <summary>
        /// Gets a span of elements from the memory.
        /// <para>Both <paramref name="start"/> and <paramref name="length"/> are expected
        /// to be in <typeparamref name="T"/> elements.</para>
        /// </summary>
        public readonly USpan<T> AsSpan<T>(uint start, uint length) where T : unmanaged
        {
            uint stride = (uint)sizeof(T);
            uint bytePosition = start * stride;
            return new USpan<T>((void*)((nint)pointer + bytePosition), length);
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the memory starting from the given byte position.
        /// </summary>
        public readonly ref T Read<T>(uint bytePosition = 0) where T : unmanaged
        {
            void* value = (void*)((nint)pointer + bytePosition);
            return ref *(T*)value;
        }

        /// <summary>
        /// Reads data from the memory starting from the given <paramref name="bytePosition"/>.
        /// </summary>
        public readonly void* Read(uint bytePosition)
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfIndexOutOfRange(bytePosition);

            return (void*)((nint)pointer + bytePosition);
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
        public readonly void CopyTo(Allocation destination, uint sourceIndex, uint destinationIndex, uint byteLength)
        {
            void* destinationStart = (void*)((nint)destination.pointer + destinationIndex);
            void* sourceStart = (void*)((nint)pointer + sourceIndex);
            Unsafe.CopyBlock(destinationStart, sourceStart, byteLength);
        }

        /// <summary>
        /// Copies bytes of this allocation into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(Allocation destination, uint byteLength)
        {
            Unsafe.CopyBlock(destination.pointer, pointer, byteLength);
        }

        /// <summary>
        /// Copies the bytes from <paramref name="source"/> and writes them into this allocation.
        /// </summary>
        public readonly void CopyFrom(Allocation source, uint byteLength)
        {
            Unsafe.CopyBlock(pointer, source.pointer, byteLength);
        }

        /// <summary>
        /// Copies the bytes from <paramref name="source"/> and writes them into this allocation.
        /// </summary>
        public readonly void CopyFrom(void* source, uint byteLength)
        {
            Unsafe.CopyBlock(pointer, source, byteLength);
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
        public static Allocation Create()
        {
            return new(0);
        }

        /// <summary>
        /// Creates an allocation that contains the data of the given value.
        /// </summary>
        public static Allocation Create<T>(T value) where T : unmanaged
        {
            Allocation allocation = new((uint)sizeof(T));
            allocation.Write(0, value);
            return allocation;
        }

        /// <summary>
        /// Creates an uninitialized allocation that can contain a(n) <typeparamref name="T"/>
        /// </summary>
        public static Allocation Create<T>() where T : unmanaged
        {
            return new((uint)sizeof(T), true);
        }

        /// <summary>
        /// Creates an allocation containg the given <paramref name="span"/>.
        /// </summary>
        public static Allocation Create<T>(USpan<T> span) where T : unmanaged
        {
            uint length = span.Length * (uint)sizeof(T);
            Allocation allocation = new(length);
            if (span.Length > 0)
            {
                span.CopyTo(allocation.AsSpan<T>(0, span.Length));
            }

            return allocation;
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
