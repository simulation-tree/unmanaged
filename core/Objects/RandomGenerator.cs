﻿using System;

namespace Unmanaged
{
    /// <summary>
    /// Pseudo-random number generator using XORshift.
    /// </summary>
    public unsafe struct RandomGenerator : IDisposable
    {
        private static uint counter;

        private MemoryAddress pointer;

        /// <summary>
        /// The current state of the generator.
        /// </summary>
        public readonly ulong State => pointer.Read<ulong>();

        /// <summary>
        /// Checks if the generator has been disposed.
        /// </summary>
        public readonly bool IsDisposed => pointer == default;

#if NET
        /// <summary>
        /// Creates a new random generator initialized with a random seed.
        /// </summary>
        public RandomGenerator()
        {
            pointer = MemoryAddress.AllocateValue(GetRandomSeed());
        }
#endif
        /// <summary>
        /// Creates a new disposable randomness generator.
        /// </summary>
        public RandomGenerator(ulong seed)
        {
            pointer = MemoryAddress.AllocateValue(seed);
        }

        /// <summary>
        /// Creates a new disposable randomness generator using the given byte
        /// sequence as the initialization seed.
        /// </summary>
        public RandomGenerator(Span<byte> seed)
        {
            unchecked
            {
                long hash = 17;
                for (int i = 0; i < seed.Length; i++)
                {
                    hash = hash * 31 + seed[i];
                }

                pointer = MemoryAddress.AllocateValue((ulong)hash);
            }
        }

        /// <summary>
        /// Creates a new disposable randomness generator using the given
        /// text input as the initialization seed.
        /// </summary>
        public RandomGenerator(Span<char> seed)
        {
            unchecked
            {
                long hash = seed.GetLongHashCode();
                pointer = MemoryAddress.AllocateValue((ulong)hash);
            }
        }

        /// <summary>
        /// Creates a new disposable randomness generator using the given
        /// text input as the initialization seed.
        /// </summary>
        public RandomGenerator(ASCIIText256 seed)
        {
            unchecked
            {
                long hash = seed.GetLongHashCode();
                pointer = MemoryAddress.AllocateValue((ulong)hash);
            }
        }

        /// <summary>
        /// Creates a new disposable randomness generator using the given
        /// text input as the initialization seed.
        /// </summary>
        public RandomGenerator(string seed)
        {
            unchecked
            {
                long hash = seed.GetLongHashCode();
                pointer = MemoryAddress.AllocateValue((ulong)hash);
            }
        }

        /// <summary>
        /// Disposes the generator and releases the memory used by it.
        /// </summary>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(pointer);

            pointer.Dispose();
        }

        /// <summary>
        /// Generates a new <see cref="byte"/>.
        /// </summary>
        public readonly byte NextByte()
        {
            unchecked
            {
                return (byte)NextUInt();
            }
        }

        /// <summary>
        /// Generates a new signed <see cref="sbyte"/>.
        /// </summary>
        public readonly sbyte NextSByte()
        {
            unchecked
            {
                return (sbyte)NextUInt();
            }
        }

        /// <summary>
        /// Generates a new unsigned <see cref="ulong"/>.
        /// </summary>
        public readonly ulong NextULong()
        {
            ulong t = *(ulong*)pointer;
            t ^= t << 13;
            t ^= t >> 7;
            t ^= t << 17;
            *(ulong*)pointer = t;
            return t;
        }

        /// <summary>
        /// Generates a new unsigned <see cref="uint"/>.
        /// </summary>
        public readonly uint NextUInt()
        {
            uint t = *(uint*)pointer;
            t ^= t << 13;
            t ^= t >> 17;
            t ^= t << 5;
            *(uint*)pointer = t;
            return t;
        }

        /// <summary>
        /// Generates a new <see cref="bool"/> value.
        /// </summary>
        public readonly bool NextBool()
        {
            return NextUInt() % 2 == 0;
        }

        /// <summary>
        /// Generates a new unsigned <see cref="ulong"/> value between 0 and <paramref name="maxExclusive"/>.
        /// </summary>
        public readonly ulong NextULong(ulong maxExclusive)
        {
            return NextULong() % maxExclusive;
        }

        /// <summary>
        /// Generates a new unsigned <see cref="ulong"/> value between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
        /// </summary>
        public readonly ulong NextULong(ulong minInclusive, ulong maxExclusive)
        {
            ulong range = maxExclusive - minInclusive;
            ulong value = NextULong() % range;
            return value + minInclusive;
        }

        /// <summary>
        /// Generates a new <see cref="long"/>.
        /// </summary>
        public readonly long NextLong()
        {
            return (long)NextULong();
        }

        /// <summary>
        /// Generates a new <see cref="long"/> value between 0 and <paramref name="maxExclusive"/>.
        /// </summary>
        public readonly long NextLong(long maxExclusive)
        {
            return (long)(NextULong() % (ulong)maxExclusive);
        }

