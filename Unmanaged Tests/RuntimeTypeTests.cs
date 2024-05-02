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
            values.Add(84327);
            values.Add(100252);
            values.Add(164309);
            values.Add(179070);
            values.Add(295439);
            values.Add(310200);
            values.Add(557678);
            values.Add(572439);
            values.Add(315623);
            values.Add(575600);
            values.Add(111465);
            values.Add(162741);

            StringBuilder s2 = new();
            foreach (var value in values)
            {
                s2.AppendLine(value.ToString());
            }

            string result2 = s2.ToString();
            Assert.That(result, Is.EqualTo(result2));
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
