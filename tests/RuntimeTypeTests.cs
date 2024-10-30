using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Unmanaged
{
    public class RuntimeTypeTests : UnmanagedTests
    {
        [Test]
        public void CheckEquality()
        {
            RuntimeType a = RuntimeType.Get<int>();
            RuntimeType b = RuntimeType.Get<int>();
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public unsafe void TypeIsTooBig()
        {
            RuntimeType a = RuntimeType.Get<BigType>();
            Console.WriteLine(a);
            Assert.That(a.Size, Is.EqualTo(sizeof(BigType)));
            Assert.Throws<InvalidOperationException>(() =>
            {
                RuntimeType b = RuntimeType.Get<TooBigType>();
                Console.WriteLine(b);
                Assert.That(a.Size, Is.EqualTo(sizeof(TooBigType)));
            });
        }

        public unsafe struct BigType
        {
            private fixed byte data[RuntimeType.MaxSize];
        }

        public unsafe struct TooBigType
        {
            private fixed byte data[RuntimeType.MaxSize + 1];
        }

        [Test]
        public void CheckPrimitives()
        {
            RuntimeType byteType = RuntimeType.Get<byte>();
            RuntimeType sbyteType = RuntimeType.Get<sbyte>();
            RuntimeType shortType = RuntimeType.Get<short>();
            RuntimeType ushortType = RuntimeType.Get<ushort>();
            RuntimeType intType = RuntimeType.Get<int>();
            RuntimeType uintType = RuntimeType.Get<uint>();
            RuntimeType longType = RuntimeType.Get<long>();
            RuntimeType ulongType = RuntimeType.Get<ulong>();
            RuntimeType floatType = RuntimeType.Get<float>();
            RuntimeType doubleType = RuntimeType.Get<double>();
            RuntimeType boolType = RuntimeType.Get<bool>();
            RuntimeType charType = RuntimeType.Get<char>();
            RuntimeType type = RuntimeType.Get<RuntimeType>();
            StringBuilder s = new();
            s.AppendLine(byteType.value.ToString());
            s.AppendLine(sbyteType.value.ToString());
            s.AppendLine(shortType.value.ToString());
            s.AppendLine(ushortType.value.ToString());
            s.AppendLine(intType.value.ToString());
            s.AppendLine(uintType.value.ToString());
            s.AppendLine(longType.value.ToString());
            s.AppendLine(ulongType.value.ToString());
            s.AppendLine(floatType.value.ToString());
            s.AppendLine(doubleType.value.ToString());
            s.AppendLine(boolType.value.ToString());
            s.AppendLine(charType.value.ToString());
            s.AppendLine(type.value.ToString());
            string result = s.ToString();

            List<uint> values = new();
            values.Add(RuntimeType.Byte);
            values.Add(RuntimeType.SByte);
            values.Add(RuntimeType.Short); //short
            values.Add(RuntimeType.UShort);
            values.Add(RuntimeType.Int); //int
            values.Add(RuntimeType.UInt);
            values.Add(RuntimeType.Long); //long
            values.Add(RuntimeType.ULong);
            values.Add(RuntimeType.Float);
            values.Add(RuntimeType.Double); //double
            values.Add(RuntimeType.Bool);
            values.Add(RuntimeType.Char);
            values.Add(RuntimeType.Identity);

            StringBuilder s2 = new();
            foreach (var value in values)
            {
                s2.AppendLine(value.ToString());
            }

            string result2 = s2.ToString();
            Assert.That(result, Is.EqualTo(result2));
        }

        [Test]
        public void CheckDuplicatesFrequency()
        {
            TextWriter previousOutput = Console.Out;
            StringWriter output = new();
            Console.SetOut(output);
            List<RuntimeType> types = new();
            MethodInfo getMethod = typeof(RuntimeType).GetMethod("Get") ?? throw new Exception("Get method not found");
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string? assemblyName = assembly.GetName()?.Name;
                if (assemblyName == "Unmanaged" || assemblyName == "System.Runtime" || assemblyName == "System.Private.CoreLib")
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.FullName?.Contains("<PrivateImplementationDetails>") == true) continue;

                        if (IsUnmanagedType(type))
                        {
                            MethodInfo genericMethod = getMethod.MakeGenericMethod(type);
                            RuntimeType runtimeType = (RuntimeType)(genericMethod.Invoke(null, null) ?? throw new Exception("Invalid type"));
                            types.Add(runtimeType);
                        }
                    }
                }
            }

            string outputStr = output.ToString();
            int collisions = outputStr.Count(c => c == '\n') - 1;
            float percentage = collisions / (float)types.Count;
            if (collisions == 0 || outputStr.Length == 0)
            {
                percentage = 0f;
            }

            Console.SetOut(previousOutput);
            Console.WriteLine($"Total types: {types.Count}, collision %: {percentage * 100f}%");

            static bool IsUnmanagedType(Type type)
            {
                if (type == typeof(void))
                {
                    return false;
                }

                if (!type.IsValueType || type.IsGenericType || type.IsByRef || type.IsByRefLike)
                {
                    return false;
                }

                if (type.IsPrimitive)
                {
                    return true;
                }

                Stack<Type> stack = new();
                stack.Push(type);
                while (stack.Count > 0)
                {
                    Type current = stack.Pop();
                    FieldInfo[] fields = current.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (FieldInfo field in fields)
                    {
                        if (!field.FieldType.IsValueType || type.IsGenericType || type.IsByRef || type.IsByRefLike)
                        {
                            return false;
                        }

                        if (!field.FieldType.IsPrimitive)
                        {
                            stack.Push(field.FieldType);
                        }
                    }
                }

                return true;
            }
        }

        [Test]
        public void TypeAsNumValue()
        {
            RuntimeType a = RuntimeType.Get<bool>();
            uint aRaw = a.value;
            RuntimeType b = new(aRaw);
            Assert.That(a.Is<bool>(), Is.True);
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void CompareAgainstSystemType()
        {
            RuntimeType a = RuntimeType.Get<Guid>();
            Assert.That(a.value == RuntimeType.CalculateHash(typeof(Guid)), Is.True);
        }

        [Test]
        public void CompareCombinedHashes()
        {
            RuntimeType a = RuntimeType.Get<Guid>();
            RuntimeType b = RuntimeType.Get<Vector3>();
            RuntimeType c = RuntimeType.Get<float>();
            int hash = RuntimeType.CombineHash([a, b, c]);
            int otherHash = RuntimeType.CombineHash([c, a, b]);
            Assert.That(hash, Is.EqualTo(otherHash));
        }

        [Test]
        public void CheckIfTypeIsUnmanaged()
        {
            Assert.That(RuntimeType.IsUnmanaged(typeof(DateTime), out uint size), Is.True);
            Assert.That(size, Is.EqualTo(8));

            Assert.That(RuntimeType.IsUnmanaged(typeof(DeepType), out uint deepSize), Is.True);
            Assert.That(deepSize, Is.EqualTo(40));

            Assert.That(RuntimeType.IsUnmanaged(typeof(Color), out uint colorSize), Is.False); //<--- actually insane...
        }

        [Test]
        public void CalculateTypeSize()
        {
            RuntimeType first = new(RuntimeType.CalculateHash(typeof(DateTime)));
            Assert.That(first.Size, Is.EqualTo(8));

            RuntimeType second = new(RuntimeType.CalculateHash(typeof(Guid)));
            Assert.That(second.Size, Is.EqualTo(16));

            RuntimeType third = new(RuntimeType.CalculateHash(typeof(Vector3)));
            Assert.That(third.Size, Is.EqualTo(12));
        }

        [Test]
        public void ManagedType()
        {
            Assert.That(RuntimeType.IsUnmanaged(typeof(string), out uint size), Is.False);

            Assert.Throws<InvalidOperationException>(() =>
            {
                RuntimeType bad = new(RuntimeType.CalculateHash(typeof(string)));
                Assert.That(bad.Size, Is.EqualTo(0));
            });
        }

        public readonly struct DeepType
        {
            public readonly Container type;
        }

        public readonly struct Container
        {
            public readonly Identifier identifier;
            public readonly Guid guid;
            public readonly Vector3 position;
        }

        public readonly struct Identifier
        {
            public readonly Vector3 position;
        }
    }
}
