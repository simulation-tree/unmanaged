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
        }
    }
}
