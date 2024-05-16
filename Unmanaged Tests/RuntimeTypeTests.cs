using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unmanaged;

namespace Tests
{
    public class RuntimeTypeTests
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
            StringBuilder s = new();
            s.AppendLine(byteType.AsRawValue().ToString());
            s.AppendLine(sbyteType.AsRawValue().ToString());
            s.AppendLine(shortType.AsRawValue().ToString());
            s.AppendLine(ushortType.AsRawValue().ToString());
            s.AppendLine(intType.AsRawValue().ToString());
            s.AppendLine(uintType.AsRawValue().ToString());
            s.AppendLine(longType.AsRawValue().ToString());
            s.AppendLine(ulongType.AsRawValue().ToString());
            s.AppendLine(floatType.AsRawValue().ToString());
            s.AppendLine(doubleType.AsRawValue().ToString());
            s.AppendLine(boolType.AsRawValue().ToString());
            s.AppendLine(charType.AsRawValue().ToString());
            string result = s.ToString();

            List<uint> values = new();
            values.Add(118635);
            values.Add(77710);
            values.Add(144269); //short
            values.Add(195740);
            values.Add(283011); //int
            values.Add(268946);
            values.Add(589356); //long
            values.Add(575291);
            values.Add(265195);
            values.Add(534554); //double
            values.Add(111961);
            values.Add(158381);

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

            int collisions = output.ToString().Count(c => c == '\n') - 1;
            float percentage = collisions / (float)types.Count;
            if (collisions == 0)
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
        public void ReadSystemType()
        {
            RuntimeType a = RuntimeType.Get<uint>();
            Type b = typeof(uint);
            Assert.That(a.Type, Is.EqualTo(b));
        }

        [Test]
        public void TypeAsNumValue()
        {
            RuntimeType a = RuntimeType.Get<bool>();
            uint aRaw = a.AsRawValue();
            RuntimeType b = new(aRaw);
            Assert.That(a.Is<bool>(), Is.True);
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void CompareAgainstSystemType()
        {
            RuntimeType a = RuntimeType.Get<Guid>();
            Assert.That(a == typeof(Guid), Is.True);
        }

        [Test]
        public void CompareToString()
        {
            RuntimeType a = RuntimeType.Get<ushort>();
            Assert.That(a.ToString(), Is.EqualTo(typeof(ushort).ToString()));
        }
    }
}
