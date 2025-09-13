using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

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
        public RandomGenerator(ReadOnlySpan<byte> seed)
        {
            long hash = 17;
            for (int i = 0; i < seed.Length; i++)
            {
                hash = hash * 31 + seed[i];
            }

            pointer = MemoryAddress.AllocateValue((ulong)hash);
        }

        /// <summary>
        /// Creates a new disposable randomness generator using the given
        /// text input as the initialization seed.
        /// </summary>
        public RandomGenerator(ReadOnlySpan<char> seed)
        {
            long hash = seed.GetLongHashCode();
            pointer = MemoryAddress.AllocateValue((ulong)hash);
        }

        /// <summary>
        /// Creates a new disposable randomness generator using the given
        /// text input as the initialization seed.
        /// </summary>
        public RandomGenerator(ASCIIText256 seed)
        {
            long hash = seed.GetLongHashCode();
            pointer = MemoryAddress.AllocateValue((ulong)hash);
        }

        /// <summary>
        /// Creates a new disposable randomness generator using the given
        /// text input as the initialization seed.
        /// </summary>
        public RandomGenerator(string seed)
        {
            long hash = seed.GetLongHashCode();
            pointer = MemoryAddress.AllocateValue((ulong)hash);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly byte NextByte()
        {
            return (byte)NextUInt();
        }

        /// <summary>
        /// Generates a new signed <see cref="sbyte"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly sbyte NextSByte()
        {
            return (sbyte)NextUInt();
        }

        /// <summary>
        /// Generates a new unsigned <see cref="ulong"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ulong NextULong()
        {
            ulong* p = (ulong*)pointer;
            ulong t = *p;
            t ^= t << 13;
            t ^= t >> 7;
            t ^= t << 17;
            *p = t;
            return t;
        }

        /// <summary>
        /// Generates a new unsigned <see cref="uint"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint NextUInt()
        {
            uint* p = (uint*)pointer;
            uint t = *p;
            t ^= t << 13;
            t ^= t >> 17;
            t ^= t << 5;
            *p = t;
            return t;
        }

        /// <summary>
        /// Generates a new <see cref="bool"/> value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool NextBool()
        {
            return (NextUInt() & 1) == 1;
        }

        /// <summary>
        /// Generates a new unsigned <see cref="ulong"/> value between 0 and <paramref name="maxExclusive"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ulong NextULong(ulong maxExclusive)
        {
            if (BitOperations.IsPow2(maxExclusive))
            {
                return NextUInt() & (maxExclusive - 1);
            }

            ulong x = NextUInt();
            UInt128 m = (UInt128)x * maxExclusive;
            return (ulong)(m >> 64);
        }

        /// <summary>
        /// Generates a new unsigned <see cref="ulong"/> value between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ulong NextULong(ulong minInclusive, ulong maxExclusive)
        {
            ulong range = maxExclusive - minInclusive;
            return NextULong(range) + minInclusive;
        }

        /// <summary>
        /// Generates a new <see cref="long"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly long NextLong()
        {
            return (long)NextULong();
        }

        /// <summary>
        /// Generates a new <see cref="long"/> value between 0 and <paramref name="maxExclusive"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly long NextLong(long maxExclusive)
        {
            return (long)NextULong((ulong)maxExclusive);
        }

        /// <summary>
        /// Generates a new <see cref="long"/> value between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly long NextLong(long minInclusive, long maxExclusive)
        {
            long range = maxExclusive - minInclusive;
            return (long)NextULong((ulong)range) + minInclusive;
        }

        /// <summary>
        /// Generates a new <see cref="int"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int NextInt()
        {
            return (int)NextUInt();
        }

        /// <summary>
        /// Generates a new <see cref="int"/> value between 0 and <paramref name="maxExclusive"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int NextInt(int maxExclusive)
        {
            return (int)NextUInt((uint)maxExclusive);
        }

        /// <summary>
        /// Generates a new <see cref="int"/> value between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int NextInt(int minInclusive, int maxExclusive)
        {
            int range = maxExclusive - minInclusive;
            return (int)NextUInt((uint)range) + minInclusive;
        }

        /// <summary>
        /// Generates a new unsigned <see cref="uint"/> value between 0 and <paramref name="maxExclusive"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint NextUInt(uint maxExclusive)
        {
            if (BitOperations.IsPow2(maxExclusive))
            {
                return NextUInt() & (maxExclusive - 1);
            }

            uint x = NextUInt();
            ulong m = (ulong)x * maxExclusive;
            return (uint)(m >> 32);
        }

        /// <summary>
        /// Generates a new unsigned <see cref="uint"/> value between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint NextUInt(uint minInclusive, uint maxExclusive)
        {
            uint range = maxExclusive - minInclusive;
            return NextUInt(range) + minInclusive;
        }

        /// <summary>
        /// Generates a 0-1 unit <see cref="float"/> value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float NextFloat()
        {
            uint bits = NextUInt() & 0x007FFFFF;
            bits |= 0x3F800000;
            return BitConverter.UInt32BitsToSingle(bits) - 1.0f;
        }

        /// <summary>
        /// Generates a new <see cref="float"/> between 0 and <paramref name="maxExclusive"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float NextFloat(float maxExclusive)
        {
            return NextFloat() * maxExclusive;
        }

        /// <summary>
        /// Generates a new <see cref="float"/> between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float NextFloat(float minInclusive, float maxExclusive)
        {
            float range = maxExclusive - minInclusive;
            return NextFloat() * range + minInclusive;
        }

        /// <summary>
        /// Generates a 0-1 unit <see cref="double"/> value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly double NextDouble()
        {
            ulong bits = NextULong() & 0x000FFFFFFFFFFFFF;
            bits |= 0x3FF0000000000000;
            return BitConverter.UInt64BitsToDouble(bits) - 1.0;
        }

        /// <summary>
        /// Generates a new <see cref="double"/> between 0 and <paramref name="maxExclusive"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly double NextDouble(double maxExclusive)
        {
            return NextDouble() * maxExclusive;
        }

        /// <summary>
        /// Generates a new <see cref="double"/> between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly double NextDouble(double minInclusive, double maxExclusive)
        {
            double range = maxExclusive - minInclusive;
            return NextDouble() * range + minInclusive;
        }

        /// <summary>
        /// Fills the given span buffer with random bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.NoInlining)]
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