using System.Numerics;

namespace Unmanaged.Tests
{
    public class SpanToStringTests : UnmanagedTests
    {
        [Test]
        public void PrimitivesToUSpanString()
        {
            USpan<char> buffer = stackalloc char[64];
            uint length = 0;

            byte byteValue1 = 128;
            byte byteValue2 = 29;
            byte byteValue3 = 3;
            short shortValue = 32767;
            int intValue = 2147483647;
            long longValue = 9223372036854775807;
            float floatValue = 3.40282347E+38f;
            double doubleValue = 1.7976931348623157E+308;
            char charValue = 'A';
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
        }
    }
}
