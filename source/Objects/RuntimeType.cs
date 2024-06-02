using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// Represents a <see cref="System.Type"/> object that can be stored inside unmanaged values.
    /// <para>
    /// Deterministically tied to the full name of the type.
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public readonly struct RuntimeType : IEquatable<RuntimeType>
    {
        /// <summary>
        /// The maximum allowed size of the type.
        /// </summary>
        public const ushort MaxSize = 4095;

        public readonly uint value;

        /// <summary>
        /// Size of the type.
        /// </summary>
        public readonly ushort Size
        {
            get
            {
                //last 12 bits
                return (ushort)(value & 0xFFF);
            }
        }

        /// <summary>
        /// The <see cref="System.Type"/> that this instance was created from.
        /// </summary>
        public readonly Type Type => TypeTable.types[value];

        public RuntimeType(uint value)
        {
            this.value = value;
        }

        /// <returns>The <see cref="Type.ToString"/> result.</returns>
        public readonly override string ToString()
        {
            return Type.ToString();
        }

        public readonly override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is RuntimeType type && Equals(type);
        }

        public readonly bool Equals(RuntimeType other)
        {
            return value == other.value;
        }

        public readonly bool Is<T>() where T : unmanaged
        {
            return RuntimeTypeHash<T>.value == value;
        }

        /// <summary>
        /// Gets a deterministic <see cref="RuntimeType"/> instance for the
        /// given type <typeparamref name="T"/>.
        /// </summary>
        public unsafe static RuntimeType Get<T>() where T : unmanaged
        {
            if (sizeof(T) > MaxSize)
            {
                throw new InvalidOperationException($"The type {typeof(T)} is too large to be used as a RuntimeType.");
            }

            uint value = RuntimeTypeHash<T>.value;
            return new(value);
        }

        /// <summary>
        /// Retrieves a hash of the given types regardless of order.
        /// </summary>
        public static uint CalculateHash(ReadOnlySpan<RuntimeType> types)
        {
            int typeCount = types.Length;
            Span<RuntimeType> typesSpan = stackalloc RuntimeType[typeCount];
            types.CopyTo(typesSpan);
            uint hash = 0;
            while (typeCount > 0)
            {
                uint max = 0;
                int index = -1;
                for (int i = 0; i < typeCount; i++)
                {
                    RuntimeType type = typesSpan[i];
                    if (type.value > max)
                    {
                        max = type.value;
                        index = i;
                    }
                }

                unchecked
                {
                    hash += max * 174440041u;
                }

                RuntimeType last = typesSpan[typeCount - 1];
                typesSpan[index] = last;
                typeCount--;
            }

            return hash;
        }

        public static bool operator ==(RuntimeType left, RuntimeType right) => left.Equals(right);
        public static bool operator !=(RuntimeType left, RuntimeType right) => !left.Equals(right);
        public static bool operator ==(RuntimeType left, Type right) => left.Type == right;
        public static bool operator !=(RuntimeType left, Type right) => left.Type != right;
        public static bool operator ==(Type left, RuntimeType right) => left == right.Type;
        public static bool operator !=(Type left, RuntimeType right) => left != right.Type;
        public static implicit operator Type(RuntimeType type) => type.Type;

        private static class RuntimeTypeHash<T> where T : unmanaged
        {
            internal static readonly uint value;

            unsafe static RuntimeTypeHash()
            {
                unchecked
                {
                    Type type = typeof(T);
                    uint size = (uint)sizeof(T);
                    byte attempt = 1;
                    while (true)
                    {
                        value = CalculateHash(type, attempt);
                        attempt++;

                        //replace last 12 bits with type length
                        value &= 0xFFFFF000;
                        value |= (size & 0xFFF);

                        if (!TypeTable.typeHashes.Contains(value))
                        {
                            break;
                        }
#if TEST
                        Console.WriteLine($"Collision hash detected between {type} and {TypeTable.types[value]}");
#else
                        Debug.WriteLine($"Collision hash detected between {type} and {TypeTable.types[value]}");
#endif
                    }

                    TypeTable.typeHashes.Add(value);
                    TypeTable.types.Add(value, type);
                }
            }

            private static uint CalculateHash(Type type, byte attempt)
            {
                unchecked
                {
                    ReadOnlySpan<char> aqn = type.AssemblyQualifiedName.AsSpan();
                    uint salt = 174440041u * attempt;
                    uint hash = 0;
                    for (int i = 0; i < aqn.Length; i++)
                    {
                        char c = aqn[i];
                        hash = (hash << 5) - hash + (c * salt);
                    }

                    return hash;
                }
            }
        }

        private static class TypeTable
        {
            internal static readonly List<uint> typeHashes = [];
            internal static readonly Dictionary<uint, Type> types = [];
        }
    }
}
