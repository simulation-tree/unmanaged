namespace Unmanaged.Pointers
{
    internal unsafe struct ByteWriter
    {
        internal MemoryAddress data;
        internal uint bytePosition;
        internal uint capacity;

        internal ByteWriter(MemoryAddress items, uint length, uint capacity)
        {
            this.data = items;
            this.bytePosition = length;
            this.capacity = capacity;
        }
    }
}
