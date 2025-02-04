using System;

namespace Unmanaged
{
    /// <summary>
    /// Describes the start and length of a range.
    /// </summary>
    public struct URange : IEquatable<URange>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public uint start;

        /// <summary>
        /// End index.
        /// </summary>
        public uint end;

        /// <summary>
        /// Length of the range.
        /// </summary>
        public readonly uint Length => end - start;

        /// <inheritdoc/>
        public URange(uint start, uint end)
        {
            this.start = start;
            this.end = end;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            Range range = this;
            return range.ToString();
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is URange range && Equals(range);
        }

        /// <inheritdoc/>
        public readonly bool Equals(URange other)
        {
            return start == other.start && end == other.end;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return System.HashCode.Combine(start, end);
        }

        /// <inheritdoc/>
        public static bool operator ==(URange left, URange right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(URange left, URange right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static implicit operator URange(Range range)
        {
            return new URange((uint)range.Start.Value, (uint)range.End.Value);
        }

        /// <inheritdoc/>
        public static implicit operator Range(URange range)
        {
            return new Range((int)range.start, (int)range.end);
        }
    }
}