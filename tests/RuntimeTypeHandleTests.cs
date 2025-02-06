﻿using System;

namespace Unmanaged.Tests
{
    public class RuntimeTypeHandleTests
    {
        [Test]
        public void CastTypeAddressBackToHandle()
        {
            nint address = RuntimeTypeTable.GetAddress<int>();
            Type? type = Type.GetTypeFromHandle(RuntimeTypeTable.FromAddress(address));
            Assert.That(type, Is.EqualTo(typeof(int)));
        }
    }
}