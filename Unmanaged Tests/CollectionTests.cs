using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Tests
{
    public class CollectionTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAnyAllocation();
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
            Console.WriteLine("started empty list 2");
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

        [Test]
        public unsafe void EmptyList()
        {
            using UnmanagedList<byte> list = new(8);
            Assert.That(list.Capacity, Is.EqualTo(8));
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddingIntoList()
        {
            using UnmanagedList<byte> list = new(1);
            list.Add(32);
            Assert.That(list[0], Is.EqualTo(32));
            Assert.That(list.Count, Is.EqualTo(1));
        }

        [Test]
        public void ListOfStrings()
        {
            using UnmanagedList<FixedString> list = new(3);
            list.Add("Hello");
            list.Add(" ");
            list.Add("there...");
            Assert.That(list[0].ToString(), Is.EqualTo("Hello"));
            Assert.That(list[1].ToString(), Is.EqualTo(" "));
            Assert.That(list[2].ToString(), Is.EqualTo("there..."));
        }

        [Test]
        public void ExpandingList()
        {
            using UnmanagedList<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(3));
            Assert.That(list[3], Is.EqualTo(4));
            Assert.That(list.Count, Is.EqualTo(4));
            Assert.That(list.Capacity, Is.EqualTo(4));
        }

        [Test]
        public void RemoveAtIndex()
        {
            using UnmanagedList<int> list = new();
            list.Add(1);
            list.Add(2); //removed
            list.Add(3);
            list.Add(4);
            list.RemoveAt(1);
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(3));
            Assert.That(list[2], Is.EqualTo(4));
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(4));
        }

        [Test]
        public void RemoveAtIndexWithSwapback()
        {
            using UnmanagedList<int> list = new();
            list.Add(1);
            list.Add(2); //removed
            list.Add(3);
            list.Add(4);
            list.RemoveAtBySwapping(1);
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(4));
            Assert.That(list[2], Is.EqualTo(3));
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(4));
        }

        [Test]
        public void InsertIntoList()
        {
            using UnmanagedList<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(4);
            list.Insert(2, 3);
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(3));
            Assert.That(list[3], Is.EqualTo(4));
        }

        [Test]
        public void ListContains()
        {
            using UnmanagedList<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            Assert.That(list.Contains(3), Is.True);
            Assert.That(list.Contains(5), Is.False);
        }

        [Test]
        public void ClearListThenAdd()
        {
            using UnmanagedList<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.Clear();
            Assert.That(list.Count, Is.EqualTo(0));
            list.Add(5);
            Assert.That(list[0], Is.EqualTo(5));
            Assert.That(list.Count, Is.EqualTo(1));
        }

        [Test]
        public void BuildListThenCopyToSpan()
        {
            using UnmanagedList<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            Span<int> span = stackalloc int[4];
            list.CopyTo(span);
            Assert.That(span[0], Is.EqualTo(1));
            Assert.That(span[1], Is.EqualTo(2));
            Assert.That(span[2], Is.EqualTo(3));
            Assert.That(span[3], Is.EqualTo(4));
        }

        [Test]
        public unsafe void ReadBytesFromList()
        {
            UnsafeList* data = UnsafeList.Allocate<int>();
            UnsafeList.Add(data, 1);
            UnsafeList.Add(data, 2);
            UnsafeList.Add(data, 3);
            UnsafeList.Add(data, 4);

            Span<byte> span = UnsafeList.AsSpan<byte>(data);
            int value1 = BitConverter.ToInt32(span.Slice(0, 4));
            int value2 = BitConverter.ToInt32(span.Slice(4, 4));
            int value3 = BitConverter.ToInt32(span.Slice(8, 4));
            int value4 = BitConverter.ToInt32(span.Slice(12, 4));
            Assert.That(value1, Is.EqualTo(1));
            Assert.That(value2, Is.EqualTo(2));
            Assert.That(value3, Is.EqualTo(3));
            Assert.That(value4, Is.EqualTo(4));
            UnsafeList.Free(ref data);
            Assert.That(UnsafeList.IsDisposed(data), Is.True);
        }

        [Test]
        public void ListFromSpan()
        {
            Span<char> word = stackalloc char[] { 'H', 'e', 'l', 'l', 'o' };
            using UnmanagedList<char> list = new(word);
            Assert.That(list.Count, Is.EqualTo(5));
            Span<char> otherSpan = list.AsSpan();
            Assert.That(otherSpan[0], Is.EqualTo('H'));
            Assert.That(otherSpan[1], Is.EqualTo('e'));
            Assert.That(otherSpan[2], Is.EqualTo('l'));
            Assert.That(otherSpan[3], Is.EqualTo('l'));
            Assert.That(otherSpan[4], Is.EqualTo('o'));
        }

        [Test]
        public void ListInsideArray()
        {
            UnmanagedArray<UnmanagedList<byte>> nestedData = new(8);
            for (uint i = 0; i < 8; i++)
            {
                ref UnmanagedList<byte> list = ref nestedData.GetRef(i);
                list = new();
                list.Add((byte)i);
            }

            for (uint i = 0; i < 8; i++)
            {
                UnmanagedList<byte> list = nestedData[i];
                Assert.That(list[0], Is.EqualTo((byte)i));

                list.Dispose();
            }

            nestedData.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void CreatingAndDisposingDictionary()
        {
            UnmanagedDictionary<byte, uint> map = new();
            map.Add(0, 23);
            map.Add(1, 42);
            map.Add(2, 69);
            Assert.That(map.ContainsKey(0), Is.True);
            Assert.That(map.ContainsKey(1), Is.True);
            Assert.That(map.ContainsKey(2), Is.True);
            Assert.That(map.ContainsKey(3), Is.False);
            Assert.That(map.GetRef(0), Is.EqualTo(23));
            Assert.That(map.GetRef(1), Is.EqualTo(42));
            Assert.That(map.GetRef(2), Is.EqualTo(69));
            map.Dispose();

            Assert.That(Allocations.Count, Is.EqualTo(0));
        }
    }
}
