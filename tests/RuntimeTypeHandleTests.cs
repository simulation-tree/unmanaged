using System;

namespace Unmanaged.Tests
{
    public class RuntimeTypeHandleTests
    {
        [Test]
        public void CastTypeAddressBackToHandle()
        {
            nint address = RuntimeTypeHandle.ToIntPtr(typeof(int).TypeHandle);
            RuntimeTypeHandle handle = RuntimeTypeHandle.FromIntPtr(address);
            Type? type = Type.GetTypeFromHandle(handle);
            Assert.That(type, Is.EqualTo(typeof(int)));
        }
    }
}
