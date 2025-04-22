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
            MemoryTracker.ThrowIfAny(true);
        }
    }
}