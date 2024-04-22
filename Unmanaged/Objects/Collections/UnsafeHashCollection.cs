using System.Runtime.InteropServices;

namespace Unmanaged.Collections
{
    //public unsafe struct UnsafeHashSet
    //{
    //    private Allocation freeHead;
    //    private Allocation buckets;
    //    private RuntimeType type;
    //    private uint used;
    //    private uint free;
    //
    //    public static UnsafeHashSet* Allocate<T>(uint capacity = 0) where T : unmanaged
    //    {
    //        UnsafeHashSet* set = (UnsafeHashSet*)Marshal.AllocHGlobal(sizeof(UnsafeHashSet));
    //        set->type = RuntimeType.Get<T>();
    //        set->buckets = new Allocation((uint)(sizeof(nint) * capacity));
    //        set->freeHead = new Allocation((uint)(sizeof(Entry) * capacity));
    //        set->used = 0;
    //        set->free = 0;
    //        return set;
    //    }
    //
    //    public static void Free(UnsafeHashSet* set)
    //    {
    //        set->buckets.Dispose();
    //        set->freeHead.Dispose();
    //        Marshal.FreeHGlobal((nint)set);
    //    }
    //
    //    public struct Entry
    //    {
    //        public Entry* next;
    //        public int hash;
    //        public State state;
    //
    //        public enum State
    //        {
    //            None,
    //            Free,
    //            Used
    //        }
    //    }
    //}
}
