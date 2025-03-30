using System;

namespace Unmanaged
{
    /// <summary>
    /// Unmanaged code compatible <see cref="bool"/> type.
    /// </summary>
    public readonly struct Bool : IEquatable<Bool>
    {
        /// <summary>
        /// <see langword="false"/> value.
        /// </summary>
        public static readonly Bool False = new(0);

        /// <summary>
        /// <see langword="true"/> value.
        /// </summary>
        public static readonly Bool True = new(1);

        private readonly byte value;

        /// <summary>
        /// Initialize a new instance with the given <paramref name="value"/>.
        /// </summary>
        public unsafe Bool(bool value)
        {
            this.value = *(byte*)&value;
        }

        private Bool(byte value)
        {
            this.value = value;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return value == 0 ? "False" : "True";
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Bool otherValue && Equals(otherValue);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Bool other)
        {
            return value == other.value;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return value;
        }

        /// <inheritdoc/>
        public static bool operator ==(Bool left, Bool right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Bool left, Bool right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public unsafe static implicit operator Bool(bool value)
        {
            return new(value);
        }

        /// <inheritdoc/>
        public unsafe static implicit operator bool(Bool value)
        {
            return *(bool*)&value.value;
        }
    }
}
