using System;
using System.Collections.Generic;

namespace Unmanaged
{
    public class Pool<T> where T : class, new()
    {
        private readonly Queue<T> queue = new();
        private readonly Func<T> factory;

        public Pool(Func<T> factory)
        {
            this.factory = factory;
        }

        public T Rent()
        {
            if (queue is null)
            {
                throw new InvalidOperationException("Pool has been disposed, unable to rent");
            }

            if (queue.Count > 0)
            {
                T instance = queue.Dequeue();
                return instance ?? factory();
            }
            else
            {
                return factory();
            }
        }

        public void Return(T instance)
        {
            if (queue is null)
            {
                throw new InvalidOperationException("Pool has been disposed, unable to return");
            }

            queue.Enqueue(instance);
        }
    }
}