using System;

namespace Unmanaged.Tests
{
    public class NumberExtensionTests : UnmanagedTests
    {
        [Test]
        public void CeilingAlignment()
        {
            uint a = NumberExtensions.CeilAlignment(4, 4);
            Assert.That(a, Is.EqualTo(4));

            uint b = NumberExtensions.CeilAlignment(5, 4);
            Assert.That(b, Is.EqualTo(8));

            uint c = NumberExtensions.CeilAlignment(6, 4);
            Assert.That(c, Is.EqualTo(8));

            uint d = NumberExtensions.CeilAlignment(7, 4);
            Assert.That(d, Is.EqualTo(8));

            uint e = NumberExtensions.CeilAlignment(8, 4);
            Assert.That(e, Is.EqualTo(8));

            uint f = NumberExtensions.CeilAlignment(1, 8);
            Assert.That(f, Is.EqualTo(8));

            uint g = NumberExtensions.CeilAlignment(9, 8);
            Assert.That(g, Is.EqualTo(16));

            uint h = NumberExtensions.CeilAlignment(10, 8);
            Assert.That(h, Is.EqualTo(16));

            uint i = NumberExtensions.CeilAlignment(1, 1);
            Assert.That(i, Is.EqualTo(1));

            uint j = NumberExtensions.CeilAlignment(1, 2);
            Assert.That(j, Is.EqualTo(2));
        }

        [Test]
        public void GetAlignment()
        {
            uint a = NumberExtensions.GetAlignment(4u);
            Assert.That(a, Is.EqualTo(4));

            uint b = NumberExtensions.GetAlignment(5u);
            Assert.That(b, Is.EqualTo(1));

            uint c = NumberExtensions.GetAlignment(6u);
            Assert.That(c, Is.EqualTo(2));

            uint d = NumberExtensions.GetAlignment(7u);
            Assert.That(d, Is.EqualTo(1));

            uint e = NumberExtensions.GetAlignment(8u);
            Assert.That(e, Is.EqualTo(8));

            uint f = NumberExtensions.GetAlignment(9u);
            Assert.That(f, Is.EqualTo(1));
        }

        [Test]
        public void GetNextPowerOf2()
        {
            int a = NumberExtensions.GetNextPowerOf2(0);
            Assert.That(a, Is.EqualTo(0));

            int b = NumberExtensions.GetNextPowerOf2(1);
            Assert.That(b, Is.EqualTo(1));

            int c = NumberExtensions.GetNextPowerOf2(2);
            Assert.That(c, Is.EqualTo(2));

            int d = NumberExtensions.GetNextPowerOf2(3);
            Assert.That(d, Is.EqualTo(4));

            int e = (int)NumberExtensions.GetNextPowerOf2(4u);
            Assert.That(e, Is.EqualTo(4));

            int f = NumberExtensions.GetNextPowerOf2(5);
            Assert.That(f, Is.EqualTo(8));
        }

        [Test]
        public void IndexPowerOf2AndIncrement()
        {
            int a = (int)NumberExtensions.GetIndexOfPowerOf2(4u);
            Assert.That(a, Is.EqualTo(2));
            a++;
            a = (int)Math.Pow(2, a);
            Assert.That(a, Is.EqualTo(8));
            
            int b = NumberExtensions.GetIndexOfPowerOf2(a);
            Assert.That(b, Is.EqualTo(3));
        }
    }
}
