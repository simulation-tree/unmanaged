using System;
using System.Collections.Generic;
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
        /// Size of the type.
        /// </summary>
        public readonly ushort size;

        private readonly ushort hash;

        /// <summary>
        /// The <see cref="System.Type"/> that this instance was created from.
        /// </summary>
        public readonly Type Type => TypeTable.types[hash];

        private RuntimeType(ushort id, ushort size)
        {
            this.hash = id;
            this.size = size;
        }

        /// <summary>
        /// Creates an instance from a raw number value that was
        /// retrieved using <see cref="AsRawValue"/>
        /// </summary>
        public RuntimeType(uint rawValue)
        {
            unchecked
            {
                hash = (ushort)rawValue;
                size = (ushort)(rawValue >> 16);
            }
        }

        /// <returns>The <see cref="Type.ToString"/> result.</returns>
        public readonly override string ToString()
        {
            return Type.ToString();
        }

        /// <returns>A number value that can represent this instance.</returns>
        public readonly uint AsRawValue()
        {
            unchecked
            {
                uint rawValue = default;
                rawValue |= hash;
                rawValue |= (uint)size << 16;
                return rawValue;
            }
        }

        public readonly override int GetHashCode()
        {
            return hash.GetHashCode();
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is RuntimeType type && Equals(type);
        }

        public readonly bool Equals(RuntimeType other)
        {
            return hash == other.hash;
        }

        public readonly bool Is<T>() where T : unmanaged
        {
            return RuntimeTypeHash<T>.value == hash;
        }

        /// <summary>
        /// Gets a deterministic <see cref="RuntimeType"/> instance for the
        /// given type <typeparamref name="T"/>.
        /// </summary>
        public unsafe static RuntimeType Get<T>() where T : unmanaged
        {
            ushort id = RuntimeTypeHash<T>.value;
            return new(id, (ushort)sizeof(T));
        }

        public static bool operator ==(RuntimeType left, RuntimeType right) => left.Equals(right);
        public static bool operator !=(RuntimeType left, RuntimeType right) => !left.Equals(right);
        public static bool operator ==(RuntimeType left, Type right) => left.Type == right;
        public static bool operator !=(RuntimeType left, Type right) => left.Type != right;

        private static class RuntimeTypeHash<T>
        {
            internal static readonly ushort value;

            unsafe static RuntimeTypeHash()
            {
                unchecked
                {
                    Type type = typeof(T);
                    int hash = GetHashCode(type);
                    while (TypeTable.typeIds.Contains((ushort)hash))
                    {
                        hash += 174440041;
                        Console.WriteLine($"Collision detected for {type}");
                    }

                    value = (ushort)hash;
                    TypeTable.typeIds.Add(value);
                    TypeTable.types.Add(value, type);
                }
            }

            private static int GetHashCode(Type type)
            {
                ReadOnlySpan<char> aqn = type.AssemblyQualifiedName.AsSpan();
                int hash = 0;
                for (int i = 0; i < aqn.Length; i++)
                {
                    char c = aqn[i];
                    hash = (hash << 5) - hash + (c * 174440041);
                }

                return hash;
            }
        }

        private static class TypeTable
        {
            internal static readonly HashSet<ushort> typeIds = [];
            internal static readonly Dictionary<ushort, Type> types = [];
        }
    }
}
