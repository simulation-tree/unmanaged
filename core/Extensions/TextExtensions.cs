using System;

namespace Unmanaged
{
    /// <summary>
    /// Extensions for text.
    /// </summary>
    public static class TextExtensions
    {
        /// <summary>
        /// Retrieves a <see cref="long"/> hash for the input <paramref name="text"/>.
        /// </summary>
        public static long GetLongHashCode(this string text)
        {
            unchecked
            {
                long hash = 3074457345618258791;
                for (int i = 0; i < text.Length; i++)
                {
                    hash += text[i];
                    hash *= 3074457345618258799;
                }

                return hash;
            }
        }

        /// <summary>
        /// Retrieves a <see cref="long"/> hash for the input <paramref name="text"/>.
        /// </summary>
        public static long GetLongHashCode(this Span<char> text)
        {
            unchecked
            {
                long hash = 3074457345618258791;
                for (int i = 0; i < text.Length; i++)
                {
                    hash += text[i];
                    hash *= 3074457345618258799;
                }

                return hash;
            }
        }

        /// <summary>
        /// Retrieves a <see cref="long"/> hash for the input <paramref name="text"/>.
        /// </summary>
        public static long GetLongHashCode(this ReadOnlySpan<char> text)
        {
            unchecked
            {
                long hash = 3074457345618258791;
                for (int i = 0; i < text.Length; i++)
                {
                    hash += text[i];
                    hash *= 3074457345618258799;
                }

                return hash;
            }
        }

        /// <summary>
        /// Retrieves a <see cref="long"/> hash for the input <paramref name="text"/>.
        /// </summary>
        public static long GetLongHashCode(this ASCIIText256 text)
        {
            unchecked
            {
                long hash = 3074457345618258791;
                for (int i = 0; i < text.Length; i++)
                {
                    hash += text[i];
                    hash *= 3074457345618258799;
                }

                return hash;
            }
        }
    }
}