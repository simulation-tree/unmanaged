using Unmanaged;

namespace Tests
{
    public class ContainerTests
    {
        [Test]
        public void ContainValueAndCompare()
        {
            using Container container = Container.Allocate(32);
            ref int value = ref container.AsRef<int>();
            value *= 32;
            using Container anotherContainer = Container.Allocate(value);
            Assert.That(anotherContainer.AsRef<int>(), Is.EqualTo(container.AsRef<int>()));
        }

        [Test]
        public void CompareTwoContainers()
        {
            using Container a = Container.Allocate(32);
            using Container b = Container.Allocate(32);
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void CheckType()
        {
            using Container a = Container.Allocate(32);
            Assert.That(a.Is<int>(), Is.True);
        }

        [Test]
        public void ReadBytes()
        {
            using Container a = Container.Allocate(1337);
            Span<byte> bytes = a.AsSpan();
            Assert.That(bytes.Length, Is.EqualTo(sizeof(int)));
            Assert.That(BitConverter.ToInt32(bytes), Is.EqualTo(1337));
        }

        [Test]
        public void Disposing()
        {
            Container a = Container.Allocate(1337);
            a.Dispose();
            Assert.That(a.IsDisposed, Is.True);
        }
    }
}
