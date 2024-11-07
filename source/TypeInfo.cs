namespace Unmanaged
{
    public unsafe static class TypeInfo<T> where T : unmanaged
    {
        public readonly static uint size = (uint)sizeof(T);
    }
}
