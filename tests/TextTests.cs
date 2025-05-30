﻿using System;

namespace Unmanaged.Tests
{
    public class TextTests : UnmanagedTests
    {
        [Test]
        public void CreateTextFromSpan()
        {
            using Text text = new("Hello there");
            Assert.That(text.ToString(), Is.EqualTo("Hello there"));
        }

        [Test]
        public void ResizeText()
        {
            using Text text = new("Apple");
            text.SetLength(10, 'x');

            Assert.That(text.Length, Is.EqualTo(10));
            Assert.That(text.ToString(), Is.EqualTo("Applexxxxx"));

            text.SetLength(3);
            Assert.That(text.Length, Is.EqualTo(3));
            Assert.That(text.ToString(), Is.EqualTo("App"));
        }

        [Test]
        public void CopyFrom()
        {
            using Text text = new("");
            text.CopyFrom("Hello there");

            Assert.That(text.ToString(), Is.EqualTo("Hello there"));
        }

        [Test]
        public void ConcatenateText()
        {
            using Text a = new("This");
            using Text b = new(" a test");
            using Text result = a + b;

            Assert.That(result.ToString(), Is.EqualTo("This a test"));
        }

        [Test]
        public void Enumerate()
        {
            using Text text = new("Something in here");
            string manual = "";
            foreach (char c in text)
            {
                manual += c;
            }

            Assert.That(manual, Is.EqualTo(text.ToString()));
        }

        [Test]
        public void ReplaceAll()
        {
            using Text text = new("Hello there");
            Span<char> destination = stackalloc char[256];
            int newLength = Text.Replace(text.AsSpan(), "e", "x", destination);
            Assert.That(destination.Slice(0, newLength).ToString(), Is.EqualTo("Hxllo thxrx"));

            text.CopyFrom("This is another is");
            newLength = Text.Replace(text.AsSpan(), "is", "was", destination);
            Assert.That(destination.Slice(0, newLength).ToString(), Is.EqualTo("Thwas was another was"));
        }

        [Test]
        public void RemoveRange()
        {
            using Text text = new("Hello there");
            text.Remove(0, 6);

            Assert.That(text.ToString(), Is.EqualTo("there"));

            text.Remove(3, 2);

            Assert.That(text.ToString(), Is.EqualTo("the"));

            text.Remove(2, 1);

            Assert.That(text.ToString(), Is.EqualTo("th"));
        }

        [Test]
        public void ClearByRemoving()
        {
            using Text text = new("Hello there");
            text.RemoveAt(0);
            text.RemoveAt(0);
            text.RemoveAt(0);
            text.RemoveAt(0);
            text.RemoveAt(0);
            text.RemoveAt(0);

            Assert.That(text.Length, Is.EqualTo(5));
            Assert.That(text.ToString(), Is.EqualTo("there"));

            text.RemoveAt(0);
            text.RemoveAt(0);
            text.RemoveAt(0);
            text.RemoveAt(0);
            text.RemoveAt(0);

            Assert.That(text.Length, Is.EqualTo(0));
        }

        [Test]
        public void SliceText()
        {
            using Text text = new("Some kind of sample");

            Assert.That(text.Slice(0, 4).ToString(), Is.EqualTo("Some"));
            Assert.That(text.Slice(5, 4).ToString(), Is.EqualTo("kind"));
            Assert.That(text.Slice(10, 2).ToString(), Is.EqualTo("of"));
            Assert.That(text.Slice(13, 6).ToString(), Is.EqualTo("sample"));
        }

        [Test]
        public void RemoveFromEnd()
        {
            using Text text = new(0);
            for (int i = 0; i < 5; i++)
            {
                text.Append("Item, ");
            }

            text.RemoveAt(text.Length - 1);
            text.RemoveAt(text.Length - 1);

            Assert.That(text.ToString(), Is.EqualTo("Item, Item, Item, Item, Item"));
        }

        [Test]
        public void AppendWords()
        {
            using Text text = new();
            text.Append("Item".AsSpan());

            Assert.That(text.ToString(), Is.EqualTo("Item"));

            text.Append("ization");

            Assert.That(text.ToString(), Is.EqualTo("Itemization"));

            using Text otherText = new(" of stuff");
            text.Append(otherText.Borrow());

            Assert.That(text.ToString(), Is.EqualTo("Itemization of stuff"));
        }

        [Test]
        public void AppendNumbers()
        {
            using Text text = new();
            text.Append("something");
            Assert.That(text.ToString(), Is.EqualTo("something"));
            text.Append(1234);
            Assert.That(text.ToString(), Is.EqualTo("something1234"));
        }

        [Test]
        public void AppendCharacters()
        {
            using Text text = new();
            text.Append('L');

            Assert.That(text.ToString(), Is.EqualTo("L"));

            text.Append('o', 5);
            text.Append('l');

            Assert.That(text.ToString(), Is.EqualTo("Loooool"));
        }

        [Test]
        public void InsertSingleCharacter()
        {
            using Text text = new("abacus");
            text.Insert(3, 'x');

            Assert.That(text.ToString(), Is.EqualTo("abaxcus"));

            text.RemoveAt(3);
            text.Insert(text.Length, 'a');

            Assert.That(text.ToString(), Is.EqualTo("abacusa"));
        }

        [Test]
        public void InsertWords()
        {
            using Text text = new();
            text.Insert(0, "there");

            Assert.That(text.ToString(), Is.EqualTo("there"));

            text.Insert(0, "Hello ");

            Assert.That(text.ToString(), Is.EqualTo("Hello there"));

            text.Insert(6, "world, ");

            Assert.That(text.ToString(), Is.EqualTo("Hello world, there"));

            text.Insert(text.Length, " is");

            Assert.That(text.ToString(), Is.EqualTo("Hello world, there is"));
        }

        [Test]
        public void InsertDigits()
        {
            using Text text = new();
            text.Append(2025);
            text.Insert(2, 1337);

            Assert.That(text.ToString(), Is.EqualTo("20133725"));
        }

        [Test]
        public void RemoveThenInsertToReplace()
        {
            using Text text = new("This is the way");

            Assert.That(text.Length, Is.EqualTo(15));

            text.Remove(5, 2);

            Assert.That(text.Length, Is.EqualTo(13));
            Assert.That(text.ToString(), Is.EqualTo("This  the way"));

            text.Insert(5, "isnt");

            Assert.That(text.Length, Is.EqualTo(17));
            Assert.That(text.ToString(), Is.EqualTo("This isnt the way"));
        }
    }
}
