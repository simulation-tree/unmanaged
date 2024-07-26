using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unmanaged.Collections;

namespace Unmanaged
{
    public unsafe struct RandomGenerator : IDisposable
    {
        private void* pointer;

        public readonly ulong State => *(ulong*)pointer;

#if NET5_0_OR_GREATER
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
        public RandomGenerator(ReadOnlySpan<byte> seed) : this((ulong)Djb2Hash.Get(seed))
        {
        }

        /// <summary>
        /// Creates a new disposable randomness generator using the given
        /// text input as the initialization seed.
        /// </summary>
        public RandomGenerator(ReadOnlySpan<char> seed) : this((ulong)Djb2Hash.Get(seed))
        {
        }

        public void Dispose()
        {
            Allocations.ThrowIfNull(pointer);
            Allocations.Free(ref pointer);
        }

        public readonly byte NextByte()
        {
            uint* t = (uint*)pointer;
            *t ^= *t << 13;
            *t ^= *t >> 7;
            *t ^= *t << 17;
            return (byte)*t;
        }

        public readonly sbyte NextSByte()
        {
            return (sbyte)NextByte();
        }

        public readonly ulong NextULong()
        {
            ulong* t = (ulong*)pointer;
            *t ^= *t >> 13;
            *t ^= *t << 7;
            *t ^= *t >> 17;
            return *t;
        }

        public readonly uint NextUInt()
        {
            uint* t = (uint*)pointer;
            *t ^= *t << 13;
            *t ^= *t >> 17;
            *t ^= *t << 5;
            return *t;
        }

        public readonly bool NextBool()
        {
            uint* t = (uint*)pointer;
            *t ^= *t << 13;
            *t ^= *t >> 17;
            *t ^= *t << 5;
            return (*t & 0x8000) != 0;
        }

        public readonly ulong NextULong(ulong max)
        {
            return NextULong() % max;
        }

        public readonly ulong NextULong(ulong min, ulong max)
        {
            ulong range = max - min;
            ulong value = NextULong() % range;
            return value + min;
        }

        public readonly long NextLong()
        {
            return (long)NextULong();
        }

        public readonly long NextLong(long max)
        {
            return (long)(NextULong() % (ulong)max);
        }

        public readonly long NextLong(long min, long max)
        {
            long range = max - min;
            long value = (long)(NextULong() % (ulong)range);
            return value + min;
        }

        public readonly int NextInt()
        {
            return (int)NextUInt();
        }

        public readonly int NextInt(int max)
        {
            return (int)(NextULong() % (ulong)max);
        }

        public readonly int NextInt(int min, int max)
        {
            int range = max - min;
            int value = (int)(NextULong() % (uint)range);
            return value + min;
        }

        public readonly uint NextUInt(uint max)
        {
            return NextUInt() % max;
        }

        public readonly uint NextUInt(uint min, uint max)
        {
            uint range = max - min;
            uint value = (NextUInt() % range);
            return value + min;
        }

        /// <summary>
        /// Generates a 0-1 unit value.
        /// </summary>
        public readonly float NextFloat()
        {
            uint value = NextUInt();
            return value / (float)uint.MaxValue;
        }

        public readonly float NextFloat(float max)
        {
            return NextFloat() * max;
        }

        public readonly float NextFloat(float min, float max)
        {
            float range = max - min;
            float value = NextFloat() * range;
            return value + min;
        }

        public readonly double NextDouble()
        {
            ulong value = NextULong();
            return value / (double)ulong.MaxValue;
        }

        public readonly double NextDouble(double max)
        {
            return NextDouble() * max;
        }

        public readonly double NextDouble(double min, double max)
        {
            double range = max - min;
            double value = NextDouble() * range;
            return value + min;
        }

        /// <summary>
        /// Fills the given span with random bytes.
        /// </summary>
        /// <param name="bytes"></param>
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

        public readonly UnmanagedArray<byte> GetBytes(uint length)
        {
            UnmanagedArray<byte> list = new(length);
            NextBytes(list.AsSpan());
            return list;
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
                int pid = Process.GetCurrentProcess().Id;
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
