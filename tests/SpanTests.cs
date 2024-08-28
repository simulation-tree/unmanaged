using System;
using Unmanaged;

namespace Tests
{
    public class SpanTests
    {
        [Test]
        public void CreatingUsingStackalloc()
        {
            USpan<byte> data = stackalloc byte[8];
            Assert.That(data.length, Is.EqualTo(8));
            data[0] = 1;
            data[1] = 2;

            Assert.That(data[0], Is.EqualTo(1));
            Assert.That(data[1], Is.EqualTo(2));
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void AccessingSpanOutOfRange()
        {
            USpan<byte> data = stackalloc byte[8];
            try
            {
                data[8] = 0;
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Slicing()
        {
            USpan<byte> data = stackalloc byte[8];
            Span<byte> referenceData = stackalloc byte[8];
            for (uint i = 0; i < 8; i++)
            {
                data[i] = (byte)i;
                referenceData[(int)i] = (byte)i;
            }

            USpan<byte> slice = data[2..4];
            Span<byte> referenceSlice = referenceData[2..4];

            Assert.That(slice.length, Is.EqualTo(referenceSlice.Length));
            Assert.That(slice.ToArray(), Is.EqualTo(referenceSlice.ToArray()));

            using RandomGenerator rng = new();
            for (int i = 0; i < 32; i++)
            {
                uint length = rng.NextUInt(8, 16);
                uint sliceLength = rng.NextUInt(1, 4);
                uint sliceStart = rng.NextUInt(0, length - sliceLength);
                TestRandomSlice(length, sliceStart, sliceLength);
            }

            static void TestRandomSlice(uint length, uint sliceStart, uint sliceLength)
            {
                USpan<byte> data = stackalloc byte[(int)length];
                Span<byte> referenceData = stackalloc byte[(int)length];
                for (uint i = 0; i < length; i++)
                {
                    data[i] = (byte)i;
                    referenceData[(int)i] = (byte)i;
                }

                Range sliceRange = (int)sliceStart..(int)(sliceStart + sliceLength);
                USpan<byte> slice = data[sliceRange];
                Span<byte> referenceSlice = referenceData[sliceRange];
                Assert.That(slice.length, Is.EqualTo(referenceSlice.Length));
                Assert.That(slice.ToArray(), Is.EqualTo(referenceSlice.ToArray()));
            }
        }

        [Test]
        public void SliceOutOfRange()
        {
            Span<byte> data = stackalloc byte[8];
            try
            {
                Span<byte> slice = data[8..9];
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch
            {
                Assert.Fail();
            }

            USpan<byte> uData = stackalloc byte[8];
            try
            {
                USpan<byte> slice = uData[8..9];
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch
            {
                Assert.Fail();
            }
        }
    }
}
