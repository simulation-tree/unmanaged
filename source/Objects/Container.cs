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

        public Container()
        {
            throw new NotImplementedException("Empty container is not supported.");
        }

        private Container(void* pointer, RuntimeType type)
        {
            this.pointer = pointer;
            this.type = type;
        }

        public void Dispose()
        {
            Allocations.ThrowIfNull(pointer);
            Allocations.Free(ref pointer);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSizeMismatch(int size)
        {
            if (size != type.Size)
            {
                throw new ArgumentException("Size mismatch.", nameof(size));
            }
        }

        /// <summary>
        /// Retrieves a span of all bytes in the container.
        /// </summary>
        public unsafe readonly Span<byte> AsSpan()
        {
            Allocations.ThrowIfNull(pointer);
            return new Span<byte>(pointer, type.Size);
        }

        public unsafe readonly ref T AsRef<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfSizeMismatch(sizeof(T));
            return ref Unsafe.AsRef<T>(pointer);
        }

        public readonly bool Is<T>() where T : unmanaged
        {
            return type == RuntimeType.Get<T>();
        }

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

        public static Container Create(RuntimeType type, ReadOnlySpan<byte> bytes)
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