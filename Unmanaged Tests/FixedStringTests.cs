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
            Assert.That(a, Is.EqualTo(new FixedString(b)));
        }

        [Test]
        public void HashEquality()
        {
            FixedString a = "Hello World!";
            string b = "Hello World!";
            Assert.That(a.GetHashCode(), Is.EqualTo(Djb2Hash.GetDjb2HashCode(b)));
        }

        [Test]
        public void CheckLengths()
        {
            FixedString a = "Hello World!";
            Assert.That(a.Length, Is.EqualTo(12));

            FixedString b = "qwertyuiopasdfghjklzxcvbnm";
            Assert.That(b.Length, Is.EqualTo(26));
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
        public void AccessOutOfRangeError()
        {
            FixedString a = "abcd";
            Assert.Throws<IndexOutOfRangeException>(() => { a[5] = 'e'; });
        }

        [Test]
        public void ModifyStringManually()
        {
            FixedString a = "abcd";
            a.Length *= 2;
            a[4] = 'e';
            a[5] = 'f';
            a[6] = 'g';
            a[7] = 'h';
            Assert.That(a.Length, Is.EqualTo(8));
            Assert.That(a, Is.EqualTo("abcdefgh"));
        }

        [Test]
        public void UseAddOperator()
        {
            FixedString a = "Hello";
            FixedString b = " World!";
            FixedString c = a + b;
            Assert.That(c.ToString(), Is.EqualTo("Hello World!"));
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
        public void Contains()
        {
            FixedString a = default;
            a.Append("Hello");

            Assert.That(a.Contains("ll"), Is.True);
            Assert.That(a.Contains('o'), Is.True);
        }

        [Test]
        public void Substring()
        {
            FixedString a = "Hello World!";
            FixedString b = a.Substring(6);
            Assert.That(b.ToString(), Is.EqualTo("World!"));
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

        [Test]
        public void CreatingFromUTF8Bytes()
        {
            string a = "Hello World!";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(a);
            FixedString b = FixedString.FromUTF8Bytes(bytes);
            Assert.That(b.ToString(), Is.EqualTo(a));
        }
    }
}
