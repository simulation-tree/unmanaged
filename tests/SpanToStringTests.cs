using System;
using System.Numerics;

namespace Unmanaged.Tests
{
    public class SpanToStringTests : UnmanagedTests
    {
        [Test]
        public void PrimitivesToUSpanString()
        {
            Span<char> buffer = stackalloc char[64];
            int length = 0;

            byte byteValue1 = 128;
            byte byteValue2 = 29;
            byte byteValue3 = 3;
            Vector2 vector2Value = new(1, 2);
            Vector3 vector3Value = new(1, 2, 3);
            Vector4 vector4Value = new(1, 2, 3, 4);
            Quaternion quaternionValue = new(1, 2, 3, 4);

            length = byteValue1.ToString(buffer);
            Assert.That(buffer.Slice(0, length).ToString(), Is.EqualTo(byteValue1.ToString()));

            length = byteValue2.ToString(buffer);
            Assert.That(buffer.Slice(0, length).ToString(), Is.EqualTo(byteValue2.ToString()));

            length = byteValue3.ToString(buffer);
            Assert.That(buffer.Slice(0, length).ToString(), Is.EqualTo(byteValue3.ToString()));

            length = vector2Value.ToString(buffer);
            Assert.That(buffer.Slice(0, length).ToString(), Is.EqualTo(vector2Value.ToString()));

            length = vector3Value.ToString(buffer);
            Assert.That(buffer.Slice(0, length).ToString(), Is.EqualTo(vector3Value.ToString()));

            length = vector4Value.ToString(buffer);
            Assert.That(buffer.Slice(0, length).ToString(), Is.EqualTo(vector4Value.ToString()));

            length = quaternionValue.ToString(buffer);
            Assert.That(buffer.Slice(0, length).ToString(), Is.EqualTo(quaternionValue.ToString()));
        }
    }
}
