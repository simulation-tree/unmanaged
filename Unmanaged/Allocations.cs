using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Unmanaged
{
    /// <summary>
    /// Static debug utility for tracking unmanaged instances.
    /// </summary>
    public static class Allocations
    {
        private static readonly Dictionary<nint, StackTrace> allocations = [];
        private static readonly HashSet<nint> instances = [];
        private static readonly Dictionary<nint, StackTrace> disposals = [];

        /// <summary>
        /// <c>true</c> if there are any instances allocated at the moment.
        /// </summary>
        public static bool Any
        {
            get
            {
                return instances.Count > 0;
            }
        }

        public static IEnumerable<(object, StackTrace)> All
        {
            get
            {
                foreach (nint pointer in instances)
                {
                    if (allocations.TryGetValue(pointer, out StackTrace? stackTrace))
                    {
                        yield return (pointer, stackTrace);
                    }
                }
            }
        }

        static Allocations()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                if (instances.Count > 0)
                {
                    StringBuilder leaks = new();
                    leaks.Append(instances.Count);
                    leaks.Append(" unmanaged instance(s) were left undisposed: ");
                    leaks.AppendLine();
                    foreach (nint pointer in instances)
                    {
                        StackTrace stackTrace = allocations[pointer];
                        leaks.Append(pointer);
                        leaks.Append(" allocated at:");
                        leaks.AppendLine();
                        leaks.Append(stackTrace);
                        leaks.AppendLine();
                    }

                    throw new SystemException(leaks.ToString());
                }
            };
        }

        [Conditional("DEBUG")]
        public static void Register(nint pointer)
        {
            foreach (nint existingInstance in instances)
            {
                if (existingInstance.Equals(pointer))
                {
                    if (allocations.TryGetValue(pointer, out StackTrace? previousStackTrace))
                    {
                        throw new InvalidOperationException($"Pointer {pointer} has already been allocated from:\n{previousStackTrace}.");
                    }
                }
            }

            StackTrace stackTrace = new(1, true);
            instances.Add(pointer);
            allocations[pointer] = stackTrace;
        }

        [Conditional("DEBUG")]
        public static void Unregister(nint pointer)
        {
            bool found = false;
            foreach (nint existingInstance in instances)
            {
                if (existingInstance.Equals(pointer))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                foreach ((nint disposedInstance, StackTrace disposedStackTrace) in disposals)
                {
                    if (disposedInstance.Equals(pointer))
                    {
                        throw new ObjectDisposedException($"Pointer {pointer} was disposed at\n{disposedStackTrace}.");
                    }
                }

                throw new NullReferenceException($"Pointer {pointer} has never been registered.");
            }

            instances.Remove(pointer);
            StackTrace stackTrace = new(1, true);
            disposals[pointer] = stackTrace;
        }

#if DEBUG
        public static bool IsNull(nint pointer)
        {
            foreach (nint existingInstance in instances)
            {
                if (existingInstance.Equals(pointer))
                {
                    return false;
                }
            }

            foreach ((nint disposedInstance, StackTrace stackTrace) in disposals)
            {
                if (disposedInstance.Equals(pointer))
                {
                    return true;
                }
            }

            foreach ((nint allocatedInstance, _) in allocations)
            {
                if (allocatedInstance.Equals(pointer))
                {
                    return false;
                }
            }

            return true;
        }
#else
        public static bool IsNull(nint pointer) => false;
#endif

        [Conditional("DEBUG")]
        public static void ThrowIfNull(nint pointer)
        {
            if (instances.Contains(pointer))
            {
                return;
            }

            foreach ((nint disposedInstance, StackTrace stackTrace) in disposals)
            {
                if (disposedInstance.Equals(pointer))
                {
                    throw new ObjectDisposedException($"Pointer {pointer} has been disposed at\n{stackTrace}.");
                }
            }

            foreach ((nint allocatedInstance, _) in allocations)
            {
                if (allocatedInstance.Equals(pointer))
                {
                    return;
                }
            }

            throw new NullReferenceException($"Pointer {pointer} has never been registered.");
        }
    }
}