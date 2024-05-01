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
