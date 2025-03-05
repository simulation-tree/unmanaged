namespace Unmanaged.Pointers
{
    internal unsafe struct ByteReader
    {
        public readonly bool isOriginal;

        internal uint bytePosition;
        internal MemoryAddress data;
        internal uint byteLength;

        internal ByteReader(uint position, MemoryAddress data, uint length, bool isOriginal)
        {
            this.bytePosition = position;
            this.data = data;
            this.byteLength = length;
            this.isOriginal = isOriginal;
        }
    }
}
