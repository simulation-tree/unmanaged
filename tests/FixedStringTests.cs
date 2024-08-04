using System;
using System.Text;
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
            Assert.That(a.Equals(b), Is.True);
        }

        [Test]
        public void HashEquality()
        {
            FixedString a = "Hello World!";
            string b = "Hello World!";
            Assert.That(a.GetHashCode(), Is.EqualTo(Djb2Hash.Get(b)));
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
        public void Clearing()
        {
            FixedString a = "once upon a time";
            a.Clear();

            Assert.That(a.Length, Is.EqualTo(0));
            Assert.That(a.ToString(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void RemovingAt()
        {
            FixedString a = "Hello World";
            Assert.That(a.Length, Is.EqualTo(11));
            a.RemoveAt(a.Length - 1);

            Assert.That(a.Contains('d'), Is.False);
            Assert.That(a.Length, Is.EqualTo(10));

            a.RemoveAt(4);
            Assert.That(a.ToString(), Is.EqualTo("Hell Worl"));
        }

        [Test]
        public void CopyToUTF8Bytes()
        {
            FixedString a = "abacus123+•◘○♠♣♦☺☻♥☺☻";
            Span<byte> bytes = stackalloc byte[32];
            int byteLength = a.CopyTo(bytes);
            byte[] realBytes = Encoding.UTF8.GetBytes(a.ToString());
            Assert.That(byteLength, Is.EqualTo(realBytes.Length));
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
        public void ModifyingTextLength()
        {
            FixedString a = "abacus";
            a.Length = 4;
            Assert.That(a.ToString(), Is.EqualTo("abac"));
            a.Length = 8;
            Assert.That(a.ToString(), Is.EqualTo("abac    "));
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
            FixedString a = new("Hello");

            Assert.That(a.IndexOf('e'), Is.EqualTo(1));
            Assert.That(a.IndexOf('l'), Is.EqualTo(2));
            Assert.That(a.LastIndexOf('l'), Is.EqualTo(3));
            Assert.That(a.IndexOf("lo"), Is.EqualTo(3));
            Assert.That(a.IndexOf("ll"), Is.EqualTo(2));
            Assert.That(a.IndexOf(' '), Is.EqualTo(-1));
        }

        [Test]
        public void Insert()
        {
            FixedString a = default;
            a.Insert(0, "World");
            a.Insert(0, ' ');
            a.Insert(0, "Hello");
            Assert.That(a.ToString(), Is.EqualTo("Hello World"));

            a.Replace("World", "Pastrami");
            Assert.That(a.ToString(), Is.EqualTo("Hello Pastrami"));
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
            FixedString b = a.Slice(6);
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
            FixedString b = new(bytes);
            Assert.That(b.ToString(), Is.EqualTo(a));
        }
    }
}
