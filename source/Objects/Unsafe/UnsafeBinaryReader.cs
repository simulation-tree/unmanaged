using System.IO;

namespace Unmanaged.Serialization.Unsafe
{
    public unsafe struct UnsafeBinaryReader
    {
        private uint position;
        private readonly bool clone;
        private readonly Allocation data;
        private readonly uint length;

        private UnsafeBinaryReader(uint position, Allocation data, uint length, bool clone)
        {
            this.position = position;
            this.data = data;
            this.length = length;
            this.clone = clone;
        }

        public static Allocation GetData(UnsafeBinaryReader* reader)
        {
            return reader->data;
        }

        public static UnsafeBinaryReader* Allocate(UnsafeBinaryReader* reader, uint position = 0)
        {
            UnsafeBinaryReader* copy = Allocations.Allocate<UnsafeBinaryReader>();
            copy[0] = new(position, reader->data, reader->length, true);
            return copy;
        }

        public static UnsafeBinaryReader* Allocate(UnsafeBinaryWriter* writer, uint position = 0)
        {
            UnsafeBinaryReader* copy = Allocations.Allocate<UnsafeBinaryReader>();
            Allocation data = new((Allocation*)UnsafeBinaryWriter.GetStartAddress(writer));
            copy[0] = new(position, data, UnsafeBinaryWriter.GetPosition(writer), true);
            return copy;
        }

        public static UnsafeBinaryReader* Allocate(USpan<byte> bytes, uint position = 0)
        {
            UnsafeBinaryReader* reader = Allocations.Allocate<UnsafeBinaryReader>();
            reader[0] = new(position, Allocation.Create(bytes), bytes.Length, false);
            return reader;
        }

        public static UnsafeBinaryReader* Allocate(Stream stream, uint position = 0)
        {
            uint bufferLength = (uint)stream.Length + 4;
            using Allocation buffer = new(bufferLength);
            USpan<byte> span = buffer.AsSpan(0, bufferLength);
            uint length = (uint)stream.Read(span.AsSystemSpan());
            USpan<byte> bytes = span.Slice(0, length);
            return Allocate(bytes, position);
        }

        public static bool IsDisposed(UnsafeBinaryReader* reader)
        {
            return reader is null;
        }

        public static ref uint GetPositionRef(UnsafeBinaryReader* reader)
        {
            return ref reader->position;
        }

        public static uint GetLength(UnsafeBinaryReader* reader)
        {
            return reader->length;
        }

        public static void Free(ref UnsafeBinaryReader* reader)
        {
            Allocations.ThrowIfNull(reader);
            if (!reader->clone)
            {
                reader->data.Dispose();
            }

            Allocations.Free(ref reader);
        }
    }
}
