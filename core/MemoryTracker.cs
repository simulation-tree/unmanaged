#if DEBUG
#define TRACK
#endif

using System;
using System.Collections.Generic;
using System.Threading;

#if !TRACK
using System.Diagnostics;
#endif

namespace Unmanaged
{
    internal unsafe static class MemoryTracker
    {
#if TRACK
        private static readonly ReaderWriterLockSlim threadLock = new();
        private static readonly List<nint> disposals = new();
        private static readonly Dictionary<nint, int> allocations = new();

        private static void RemoveDisposedPointer(void* pointer)
        {
            nint address = (nint)pointer;
            for (int i = 0; i < disposals.Count; i++)
            {
                if (disposals[i] == address)
                {
                    disposals.RemoveAt(i);
                    break;
                }
            }
        }

        public static void Track(void* pointer, int byteLength)
        {
            threadLock.EnterWriteLock();
            try
            {
                RemoveDisposedPointer(pointer);
                allocations.TryAdd((nint)pointer, byteLength);
            }
            finally
            {
                threadLock.ExitWriteLock();
            }
        }

        public static void Untrack(void* pointer)
        {
            threadLock.EnterWriteLock();
            try
            {
                disposals.Add((nint)pointer);
                allocations.Remove((nint)pointer);
            }
            finally
            {
                threadLock.ExitWriteLock();
            }
        }

        public static void Move(void* previousPointer, void* newPointer, int newByteLength)
        {
            threadLock.EnterWriteLock();
            try
            {
                disposals.Add((nint)previousPointer);
                allocations.Remove((nint)previousPointer);
                RemoveDisposedPointer(newPointer);
                allocations.TryAdd((nint)newPointer, newByteLength);
            }
            finally
            {
                threadLock.ExitWriteLock();
            }
        }

        public static void ThrowIfDisposed(void* pointer)
        {
            threadLock.EnterReadLock();
            try
            {
                nint address = (nint)pointer;
                if (disposals.Contains(address))
                {
                    throw new ObjectDisposedException($"The pointer at address {address} has been disposed");
                }
            }
            finally
            {
                threadLock.ExitReadLock();
            }
        }

        public static void ThrowIfOutOfBounds(void* pointer, int byteIndex)
        {
            threadLock.EnterReadLock();
            try
            {
                if (allocations.TryGetValue((nint)pointer, out int byteLength))
                {
                    if (byteIndex < 0 || byteIndex >= byteLength)
                    {
                        throw new IndexOutOfRangeException($"The pointer at address {(nint)pointer} is out of bounds at index {byteIndex}");
                    }
                }
            }
            finally
            {
                threadLock.ExitReadLock();
            }
        }

        public static void ThrowIfGreaterThanLength(void* pointer, int byteIndex)
        {
            threadLock.EnterReadLock();
            try
            {
                if (allocations.TryGetValue((nint)pointer, out int byteLength))
                {
                    if (byteIndex > byteLength)
                    {
                        throw new IndexOutOfRangeException($"The pointer at address {(nint)pointer} is out of bounds at index {byteIndex}");
                    }
                }
            }
            finally
            {
                threadLock.ExitReadLock();
            }
        }

#else
        [Conditional("TRACK")]
        public static void Track(void* pointer, int byteLength)
        {
        }
        
        [Conditional("TRACK")]
        public static void Untrack(void* pointer)
        {
        }

        [Conditional("TRACK")]
        public static void Move(void* previousPointer, void* newPointer, int newByteLength)
        {
        }

        [Conditional("TRACK")]
        public static void ThrowIfDisposed(void* pointer)
        {
        }

        [Conditional("TRACK")]
        public static void ThrowIfOutOfBounds(void* pointer, int byteIndex)
        {
        }

        [Conditional("TRACK")]
        public static void ThrowIfGreaterThanLength(void* pointer, int byteIndex)
        {
        }
#endif
    }
}