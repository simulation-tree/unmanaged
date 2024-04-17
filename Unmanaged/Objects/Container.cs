using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// A general container of unmanaged data types that must be disposed.
    /// </summary>
    public readonly struct Container : IDisposable, IEquatable<Container>
    {
        /// <summary>
        /// The type of the data stored.
        /// </summary>
        public readonly RuntimeType type;

        public readonly nint pointer;

        public readonly bool IsDisposed
        {
            get
            {
                try
                {
                    Allocations.ThrowIfNull(pointer);
                    return false;
                }
                catch
                {
                    return true;
                }
            }
        }

        public Container()
        {
            throw new NotImplementedException();
        }

        private Container(nint pointer, RuntimeType type)
        {
            this.pointer = pointer;
            this.type = type;
        }

        public readonly void Dispose()
        {
            Allocations.ThrowIfNull(pointer);

            Marshal.FreeHGlobal(pointer);
            Allocations.Unregister(pointer);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSizeMismatch(int size)
        {
            if (size != type.size)
            {
                throw new ArgumentException("Size mismatch.", nameof(size));
            }
        }

        public unsafe readonly ReadOnlySpan<byte> AsSpan()
        {
            Allocations.ThrowIfNull(pointer);
            return new ReadOnlySpan<byte>((void*)pointer, type.size);
        }

        public unsafe readonly ref T AsRef<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfSizeMismatch(sizeof(T));
            return ref Unsafe.AsRef<T>((void*)pointer);
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

            return pointer.Equals(other.pointer);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(pointer);
        }

        /// <summary>
        /// Allocates unmanaged memory to contain the given value.
        /// </summary>
        public unsafe static Container Allocate<T>(T value) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            Container container = new(Marshal.AllocHGlobal(type.size), type);
            Unsafe.Write((void*)container.pointer, value);
            Allocations.Register(container.pointer);
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