using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// Unmanaged container of data.
    /// </summary>
    public readonly unsafe struct Container : IDisposable, IEquatable<Container>
    {
        /// <summary>
        /// The type of the data stored.
        /// </summary>
        public readonly RuntimeType type;

        private readonly void* pointer;

        public readonly bool IsDisposed => Allocations.IsNull((nint)pointer);

        public Container()
        {
            throw new NotImplementedException("Empty container is not supported.");
        }

        private Container(void* pointer, RuntimeType type)
        {
            this.pointer = pointer;
            this.type = type;
        }

        public readonly void Dispose()
        {
            Allocations.ThrowIfNull((nint)pointer);
            NativeMemory.Free(pointer);
            Allocations.Unregister((nint)pointer);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSizeMismatch(int size)
        {
            if (size != type.size)
            {
                throw new ArgumentException("Size mismatch.", nameof(size));
            }
        }

        public unsafe readonly Span<byte> AsSpan()
        {
            Allocations.ThrowIfNull((nint)pointer);
            return new Span<byte>(pointer, type.size);
        }

        public unsafe readonly ref T AsRef<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull((nint)pointer);
            ThrowIfSizeMismatch(sizeof(T));
            return ref Unsafe.AsRef<T>(pointer);
        }

        public readonly bool Is<T>() where T : unmanaged
        {
            return type == RuntimeType.Get<T>();
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
        public unsafe static Container Create<T>(T value) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            Container container = new(NativeMemory.Alloc(type.size), type);
            Unsafe.Write(container.pointer, value);
            Allocations.Register((nint)container.pointer);
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