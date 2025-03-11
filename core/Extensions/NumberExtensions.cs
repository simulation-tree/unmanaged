using System;

namespace Unmanaged
{
    /// <summary>
    /// Extension functions for <see cref="uint"/> values.
    /// </summary>
    public static class NumberExtensions
    {
        /// <summary>
        /// Returns an alignment that is able to contain the input <paramref name="stride"/>.
        /// </summary>
        public static uint GetAlignment(this uint stride)
        {
            if ((stride & 7) == 0)
            {
                return 8u;
            }

            if ((stride & 3) == 0)
            {
                return 4u;
            }

            return (stride & 1) == 0 ? 2u : 1u;
        }

        /// <summary>
        /// Retrieves the upper bound of the given input <paramref name="stride"/> and <paramref name="alignment"/>.
        /// </summary>
        public static uint CeilAlignment(this uint stride, uint alignment)
        {
            return alignment switch
            {
                1 => stride,
                2 => ((stride + 1) >> 1) * 2,
                4 => ((stride + 3) >> 2) * 4,
                8 => ((stride + 7) >> 3) * 8,
                _ => throw new ArgumentException($"Invalid alignment {alignment}"),
            };
        }

        /// <summary>
        /// Retrieves the next power of 2 for the input <paramref name="value"/>.
        /// </summary>
        public static uint GetNextPowerOf2(this uint value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return ++value;
        }

        /// <summary>
        /// Retrieves the next power of 2 for the input <paramref name="value"/>.
        /// </summary>
        public static int GetNextPowerOf2(this int value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return ++value;
        }

        /// <summary>
        /// Retrieves the index of the power of 2 for the input <paramref name="value"/>.
        /// <para>
        /// If <paramref name="value"/> isn't a power of 2 the returning value
        /// isn't valid.
        /// </para>
        /// </summary>
        public static uint GetIndexOfPowerOf2(this uint value)
        {
            uint index = 0;
            while ((value & 1) == 0)
            {
                value >>= 1;
                index++;
            }

            return index;
        }

        /// <summary>
        /// Retrieves the index of the power of 2 for the input <paramref name="value"/>.
        /// <para>
        /// If <paramref name="value"/> isn't a power of 2 the returning value
        /// isn't valid.
        /// </para>
        /// </summary>
        public static int GetIndexOfPowerOf2(this int value)
        {
            int index = 0;
            while ((value & 1) == 0)
            {
                value >>= 1;
                index++;
            }

            return index;
        }
    }
}