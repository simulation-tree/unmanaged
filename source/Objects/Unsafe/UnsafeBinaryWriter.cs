using System;

namespace Unmanaged.Serialization.Unsafe
{
    public unsafe struct UnsafeBinaryWriter
    {
        private Allocation items;
        private uint position;
        private uint capacity;

        private UnsafeBinaryWriter(Allocation items, uint length, uint capacity)
        {
            this.items = items;
            this.position = length;
            this.capacity = capacity;
        }

        public static nint GetAddress(UnsafeBinaryWriter* writer)
        {
            return writer->items.Address;
        }

        public static UnsafeBinaryWriter* Allocate(uint capacity = 1)
        {
            UnsafeBinaryWriter* ptr = Allocations.Allocate<UnsafeBinaryWriter>();
            ptr[0] = new(new(capacity), 0, capacity);
            return ptr;
        }

        public static UnsafeBinaryWriter* Allocate(Span<byte> span)
        {
            UnsafeBinaryWriter* ptr = Allocations.Allocate<UnsafeBinaryWriter>();
            ptr[0] = new(Allocation.Create(span), (uint)span.Length, (uint)span.Length);
            return ptr;
        }

        public static bool IsDisposed(UnsafeBinaryWriter* writer)
        {
            return Allocations.IsNull(writer);
        }

        public static void Free(ref UnsafeBinaryWriter* writer)
        {
            Allocations.ThrowIfNull(writer);
            writer->items.Dispose();
            Allocations.Free(ref writer);
        }

        public static uint GetPosition(UnsafeBinaryWriter* writer)
        {
            Allocations.ThrowIfNull(writer);
            return writer->position;
        }

        public static void SetPosition(UnsafeBinaryWriter* writer, uint position)
        {
            Allocations.ThrowIfNull(writer);
            if (position > writer->capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            writer->position = position;
        }

        public static void Write(ref UnsafeBinaryWriter* writer, void* data, uint length)
        {
            Allocations.ThrowIfNull(writer);
            uint endPosition = writer->position + length;
            while (writer->capacity < endPosition)
            {
                writer->capacity *= 2;
                writer->items.Resize(writer->capacity);
            }

            writer->items.Write(data, writer->position, length);
            writer->position = endPosition;
        }
    }
}
