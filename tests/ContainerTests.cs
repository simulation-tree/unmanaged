﻿using System;
using Unmanaged;

namespace Unmanaged
{
    public class ContainerTests : UnmanagedTests
    {
        [Test]
        public void ContainValueAndCompare()
        {
            using Container container = Container.Create(32);
            ref int value = ref container.Read<int>();
            value *= 32;
            using Container anotherContainer = Container.Create(value);
            Assert.That(anotherContainer.Read<int>(), Is.EqualTo(container.Read<int>()));
        }

        [Test]
        public void CompareTwoContainers()
        {
            using Container a = Container.Create(32);
            using Container b = Container.Create(32);
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void CheckType()
        {
            using Container a = Container.Create(32);
            Assert.That(a.Is<int>(), Is.True);
        }

        [Test]
        public void ReadBytes()
        {
            using Container a = Container.Create(1337);
            USpan<byte> bytes = a.AsSpan();
            Assert.That(bytes.Length, Is.EqualTo(sizeof(int)));
            Assert.That(BitConverter.ToInt32(bytes.AsSystemSpan()), Is.EqualTo(1337));
        }

        [Test]
        public void Disposing()
        {
            Container a = Container.Create(1337);
            a.Dispose();
            Assert.That(a.IsDisposed, Is.True);
        }
    }
}