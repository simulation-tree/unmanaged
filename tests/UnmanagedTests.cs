﻿using System;

namespace Unmanaged.Tests
{
    public abstract class UnmanagedTests
    {
        [SetUp]
        protected virtual void SetUp()
        {
        }

        [TearDown]
        protected virtual void TearDown()
        {
            MemoryTracker.ThrowIfAny();
        }

        protected static bool IsRunningRemotely()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") is not null)
            {
                return true;
            }

            return false;
        }
    }
}