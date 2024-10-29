namespace Unmanaged
{
    public abstract class UnmanagedTests
    {
        [SetUp]
        protected virtual void SetUp()
        {
        }

        [TearDown]
        protected virtual void CleanUp()
        {
            Allocations.ThrowIfAny();
        }
    }
}
