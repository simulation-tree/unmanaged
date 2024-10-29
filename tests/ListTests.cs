﻿using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Unmanaged
{
    public class ListTests : UnmanagedTests
    {
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
            using UnmanagedList<int> list = UnmanagedList<int>.Create();
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
            using UnmanagedList<int> list = UnmanagedList<int>.Create();
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
            using UnmanagedList<int> list = UnmanagedList<int>.Create();
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
            using UnmanagedList<int> list = UnmanagedList<int>.Create();
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
        public void AddRange()
        {
            using UnmanagedList<uint> list = UnmanagedList<uint>.Create();
            list.AddRange(new[] { 1u, 2u, 3u, 4u });
            Assert.That(list.Count, Is.EqualTo(4));
            Assert.That(list[0], Is.EqualTo(1u));
            Assert.That(list[1], Is.EqualTo(2u));
            Assert.That(list[2], Is.EqualTo(3u));
            Assert.That(list[3], Is.EqualTo(4u));
            var result = list.AsSpan().ToArray();
            list.AddRange(new[] { 5u, 6u, 7u, 8u });
            var result2 = list.AsSpan().ToArray();
            Assert.That(list.Count, Is.EqualTo(8));
            Assert.That(list[4], Is.EqualTo(5u));
            Assert.That(list[5], Is.EqualTo(6u));
            Assert.That(list[6], Is.EqualTo(7u));
            Assert.That(list[7], Is.EqualTo(8u));
        }

        [Test]
        public void AddRepeat()
        {
            using UnmanagedList<byte> list = UnmanagedList<byte>.Create();
            list.AddRepeat(5, 33);
            Assert.That(list.Count, Is.EqualTo(33));
            Assert.That(list[0], Is.EqualTo(5));

            list.AddRepeat(9, 44);
            Assert.That(list[32], Is.EqualTo(5));
            Assert.That(list[33], Is.EqualTo(9));
            Assert.That(list.Count, Is.EqualTo(77));
        }

        [Test]
        public void InsertRange()
        {
            using UnmanagedList<int> list = UnmanagedList<int>.Create();
            list.Add(1);
            list.Add(2);
            list.Add(6);
            list.InsertRange(2, new[] { 3, 4, 5 });
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(3));
            Assert.That(list[3], Is.EqualTo(4));
            Assert.That(list[4], Is.EqualTo(5));
            Assert.That(list[5], Is.EqualTo(6));
        }

        [Test]
        public void ListContains()
        {
            using UnmanagedList<int> list = UnmanagedList<int>.Create();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            Assert.That(list.Contains(3), Is.True);
            Assert.That(list.Contains(5), Is.False);
        }

        [Test]
        public void ClearWithMinimumCapacity()
        {
            using UnmanagedList<int> list = UnmanagedList<int>.Create();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.Clear(32);
            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(32));
            Assert.That(list.IsDisposed, Is.False);
        }

        [Test]
        public void ClearListThenAdd()
        {
            using UnmanagedList<int> list = UnmanagedList<int>.Create();
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
            using UnmanagedList<int> list = UnmanagedList<int>.Create();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            USpan<int> span = stackalloc int[4];
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

            USpan<byte> span = UnsafeList.AsSpan<byte>(data);
            Assert.That(span.Length, Is.EqualTo(sizeof(int) * 4));
            int value1 = BitConverter.ToInt32(span.Slice(0, 4).AsSystemSpan());
            int value2 = BitConverter.ToInt32(span.Slice(4, 4).AsSystemSpan());
            int value3 = BitConverter.ToInt32(span.Slice(8, 4).AsSystemSpan());
            int value4 = BitConverter.ToInt32(span.Slice(12, 4).AsSystemSpan());
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
            USpan<char> word = ['H', 'e', 'l', 'l', 'o'];
            UnmanagedList<char> list = new(word);
            Assert.That(list.Count, Is.EqualTo(5));
            USpan<char> otherSpan = list.AsSpan();
            Assert.That(otherSpan[0], Is.EqualTo('H'));
            Assert.That(otherSpan[1], Is.EqualTo('e'));
            Assert.That(otherSpan[2], Is.EqualTo('l'));
            Assert.That(otherSpan[3], Is.EqualTo('l'));
            Assert.That(otherSpan[4], Is.EqualTo('o'));
            list.Dispose();
        }

        [Test]
        public void ListInsideArray()
        {
            UnmanagedArray<UnmanagedList<byte>> nestedData = new(8);
            for (uint i = 0; i < 8; i++)
            {
                ref UnmanagedList<byte> list = ref nestedData[i];
                list = UnmanagedList<byte>.Create();
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
        public unsafe void AddAnotherUnsafeList()
        {
            UnsafeList* a = UnsafeList.Allocate<int>();
            UnsafeList* b = UnsafeList.Allocate<int>();
            UnsafeList.AddRange(a, [1, 3]);
            UnsafeList.AddRange(b, [3, 7, 7]);
            Assert.That(UnsafeList.AsSpan<int>(a).ToArray(), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(UnsafeList.AsSpan<int>(b).ToArray(), Is.EqualTo(new[] { 3, 7, 7 }));
            UnsafeList.AddRange(a, (void*)UnsafeList.GetStartAddress(b), UnsafeList.GetCountRef(b));
            Assert.That(UnsafeList.AsSpan<int>(a).ToArray(), Is.EqualTo(new[] { 1, 3, 3, 7, 7 }));
            UnsafeList.Free(ref a);
            UnsafeList.Free(ref b);
        }
    }
}
