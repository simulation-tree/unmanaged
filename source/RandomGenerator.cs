﻿using System;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    public unsafe struct RandomGenerator : IDisposable
    {
        private void* pointer;

        public readonly ulong State => *(ulong*)pointer;

#if NET
        /// <summary>
        /// Creates a new random generator initialized with a random seed.
        /// </summary>
        public RandomGenerator()
        {
            pointer = Allocations.Allocate(sizeof(ulong));
            ulong* t = (ulong*)pointer;
            *t = GetRandomSeed();
        }
#endif
        /// <summary>
        /// Creates a new disposable randomness generator.
        /// </summary>
        public RandomGenerator(ulong seed)
        {
            pointer = Allocations.Allocate(sizeof(ulong));
            ulong* t = (ulong*)pointer;
            *t = seed;
        }

        /// <summary>
        /// Creates a new disposable randomness generator using the given byte
        /// sequence as the initialization seed.
        /// </summary>
        public RandomGenerator(USpan<byte> seed)
        {
            unchecked
            {
                long hash = 17;
                for (uint i = 0; i < seed.Length; i++)
                {
                    hash = hash * 31 + seed[i];
                }

                pointer = Allocations.Allocate(sizeof(ulong));
                ulong* t = (ulong*)pointer;
                *t = (ulong)hash;
            }
        }

        /// <summary>
        /// Creates a new disposable randomness generator using the given
        /// text input as the initialization seed.
        /// </summary>
        public RandomGenerator(USpan<char> seed)
        {
            unchecked
            {
                long hash = 17;
                for (uint i = 0; i < seed.Length; i++)
                {
                    hash = hash * 31 + seed[i];
                }

                pointer = Allocations.Allocate(sizeof(ulong));
                ulong* t = (ulong*)pointer;
                *t = (ulong)hash;
            }
        }

        public RandomGenerator(string seed) : this(seed.AsSpan())
        {
        }

        public void Dispose()
        {
            Allocations.ThrowIfNull(pointer);
            Allocations.Free(ref pointer);
        }

        public readonly byte NextByte()
        {
            unchecked
            {
                return (byte)NextUInt();
            }
        }

        public readonly sbyte NextSByte()
        {
            unchecked
            {
                return (sbyte)NextUInt();
            }
        }

        public readonly ulong NextULong()
        {
            ulong t = *(ulong*)pointer;
            t ^= t << 13;
            t ^= t >> 7;
            t ^= t << 17;
            *(ulong*)pointer = t;
            return t;
        }

        public readonly uint NextUInt()
        {
            uint t = *(uint*)pointer;
            t ^= t << 13;
            t ^= t >> 17;
            t ^= t << 5;
            *(uint*)pointer = t;
            return t;
        }

        public readonly bool NextBool()
        {
            return NextUInt() % 2 == 0;
        }

        public readonly ulong NextULong(ulong maxExclusive)
        {
            return NextULong() % maxExclusive;
        }

        public readonly ulong NextULong(ulong minInclusive, ulong maxExclusive)
        {
            ulong range = maxExclusive - minInclusive;
            ulong value = NextULong() % range;
            return value + minInclusive;
        }

        public readonly long NextLong()
        {
            return (long)NextULong();
        }

        public readonly long NextLong(long maxExclusive)
        {
            return (long)(NextULong() % (ulong)maxExclusive);
        }

        public readonly long NextLong(long minInclusive, long maxExclusive)
        {
            long range = maxExclusive - minInclusive;
            long value = (long)(NextULong() % (ulong)range);
            return value + minInclusive;
        }

        public readonly int NextInt()
        {
            return (int)NextUInt();
        }

        public readonly int NextInt(int maxExclusive)
        {
            return (int)(NextULong() % (ulong)maxExclusive);
        }

        public readonly int NextInt(int minInclusive, int maxExclusive)
        {
            int range = maxExclusive - minInclusive;
            int value = (int)(NextULong() % (uint)range);
            return value + minInclusive;
        }

        public readonly uint NextUInt(uint maxExclusive)
        {
            return NextUInt() % maxExclusive;
        }

        public readonly uint NextUInt(uint minInclusive, uint maxExclusive)
        {
            uint range = maxExclusive - minInclusive;
            uint value = (NextUInt() % range);
            return value + minInclusive;
        }

        /// <summary>
        /// Generates a 0-1 unit value.
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

        public readonly float NextFloat(float maxExclusive)
        {
            return NextFloat() * maxExclusive;
        }

        public readonly float NextFloat(float minInclusive, float maxExclusive)
        {
            float range = maxExclusive - minInclusive;
            float value = NextFloat() * range;
            return value + minInclusive;
        }

        public readonly double NextDouble()
        {
            ulong t = *(ulong*)pointer;
            t ^= t << 13;
            t ^= t >> 7;
            t ^= t << 17;
            *(ulong*)pointer = t;
            return t / (double)ulong.MaxValue;
        }

        public readonly double NextDouble(double maxExclusive)
        {
            return NextDouble() * maxExclusive;
        }

        public readonly double NextDouble(double minInclusive, double maxExclusive)
        {
            double range = maxExclusive - minInclusive;
            double value = NextDouble() * range;
            return value + minInclusive;
        }

        /// <summary>
        /// Fills the given span with random bytes.
        /// </summary>
        /// <param name="bytes"></param>
        public readonly void NextBytes(USpan<byte> bytes)
        {
            ulong* t = (ulong*)pointer;
            ulong value = *t;
            for (uint i = 0; i < bytes.Length; i++)
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

#if NET5_0_OR_GREATER
                int pid = Environment.ProcessId;
#else
                int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
#endif

                ulong baseSeed = (ulong)pid * (ulong)ticks;
                baseSeed ^= baseSeed >> 13;
                baseSeed ^= baseSeed << 3;
                baseSeed ^= baseSeed >> 27;
                void* tempAlloc = NativeMemory.Alloc((uint)((pid + ticks) % 3 + 1));
                ulong tempAddress = (ulong)tempAlloc;
                NativeMemory.Free(tempAlloc);
                tempAddress *= (ulong)Environment.TickCount;
                baseSeed *= tempAddress - baseSeed * 2;
                return baseSeed;
            }
        }

        public static RandomGenerator Create()
        {
            ulong seed = GetRandomSeed();
            return new RandomGenerator(seed);
        }
    }
}
