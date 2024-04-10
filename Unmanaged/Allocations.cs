using System;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentQueue<nint> instances = [];
        private static readonly ConcurrentDictionary<nint, StackTrace> allocations = [];
        private static readonly ConcurrentDictionary<nint, StackTrace> disposals = [];

        /// <summary>
        /// <c>true</c> if there are any instances allocated at the moment.
        /// </summary>
        public static bool Any => !instances.IsEmpty;

        public static IEnumerable<(object, StackTrace)> All
        {
            get
            {
                foreach (nint pointer in instances)
                {
                    yield return (pointer, allocations[pointer]);
                }
            }
        }

        static Allocations()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                if (!instances.IsEmpty)
                {
                    StringBuilder leaks = new();
                    leaks.Append(instances.Count);
                    leaks.Append(" unmanaged instance(s) were not disposed: ");
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

                    throw new InvalidOperationException(leaks.ToString());
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
                        throw new InvalidOperationException($"Pointer {pointer} has already been registered at:\n{previousStackTrace}.");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Pointer {pointer} has already been registered.");
                    }
                }
            }

            StackTrace stackTrace = new(1, true);
            instances.Enqueue(pointer);
            allocations.TryAdd(pointer, stackTrace);
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
                throw new NullReferenceException($"Pointer {pointer} has not been registered.");
            }

            StackTrace stackTrace = new(1, true);
            HashSet<nint> tempInstances = new(instances);
            instances.Clear();
            foreach (nint existingInstance in tempInstances)
            {
                if (existingInstance.Equals(pointer))
                {
                    disposals.TryAdd(pointer, stackTrace);
                    if (allocations.TryRemove(pointer, out StackTrace? removedStackTrace))
                    {
                        GC.SuppressFinalize(removedStackTrace);
                    }
                    else
                    {
                        //impossible exception?
                        throw new Exception($"Pointer {pointer} has not been allocated.");
                    }
                }
                else
                {
                    instances.Enqueue(existingInstance);
                }
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfNull(nint pointer)
        {
            foreach (nint existingInstance in instances)
            {
                if (existingInstance.Equals(pointer))
                {
                    return;
                }
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

            throw new InvalidOperationException($"Pointer {pointer} has not been registered.");
        }
    }
}