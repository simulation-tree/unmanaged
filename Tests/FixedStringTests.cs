using Unmanaged;

namespace Tests
{
    public class FixedStringTests
    {
        [Test]
        public void CheckEquality()
        {
            FixedString a = "Hello World!";
            string b = "Hello World!";
            Assert.That(a.ToString(), Is.EqualTo(b));
        }

        [Test]
        public void HashEquality()
        {
            FixedString a = "Hello World!";
            string b = "Hello World!";
            Assert.That(a.GetHashCode(), Is.EqualTo(Djb2.GetDjb2HashCode(b)));
        }
    }
}
