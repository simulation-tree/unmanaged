using System;
using System.Text;

namespace Unmanaged.Tests
{
    public class FixedStringTests : UnmanagedTests
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
            USpan<byte> bytes = stackalloc byte[32];
            uint byteLength = a.CopyTo(bytes);
            byte[] realBytes = Encoding.UTF8.GetBytes(a.ToString());
            Assert.That(byteLength, Is.EqualTo(realBytes.Length));
        }

        [Test]
        public void Appending()
        {
            FixedString a = default;
            a.Append("Hello");
            Assert.That(a.ToString(), Is.EqualTo("Hello"));
            a.Append(' ');
            Assert.That(a.ToString(), Is.EqualTo("Hello "));
            a.Append("World!");
            Assert.That(a.ToString(), Is.EqualTo("Hello World!"));
        }

#if DEBUG
        [Test]
        public void ThrowIfAccessOutOfRange()
        {
            FixedString a = "abcd";
            Assert.Throws<IndexOutOfRangeException>(() => { a[5] = 'e'; });
        }

        [Test]
        public void ThrowIfGreaterThanCapacity()
        {
            FixedString a = default;
            for (uint i = 0; i < FixedString.Capacity; i++)
            {
                a.Append('x');
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => a.Append('o'));
        }
#endif

        [Test]
        public void ModifyingTextLength()
        {
            FixedString a = "abacus";
            a.Length = 4;
            Assert.That(a.Length, Is.EqualTo(4));
            Assert.That(a.ToString(), Is.EqualTo("abac"));
            Console.WriteLine(a);
            a.Length = 8;
            Assert.That(a.Length, Is.EqualTo(8));
            Assert.That(a.ToString(), Is.EqualTo("abac\0\0\0\0"));
            Console.WriteLine(a);
        }

        [Test]
        public void ModifyStringManually()
        {
            FixedString a = "abcd";
            Console.WriteLine(a);
            a.Length *= 2;
            a[4] = 'e';
            a[5] = 'f';
            a[6] = 'g';
            a[7] = 'h';
            Assert.That(a.Length, Is.EqualTo(8));
            Assert.That(a.ToString(), Is.EqualTo("abcdefgh"));
            Console.WriteLine(a);
        }

        [Test]
        public void Indexing()
        {
            FixedString a = "Hello";

            Assert.That(a.IndexOf('e'), Is.EqualTo(1));
            Assert.That(a.IndexOf('l'), Is.EqualTo(2));
            Assert.That(a.LastIndexOf('l'), Is.EqualTo(3));
            Assert.That(a.IndexOf("lo"), Is.EqualTo(3));
            Assert.That(a.IndexOf("ll"), Is.EqualTo(2));
            Assert.That(a.TryIndexOf(' ', out _), Is.EqualTo(false));
        }

        [Test]
        public void Insert()
        {
            FixedString a = default;
            a.Insert(0, "World");
            a.Insert(0, ' ');
            a.Insert(0, "Hello");
            Assert.That(a.ToString(), Is.EqualTo("Hello World"));
        }

        [Test]
        public void Replace()
        {
            FixedString a = "Hello World";
            a.TryReplace("World", "Pastrami");
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
        public void EndsAndStartsWith()
        {
            FixedString a = "Hello World!";
            Assert.That(a.StartsWith("Hello"), Is.True);
            Assert.That(a.EndsWith("World!"), Is.True);
        }

        [Test]
        public void CreatingFromUTF8Bytes()
        {
            string a = "Hello World!";
            byte[] bytes = Encoding.UTF8.GetBytes(a);
            FixedString b = new(bytes);
            Assert.That(b.ToString(), Is.EqualTo(a));
        }

        [Test]
        public void CreateFromString()
        {
            string a = "Hello World!";
            FixedString b = new(a);
            Assert.That(b.Length, Is.EqualTo(a.Length));
            Assert.That(b.ToString(), Is.EqualTo(a));
        }
    }
}
