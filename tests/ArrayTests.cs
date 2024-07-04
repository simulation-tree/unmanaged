using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Tests
{
    public class ArrayTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAny();
        }

        [Test]
        public void EmptyArray()
        {
            UnmanagedArray<int> array = new();
            Assert.That(array.Length, Is.EqualTo(0));
            array.Dispose();
            Assert.That(array.IsDisposed, Is.True);
        }

        [Test]
        public void ArrayLength()
        {
            using UnmanagedArray<Guid> array = new(4);
            Assert.That(array.Length, Is.EqualTo(4));
        }

        [Test]
        public void CreatingArrayFromSpan()
        {
            Span<int> span = stackalloc int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            using UnmanagedArray<int> array = new(span);
            Assert.That(array.Length, Is.EqualTo(8));
        }

        [Test]
        public void ResizeArray()
        {
            using UnmanagedArray<int> array = new(4);
            array.Resize(8);
            Assert.That(array.Length, Is.EqualTo(8));

            array[array.Length - 1] = 1;

            array.Resize(4);
            Assert.That(array.Length, Is.EqualTo(4));

            array.Resize(12);
            Assert.That(array.Length, Is.EqualTo(12));
        }

        [Test]
        public void ClearingArray()
        {
            using UnmanagedArray<int> array = new(4);
            array[0] = 1;
            array[1] = 2;
            array[2] = 3;
            array[3] = 4;
            array.Clear();
            Assert.That(array[0], Is.EqualTo(0));
            Assert.That(array[1], Is.EqualTo(0));
            Assert.That(array[2], Is.EqualTo(0));
            Assert.That(array[3], Is.EqualTo(0));
        }

        [Test]
        public void IndexingArray()
        {
            using UnmanagedArray<int> array = new(4);
            array[0] = 1;
            array[1] = 2;
            array[2] = 3;
            array[3] = 4;
            Assert.That(array[0], Is.EqualTo(1));
            Assert.That(array[1], Is.EqualTo(2));
            Assert.That(array[2], Is.EqualTo(3));
            Assert.That(array[3], Is.EqualTo(4));
        }
    }
}
