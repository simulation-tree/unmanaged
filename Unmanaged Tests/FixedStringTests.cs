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

        [Test]
        public void Appending()
        {
            FixedString a = default;
            a.Append("Hello");
            a.Append(' ');
            a.Append("World!");
            Assert.That(a.ToString(), Is.EqualTo("Hello World!"));
        }

        [Test]
        public void Indexing()
        {
            FixedString a = default;
            a.Append("Hello");

            Assert.That(a.IndexOf('e'), Is.EqualTo(1));
            Assert.That(a.IndexOf("lo"), Is.EqualTo(3));
            Assert.That(a.IndexOf(' '), Is.EqualTo(-1));
        }

        [Test]
        public void HittingTheLimit()
        {
            FixedString a = default;
            for (int i = 0; i < FixedString.MaxLength; i++)
            {
                a.Append('x');
            }

            Assert.Throws<InvalidOperationException>(() => a.Append('o'));
        }
    }
}