        /// <summary>
        /// Generates a new <see cref="long"/> value between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
        /// </summary>
        public readonly long NextLong(long minInclusive, long maxExclusive)
        {
            long range = maxExclusive - minInclusive;
            long value = (long)(NextULong() % (ulong)range);
            return value + minInclusive;
        }

        /// <summary>
        /// Generates a new <see cref="int"/>.
        /// </summary>
        public readonly int NextInt()
        {
            return (int)NextUInt();
        }

        /// <summary>
        /// Generates a new <see cref="int"/> value between 0 and <paramref name="maxExclusive"/>.
        /// </summary>
        public readonly int NextInt(int maxExclusive)
        {
            return (int)(NextULong() % (ulong)maxExclusive);
        }

        /// <summary>
        /// Generates a new <see cref="int"/> value between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
        /// </summary>
        public readonly int NextInt(int minInclusive, int maxExclusive)
        {
            int range = maxExclusive - minInclusive;
            int value = (int)(NextULong() % (uint)range);
            return value + minInclusive;
        }

        /// <summary>
        /// Generates a new unsigned <see cref="uint"/> value between 0 and <paramref name="maxExclusive"/>.
        /// </summary>
        public readonly uint NextUInt(uint maxExclusive)
        {
            return NextUInt() % maxExclusive;
        }

        /// <summary>
        /// Generates a new unsigned <see cref="uint"/> value between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
        /// </summary>
        public readonly uint NextUInt(uint minInclusive, uint maxExclusive)
        {
            uint range = maxExclusive - minInclusive;
            uint value = (NextUInt() % range);
            return value + minInclusive;
        }

        /// <summary>
        /// Generates a 0-1 unit <see cref="float"/> value.
        /// </summary>
        public readonly float NextFloat()
        {
            uint t = *(uint*)pointer;
            t ^= t << 13;
            t ^= t >> 7;
            t ^= t << 17;
            *(uint*)pointer = t;
            return t / (float)uint.MaxValue;
        }

        /// <summary>
        /// Generates a new <see cref="float"/> between 0 and <paramref name="maxExclusive"/>.
        /// </summary>
        public readonly float NextFloat(float maxExclusive)
        {
            return NextFloat() * maxExclusive;
        }

        /// <summary>
        /// Generates a new <see cref="float"/> between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
        /// </summary>
        public readonly float NextFloat(float minInclusive, float maxExclusive)
        {
            float range = maxExclusive - minInclusive;
            float value = NextFloat() * range;
            return value + minInclusive;
        }

        /// <summary>
        /// Generates a 0-1 unit <see cref="double"/> value.
        /// </summary>
        public readonly double NextDouble()
        {
            ulong t = *(ulong*)pointer;
            t ^= t << 13;
            t ^= t >> 7;
            t ^= t << 17;
            *(ulong*)pointer = t;
            return t / (double)ulong.MaxValue;
        }

        /// <summary>
        /// Generates a new <see cref="double"/> between 0 and <paramref name="maxExclusive"/>.
        /// </summary>
        public readonly double NextDouble(double maxExclusive)
        {
            return NextDouble() * maxExclusive;
        }

        /// <summary>
        /// Generates a new <see cref="double"/> between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
        /// </summary>
        public readonly double NextDouble(double minInclusive, double maxExclusive)
        {
            double range = maxExclusive - minInclusive;
            double value = NextDouble() * range;
            return value + minInclusive;
        }

        /// <summary>
        /// Fills the given span buffer with random bytes.
        /// </summary>
        public readonly void NextBytes(Span<byte> bytes)
        {
            ulong* t = (ulong*)pointer;
            ulong value = *t;
            for (int i = 0; i < bytes.Length; i++)
            {
                value ^= value >> 13;
                value ^= value << 7;
                value ^= value >> 17;
                bytes[i] = (byte)value;
            }

            *t = value;
        }

        /// <summary>
        /// Generates a random seed based on the current time and
        /// some machine specific data (process ID, memory addresses).
        /// </summary>
        public static ulong GetRandomSeed()
        {
            unchecked
            {
                DateTime now = DateTime.UtcNow;
                long ticks = now.Ticks;
#if NET
                int pid = Environment.ProcessId;
#else
                int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
#endif
                ulong baseSeed = (ulong)pid * (ulong)ticks + counter++;
                baseSeed ^= baseSeed >> 13;
                baseSeed ^= baseSeed << 3;
                baseSeed ^= baseSeed >> 27;
                return baseSeed;
            }
        }

        /// <summary>
        /// Creates a new random generator initialized with a random seed.
        /// </summary>
        public static RandomGenerator Create()
        {
            ulong seed = GetRandomSeed();
            return new RandomGenerator(seed);
        }
    }
}