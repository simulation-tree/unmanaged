using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Unmanaged
{
    /// <summary>
    /// Unmanaged container of data.
    /// </summary>
    public unsafe struct Container : IDisposable, IEquatable<Container>
    {
        /// <summary>
        /// The type of the data stored.
        /// </summary>
        public readonly RuntimeType type;

        private void* pointer;

        public readonly bool IsDisposed => Allocations.IsNull(pointer);

        private Container(void* pointer, RuntimeType type)
        {
            this.pointer = pointer;
            this.type = type;
        }

#if NET5_0_OR_GREATER
        [Obsolete("Use Create() method", true)]
        public Container()
        {
            throw new NotImplementedException();
        }
#endif

        public void Dispose()
        {
            Allocations.ThrowIfNull(pointer);
            Allocations.Free(ref pointer);
        }

        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        public readonly uint ToString(USpan<char> buffer)
        {
            if (IsDisposed)
            {
                "<Disposed>".AsUSpan().CopyTo(buffer);
                return 10;
            }
            else
            {
                return type.ToString(buffer);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTypeMismatch(RuntimeType type)
        {
            if (this.type != type)
            {
                throw new ArgumentException("Type mismatch.", nameof(type));
            }
        }

        public readonly uint CopyTo(USpan<byte> destinationBuffer)
        {
            Allocations.ThrowIfNull(pointer);

            USpan<byte> sourceBuffer = AsSpan();
            uint length = Math.Min(sourceBuffer.Length, destinationBuffer.Length);
            sourceBuffer.Slice(0, length).CopyTo(destinationBuffer);
            return length;
        }

        /// <summary>
        /// Interprets the container as an <see cref="Allocation"/>
        /// </summary>
        public readonly Allocation AsAllocation()
        {
            return new(pointer);
        }

        /// <summary>
        /// Retrieves a span of all bytes in the container.
        /// </summary>
        public unsafe readonly USpan<byte> AsSpan()
        {
            Allocations.ThrowIfNull(pointer);
            return new USpan<byte>(pointer, type.Size);
        }

        public unsafe readonly ref T Read<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfTypeMismatch(RuntimeType.Get<T>());
            return ref Unsafe.AsRef<T>(pointer);
        }

        public readonly bool Is<T>() where T : unmanaged
        {
            return type == RuntimeType.Get<T>();
        }

        /// <summary>
        /// Creates a new copy of this container.
        /// </summary>
        public readonly Container Clone()
        {
            Allocations.ThrowIfNull(pointer);
            return Create(type, AsSpan());
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Container container && Equals(container);
        }

        public readonly bool Equals(Container other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return AsSpan().SequenceEqual(other.AsSpan());
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine((nint)pointer);
        }

        /// <summary>
        /// Allocates unmanaged memory to contain the given value.
        /// </summary>
        public static Container Create<T>(T value) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            Container container = new(Allocations.Allocate(type.Size), type);
            Unsafe.Write(container.pointer, value);
            return container;
        }

        public static Container Create(RuntimeType type, USpan<byte> bytes)
        {
            Container container = new(Allocations.Allocate(type.Size), type);
            bytes.CopyTo(container.AsSpan());
            return container;
        }

        public static bool operator ==(Container left, Container right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Container left, Container right)
        {
            return !(left == right);
        }
    }
}