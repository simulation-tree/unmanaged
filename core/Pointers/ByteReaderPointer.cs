namespace Unmanaged.Pointers
{
    internal struct ByteReaderPointer
    {
        public bool isOriginal;
        public int bytePosition;
        public int byteLength;
        public MemoryAddress data;
    }
}
