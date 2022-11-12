/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016-2018 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
 */

using System.Collections.Generic;
using RailgunNet.System.Types;
using RailgunNet.Util.Pooling;

namespace RailgunNet.System.Buffer
{
    /// <summary>
    ///     A rolling queue that maintains entries in order. Designed to access
    ///     the entry at a given tick, or the most recent entry before it.
    /// </summary>
    public class RailQueueBuffer<T>
        where T : class, IRailTimedValue, IRailPoolable<T>
    {
        private readonly int capacity;

        private readonly Queue<T> data;

        public RailQueueBuffer(int capacity)
        {
            Latest = null;
            this.capacity = capacity;
            data = new Queue<T>();
        }

        public T Latest { get; private set; }

        private static IEnumerable<T> Remainder(T latest, Queue<T>.Enumerator enumerator)
        {
            yield return latest;
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        public void Store(T val)
        {
            if (data.Count >= capacity) RailPool.Free(data.Dequeue());
            data.Enqueue(val);
            Latest = val;
        }

        /// <summary>
        ///     Returns the first value with a tick less than or equal to the given
        ///     tick, followed by all subsequent values stored. If no value has a tick
        ///     less than or equal to the given one, this function returns null.
        /// </summary>
        public IEnumerable<T> LatestFrom(Tick tick)
        {
            if (tick.IsValid == false) return null;

            Queue<T>.Enumerator head = data.GetEnumerator();
            Queue<T>.Enumerator tail = data.GetEnumerator();

            // Find the value at the given tick. TODO: Binary search?
            T latest = null;

            while (head.MoveNext())
            {
                if (head.Current.Tick <= tick)
                {
                    latest = head.Current;
                }
                else
                {
                    break;
                }

                tail.MoveNext();
            }

            if (latest == null) return null;
            return Remainder(latest, tail);
        }

        /// <summary>
        ///     Clears the buffer, freeing all contents.
        /// </summary>
        public void Clear()
        {
            foreach (T val in data)
            {
                RailPool.Free(val);
            }

            data.Clear();
            Latest = null;
        }
    }
}
