using System.Collections.Generic;
using System.Diagnostics;

namespace Seb.Vis.Internal
{
    public class Pool<T>
    {
        Queue<T> available;
        HashSet<T> inUse;
        System.Func<T> creator;


        public Pool()
        {
            available = new Queue<T>();
            inUse = new HashSet<T>();
        }

        public Pool(System.Func<T> creator)
        {
            available = new Queue<T>();
            inUse = new HashSet<T>();
            this.creator = creator;
        }

        public bool HasAvailable() => available.Count > 0;

        public T GetNextAvailable()
        {
            T pooledItem = available.Dequeue();
            inUse.Add(pooledItem);
            return pooledItem;
        }

        // Gets the next available item, but does not add it to the 'in use' list
        // (effectively removing it from the pool)
        public T PurgeNextAvailable()
        {
            T pooledItem = available.Dequeue();
            return pooledItem;
        }

        public T GetNextAvailableOrCreate()
        {
            if (HasAvailable())
            {
                return GetNextAvailable();
            }

            T newItem;
            if (creator != null)
            {
                newItem = creator.Invoke();
            }
            else
            {
                newItem = System.Activator.CreateInstance<T>();
            }
            inUse.Add(newItem);
            return newItem;
        }

        public void AddNew(T item, bool currentlyInUse)
        {
            if (currentlyInUse) inUse.Add(item);
            else available.Enqueue(item);
        }

        public void Return(T item)
        {
            available.Enqueue(item);
            inUse.Remove(item);
        }

        public void ReturnAll()
        {
            foreach (T items in inUse)
            {
                available.Enqueue(items);
            }
            inUse.Clear();
        }

       

        public int TotalCount => available.Count + inUse.Count;
        public int AvailableCount => available.Count;
        public int InUseCount => inUse.Count;

        public string StatsString()
        {
            return $"In Use: {InUseCount} | Available: {AvailableCount} | Total: {TotalCount}";
        }
    }
}