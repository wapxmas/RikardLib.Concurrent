using System;
using System.Collections.Generic;
using System.Threading;

namespace RikardLib.Concurrent
{
    public class ParallelCircleQueue<T>
    {
        private Queue<T> items;

        private object itemsLock = new object();

        public ManualResetEventSlim itemWait = new ManualResetEventSlim(false);

        public ParallelCircleQueue(List<T> init)
        {
            Count = init.Count;

            items = new Queue<T>(init);

            if (items.Count > 0)
            {
                itemWait.Set();
            }
        }

        public void AddItems(List<T> list)
        {
            lock (itemsLock)
            {
                list.ForEach(i => items.Enqueue(i));

                if (items.Count > 0)
                {
                    itemWait.Set();
                }
            }
        }

        public void PutItem(ParallelCircleQueueItem<T> item)
        {
            lock (itemsLock)
            {
                items.Enqueue(item.Item);

                itemWait.Set();
            }
        }

        public ParallelCircleQueueItem<T> GetItem()
        {
            while (true)
            {
                itemWait.Wait();

                lock (itemsLock)
                {
                    try
                    {
                        if (items.Count > 0)
                        {
                            return new ParallelCircleQueueItem<T>(items.Dequeue(), this);
                        }
                    }
                    finally
                    {
                        if (items.Count == 0)
                        {
                            itemWait.Reset();
                        }
                    }
                }
            }
        }

        public int Count { get; private set; }
    }

    public class ParallelCircleQueueItem<T> : IDisposable
    {
        private ParallelCircleQueue<T> queue;

        public ParallelCircleQueueItem(T item, ParallelCircleQueue<T> queue)
        {
            this.Item = item;
            this.queue = queue;
        }

        public T Item { get; set; }

        public void Dispose()
        {
            queue.PutItem(this);
        }
    }
}
