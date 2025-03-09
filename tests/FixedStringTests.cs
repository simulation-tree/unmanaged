using System;
using System.Text;

namespace Unmanaged.Tests
{
    public class FixedStringTests : UnmanagedTests
    {
        [Test]
        public void CheckEquality()
        {
            ASCIIText256 a = "Hello World!";
            string b = "Hello World!";
            Assert.That(a.ToString(), Is.EqualTo(b));
            Assert.That(a, Is.EqualTo(new ASCIIText256(b)));
            Assert.That(a.Equals(b), Is.True);
        }

        [Test]
        public void CheckLengths()
        {
            ASCIIText256 a = "Hello World!";
            Assert.That(a.Length, Is.EqualTo(12));

            ASCIIText256 b = "qwertyuiopasdfghjklzxcvbnm";
            Assert.That(b.Length, Is.EqualTo(26));
        }

        [Test]
        public void Clearing()
        {
            ASCIIText256 a = "once upon a time";
            a.Clear();

            Assert.That(a.Length, Is.EqualTo(0));
            Assert.That(a.ToString(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void RemovingAt()
        {
            ASCIIText256 a = "Hello World";
            Assert.That(a.Length, Is.EqualTo(11));
            a.RemoveAt((byte)(a.Length - 1));

            Assert.That(a.Contains('d'), Is.False);
            Assert.That(a.Length, Is.EqualTo(10));

            a.RemoveAt(4);
            Assert.That(a.ToString(), Is.EqualTo("Hell Worl"));
        }

        [Test]
        public void CopyToUTF8Bytes()
        {
            ASCIIText256 a = "abacus123+•◘○♠♣♦☺☻♥☺☻";
            Span<byte> bytes = stackalloc byte[32];
            int byteLength = a.CopyTo(bytes);
            byte[] realBytes = Encoding.UTF8.GetBytes(a.ToString());
            Assert.That(byteLength, Is.EqualTo(realBytes.Length));

            ASCIIText256 b = "VK_LAYER_KHRONOS_validation";
            byteLength = b.CopyTo(bytes);
            string realString = Encoding.UTF8.GetString(bytes.Slice(0, byteLength));
            Assert.That(realString, Is.EqualTo(b.ToString()));
        }

        [Test]
        public void Appending()
        {
            ASCIIText256 a = default;
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
            ASCIIText256 a = "abcd";
            Assert.Throws<IndexOutOfRangeException>(() => { a[5] = 'e'; });
        }

        [Test]
        public void ThrowIfGreaterThanCapacity()
        {
            ASCIIText256 a = default;
            for (uint i = 0; i < ASCIIText256.Capacity; i++)
            {
                a.Append('x');
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => a.Append('o'));
        }
#endif

        [Test]
        public void ModifyingTextLength()
        {
            ASCIIText256 a = "abacus";
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
            ASCIIText256 a = "abcd";
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
            ASCIIText256 a = "Hello";

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
            ASCIIText256 a = default;
            a.Insert(0, "World");
            a.Insert(0, ' ');
            a.Insert(0, "Hello");
            Assert.That(a.ToString(), Is.EqualTo("Hello World"));
        }

        [Test]
        public void InsertOneCharacterToTheStart()
        {
            ASCIIText256 a = default;
            a.Insert(0, 'H');
            Assert.That(a.ToString(), Is.EqualTo("H"));
        }

        [Test]
        public void Replace()
        {
            ASCIIText256 a = "Hello World";
            a.TryReplace("World", "Pastrami");
            Assert.That(a.ToString(), Is.EqualTo("Hello Pastrami"));
        }

        [Test]
        public void Contains()
        {
            ASCIIText256 a = default;
            a.Append("Hello");

            Assert.That(a.Contains("ll"), Is.True);
            Assert.That(a.Contains('o'), Is.True);
        }

        [Test]
        public void Substring()
        {
            ASCIIText256 a = "Hello World!";
            ASCIIText256 b = a.Slice(6);
            Assert.That(b.ToString(), Is.EqualTo("World!"));
        }

        [Test]
        public void EndsAndStartsWith()
        {
            ASCIIText256 a = "Hello World!";
            Assert.That(a.StartsWith("Hello"), Is.True);
            Assert.That(a.EndsWith("World!"), Is.True);
        }

        [Test]
        public void CreatingFromUTF8Bytes()
        {
            string a = "Hello World!";
            byte[] bytes = Encoding.UTF8.GetBytes(a);
            ASCIIText256 b = new(bytes);
            Assert.That(b.ToString(), Is.EqualTo(a));
        }

        [Test]
        public void CreateFromString()
        {
            string a = "Hello World!";
            ASCIIText256 b = new(a);
            Assert.That(b.Length, Is.EqualTo(a.Length));
            Assert.That(b.ToString(), Is.EqualTo(a));
        }
    }
}
