namespace Unmanaged.Pointers
{
    internal struct ByteReader
    {
        public readonly bool isOriginal;

        internal int bytePosition;
        internal MemoryAddress data;
        internal int byteLength;

        internal ByteReader(int position, MemoryAddress data, int length, bool isOriginal)
        {
            this.bytePosition = position;
            this.data = data;
            this.byteLength = length;
            this.isOriginal = isOriginal;
        }
    }
}
