using Unmanaged.Collections;

namespace Unmanaged.Serialization.Unsafe
{
    public unsafe struct UnsafeBinaryWriter
    {
        private UnmanagedList<byte> list;

        private UnsafeBinaryWriter(UnmanagedList<byte> list)
        {
            this.list = list;
        }

        public static nint GetAddress(UnsafeBinaryWriter* writer)
        {
            return writer->list.Address;
        }

        public static UnsafeBinaryWriter* Allocate(uint capacity = 1)
        {
            UnsafeBinaryWriter* ptr = Allocations.Allocate<UnsafeBinaryWriter>();
            ptr[0] = new(new(capacity));
            return ptr;
        }

        public static bool IsDisposed(UnsafeBinaryWriter* writer)
        {
            return Allocations.IsNull(writer);
        }

        public static void Free(ref UnsafeBinaryWriter* writer)
        {
            Allocations.ThrowIfNull(writer);
            writer->list.Dispose();
            Allocations.Free(ref writer);
        }

        public static uint SetPosition(UnsafeBinaryWriter* writer)
        {
            return writer->list.Count;
        }

        public static void SetPosition(UnsafeBinaryWriter* writer, uint position)
        {
            if (position > writer->list.Capacity)
            {
                writer->list.Capacity = position * 2;
            }

            ref uint count = ref UnsafeList.GetCountRef((UnsafeList*)writer->list.Address);
            count = position;
        }

        public static void Write(ref UnsafeBinaryWriter* writer, void* data, uint length)
        {
            Allocations.ThrowIfNull(writer);
            writer->list.AddRange(new(data, (int)length));
        }
    }
}
