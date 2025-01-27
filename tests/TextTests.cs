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
            USpan<char> destination = stackalloc char[256];
            uint newLength = Text.Replace(text.AsSpan(), "e", "x", destination);
            Assert.That(destination.Slice(0, newLength).ToString(), Is.EqualTo("Hxllo thxrx"));

            text.CopyFrom("This is another is");
            newLength = Text.Replace(text.AsSpan(), "is", "was", destination);
            Assert.That(destination.Slice(0, newLength).ToString(), Is.EqualTo("Thwas was another was"));
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
    }
}
