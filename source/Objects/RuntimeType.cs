#pragma warning disable IL2075
#pragma warning disable IL2070
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
#if DEBUG
using System.Diagnostics;
#endif

namespace Unmanaged
{
    /// <summary>
    /// Represents a <see cref="Type"/> object that can be stored inside unmanaged values.
    /// <para>
    /// Deterministically tied to the full name of the type.
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public readonly struct RuntimeType : IEquatable<RuntimeType>
    {
        public const uint Byte = 946434049;
        public const uint SByte = 1894944769;
        public const uint Short = 123904002;
        public const uint UShort = 214994946;
        public const uint Int = 3127869444;
        public const uint UInt = 2816307204;
        public const uint Long = 1580060680;
        public const uint ULong = 1535361032;
        public const uint Float = 26202116;
        public const uint Double = 2900860936;
        public const uint Bool = 2893246465;
        public const uint Char = 582934530;
        public const uint Identity = 4199456772;

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

        public RuntimeType(uint value)
        {
            this.value = value;
        }

        public override string ToString()
        {
#if DEBUG
            if (TypeTable.types.TryGetValue(value, out Type? systemType))
            {
                return systemType?.FullName ?? value.ToString();
            }
#endif
            return value.ToString();
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
            return GenericHasher<T>.value == value;
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

            uint value = GenericHasher<T>.value;
            return new(value);
        }

        public static bool IsUnmanaged(Type type, out uint size)
        {
            if (type.IsClass)
            {
                size = default;
                return false;
            }
            else if (type == typeof(RuntimeType))
            {
                size = sizeof(uint);
                return true;
            }
            else if (type == typeof(byte))
            {
                size = sizeof(byte);
                return true;
            }
            else if (type == typeof(sbyte))
            {
                size = sizeof(sbyte);
                return true;
            }
            else if (type == typeof(short))
            {
                size = sizeof(short);
                return true;
            }
            else if (type == typeof(ushort))
            {
                size = sizeof(ushort);
                return true;
            }
            else if (type == typeof(int))
            {
                size = sizeof(int);
                return true;
            }
            else if (type == typeof(uint))
            {
                size = sizeof(uint);
                return true;
            }
            else if (type == typeof(long))
            {
                size = sizeof(long);
                return true;
            }
            else if (type == typeof(ulong))
            {
                size = sizeof(ulong);
                return true;
            }
            else if (type == typeof(float))
            {
                size = sizeof(float);
                return true;
            }
            else if (type == typeof(double))
            {
                size = sizeof(double);
                return true;
            }
            else if (type == typeof(bool))
            {
                size = sizeof(bool);
                return true;
            }
            else if (type == typeof(char))
            {
                size = sizeof(char);
                return true;
            }
            else
            {
                size = 0;
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException($"The type {type} has no fields.");
                foreach (FieldInfo field in fields)
                {
                    Type fieldType = field.FieldType;
                    if (!IsUnmanaged(fieldType, out uint fieldSize))
                    {
                        size = 0;
                        return false;
                    }

                    size += fieldSize;
                }

                return true;
            }
        }

        /// <summary>
        /// Retrieves a hash of the given types regardless of order.
        /// </summary>
        public static uint CombineHash(ReadOnlySpan<RuntimeType> types)
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

        public static uint CalculateHash(ReadOnlySpan<char> fullTypeName, ReadOnlySpan<char> assemblyName, uint size, byte attempt = 1)
        {
            uint fullNameHash = CalculateHash(fullTypeName, attempt);
            uint assemblyNameHash = CalculateHash(assemblyName, attempt);
            uint value = fullNameHash ^ assemblyNameHash;

            //replace last 12 bits with type length
            value &= 0xFFFFF000;
            value |= (size & 0xFFF);
            return value;
        }

        public static uint CalculateHash(Type type, byte attempt = 1)
        {
            uint size = 0;
            Stack<Type> types = new();
            types.Push(type);
            while (types.Count > 0)
            {
                Type current = types.Pop();
                if (current.IsClass)
                {
                    throw new InvalidOperationException($"The type {type} is not unmanaged.");
                }

                FieldInfo[] fields = current.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException($"The type {type} has no fields.");
                foreach (FieldInfo field in fields)
                {
                    Type fieldType = field.FieldType;
                    if (!IsUnmanaged(fieldType, out uint fieldSize))
                    {
                        throw new InvalidOperationException($"The type {fieldType} is not unmanaged.");
                    }

                    size += fieldSize;
                }
            }

            return CalculateHash(type.FullName.AsSpan(), type.Assembly.GetName().Name.AsSpan(), size, attempt);
        }

        private static uint CalculateHash(ReadOnlySpan<char> text, byte attempt)
        {
            unchecked
            {
                uint salt = 174440041u * attempt;
                uint hash = 0;
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    hash = (hash << 5) - hash + (c * salt);
                }

                return hash;
            }
        }

        public static bool operator ==(RuntimeType left, RuntimeType right) => left.Equals(right);
        public static bool operator !=(RuntimeType left, RuntimeType right) => !left.Equals(right);

        private static class GenericHasher<T> where T : unmanaged
        {
            internal static readonly uint value;

            unsafe static GenericHasher()
            {
                unchecked
                {
                    Type type = typeof(T);
                    uint size = (uint)sizeof(T);
                    byte attempt = 1;
                    while (true)
                    {
                        value = CalculateHash(type.FullName.AsSpan(), attempt);
                        value ^= CalculateHash(type.Assembly.GetName().Name.AsSpan(), attempt);
                        attempt++;

                        //replace last 12 bits with type length
                        value &= 0xFFFFF000;
                        value |= (size & 0xFFF);

                        if (!TypeTable.typeHashes.Contains(value))
                        {
                            break;
                        }
#if DEBUG
                        Debug.WriteLine($"Collision hash detected between {type} and {TypeTable.types[value]}");
#endif
                    }

                    TypeTable.typeHashes.Add(value);
                    TypeTable.types.Add(value, type);
                }
            }
        }

        private static class TypeTable
        {
            internal static readonly List<uint> typeHashes = new();
            internal static readonly Dictionary<uint, Type> types = new();
        }
    }
}
