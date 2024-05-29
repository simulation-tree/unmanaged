using System;
using System.Diagnostics;
using System.IO;

namespace Unmanaged.Serialization.Unsafe
{
    public unsafe struct UnsafeBinaryReader
    {
        private uint position;
        private readonly uint length;

        private UnsafeBinaryReader(uint position, uint length)
        {
            this.position = position;
            this.length = length;
        }

        public static UnsafeBinaryReader* Allocate(ReadOnlySpan<byte> bytes, uint position = 0)
        {
            void* ptr = Allocations.Allocate((uint)(sizeof(UnsafeBinaryReader) + bytes.Length));
            UnsafeBinaryReader* ptrTyped = (UnsafeBinaryReader*)ptr;
            uint length = (uint)bytes.Length;
            ptrTyped[0] = new(position, length);
            fixed (byte* ptrBytes = bytes)
            {
                nint destination = ((nint)ptr + sizeof(UnsafeBinaryReader));
                System.Runtime.CompilerServices.Unsafe.CopyBlock((void*)destination, ptrBytes, length);
            }

            return ptrTyped;
        }

        public static UnsafeBinaryReader* Allocate(Stream stream, uint position = 0)
        {
            uint length = (uint)stream.Length;
            void* ptr = Allocations.Allocate((uint)(sizeof(UnsafeBinaryReader) + length));
            UnsafeBinaryReader* ptrTyped = (UnsafeBinaryReader*)ptr;
            ptrTyped[0] = new(position, length);
            nint destination = ((nint)ptr + sizeof(UnsafeBinaryReader));
            Span<byte> streamSpan = new((byte*)destination, (int)length);
            int read = stream.Read(streamSpan);
            Debug.Assert(read == length, "Failed to read the entire stream.");
            return ptrTyped;
        }

        public static bool IsDisposed(UnsafeBinaryReader* reader)
        {
            return Allocations.IsNull(reader);
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
            Allocations.Free(ref reader);
        }
    }
}
