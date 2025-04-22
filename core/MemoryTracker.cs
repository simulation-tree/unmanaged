#if DEBUG
#define TRACK
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

[assembly: InternalsVisibleTo("Unmanaged.Tests")]
namespace Unmanaged
{
    internal unsafe static class MemoryTracker
    {
#if TRACK
        private static readonly ReaderWriterLockSlim threadLock = new();
        private static readonly Dictionary<nint, int> allocations = new();
        private static readonly Dictionary<nint, StackTrace> allocationStackTraces = new();

        public static void ThrowIfAny(bool freeAll = false)
        {
            threadLock.EnterReadLock();
            try
            {
                if (allocations.Count > 0)
                {
                    StringBuilder builder = new();
                    builder.AppendLine($"Memory leak detected, the following {allocations.Count} allocation(s) were not freed:");
                    foreach (nint address in allocations.Keys)
                    {
                        StackTrace stackTrace = allocationStackTraces[address];
                        builder.AppendLine($"    Size {allocations[address]} bytes:");
                        foreach (StackFrame frame in stackTrace.GetFrames())
                        {
                            builder.AppendLine($"        {frame}");
                        }
                    }

                    if (freeAll)
                    {
                        foreach (nint address in allocations.Keys)
                        {
                            NativeMemory.Free((void*)address);
                        }

                        allocations.Clear();
                        allocationStackTraces.Clear();
                    }

                    throw new Exception(builder.ToString());
                }
            }
            finally
            {
                threadLock.ExitReadLock();
            }
        }

        public static void ThrowIfDisposed(void* pointer)
        {
            threadLock.EnterReadLock();
            try
            {
                if (!allocations.ContainsKey((nint)pointer))
                {
                    throw new ObjectDisposedException($"The pointer at address {(nint)pointer} has been disposed");
                }
            }
            finally
            {
                threadLock.ExitReadLock();
            }
        }

        public static void Track(void* pointer, int byteLength)
        {
            threadLock.EnterWriteLock();
            try
            {
                allocations.TryAdd((nint)pointer, byteLength);
                allocationStackTraces[(nint)pointer] = new StackTrace(2, true);
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
                allocationStackTraces.Remove((nint)pointer);
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
                allocationStackTraces.Remove((nint)previousPointer);
                allocations.Remove((nint)previousPointer);
                allocations.TryAdd((nint)newPointer, newByteLength);
                allocationStackTraces[(nint)newPointer] = new StackTrace(2, true);
            }
            finally
            {
                threadLock.ExitWriteLock();
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
        public static void ThrowIfAny(bool freeAll = false)
        {
        }

        [Conditional("TRACK")]
        public static void ThrowIfDisposed(void* pointer)
        {
        }

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