namespace Unmanaged.Pointers
{
    internal unsafe struct ByteWriter
    {
        internal Allocation data;
        internal uint bytePosition;
        internal uint capacity;

        internal ByteWriter(Allocation items, uint length, uint capacity)
        {
            this.data = items;
            this.bytePosition = length;
            this.capacity = capacity;
        }
    }
}
