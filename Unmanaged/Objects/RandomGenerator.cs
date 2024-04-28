using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Unmanaged
{
    public readonly unsafe struct RandomGenerator : IDisposable
    {
        private readonly nint pointer;

        /// <summary>
        /// Creates a new disposable randomness generator.
        /// <para>
        /// Initialized state is based on current state of the environment.
        /// </para>
        /// </summary>
        public RandomGenerator()
        {
            unchecked
            {
                long ticks = Environment.TickCount64;
                int pid = Environment.ProcessId;
                int tid = Environment.CurrentManagedThreadId;
                ulong seed = (ulong)ticks ^ (ulong)pid ^ (ulong)tid;
                int hash = seed.GetHashCode();
                hash ^= hash >> 13;
                hash ^= hash << 3;
                hash ^= hash >> 27;
                nint tempAlloc = Marshal.AllocHGlobal(((int)(pid + ticks) % 3) + 1);
                Marshal.FreeHGlobal(tempAlloc);
                tempAlloc = tempAlloc.GetHashCode() * Environment.TickCount;
                seed *= (ulong)(tempAlloc + hash);

                pointer = Marshal.AllocHGlobal(sizeof(ulong));
                Allocations.Register(pointer);
                seed += (ulong)pointer.GetHashCode();

                ulong* t = (ulong*)pointer;
                *t = seed;
            }
        }

        /// <summary>
        /// Creates a new disposable randomness generator.
        /// </summary>
        public RandomGenerator(ulong seed)
        {
            pointer = Marshal.AllocHGlobal(sizeof(ulong));
            Allocations.Register(pointer);
            ulong* t = (ulong*)pointer;
            *t = seed + (ulong)pointer.GetHashCode();
        }

        /// <summary>
        /// Creates a new disposable randomness generator using the given byte
        /// sequence as the initialization seed.
        /// </summary>
        public RandomGenerator(ReadOnlySpan<byte> seed) : this((ulong)Djb2Hash.GetDjb2HashCode(seed))
        {
        }

        /// <summary>
        /// Creates a new disposable randomness generator using the given
        /// text input as the initialization seed.
        /// </summary>
        public RandomGenerator(ReadOnlySpan<char> seed) : this((ulong)Djb2Hash.GetDjb2HashCode(seed))
        {
        }

        public readonly void Dispose()
        {
            Allocations.ThrowIfNull(pointer);
            Allocations.Unregister(pointer);
            Marshal.FreeHGlobal(pointer);
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
            ulong* t = (ulong*)pointer;
            *t ^= *t << 13;
            *t ^= *t >> 17;
            *t ^= *t << 5;
            return (uint)*t;
        }

        public readonly bool NextBool()
        {
            return NextUInt() == 1;
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
    }
}
