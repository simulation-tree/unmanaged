namespace Unmanaged.Pointers
{
    internal struct ByteWriter
    {
        internal MemoryAddress data;
        internal int bytePosition;
        internal int capacity;

        internal ByteWriter(MemoryAddress items, int length, int capacity)
        {
            this.data = items;
            this.bytePosition = length;
            this.capacity = capacity;
        }
    }
}
