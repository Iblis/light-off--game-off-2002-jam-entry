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

using System;
using System.Collections.Generic;
using RailgunNet.System.Encoding;
using RailgunNet.Util.Pooling;

namespace RailgunNet.Connection
{
    public class RailPackedListIncoming<T>
        where T : IRailPoolable<T>
    {
        private readonly List<T> received;

        public RailPackedListIncoming()
        {
            received = new List<T>();
        }

        public IEnumerable<T> Received => received;

        public void Clear()
        {
            // We don't free the received values as they will be passed elsewhere
            received.Clear();
        }

        public void Decode(RailBitBuffer buffer, Func<RailBitBuffer, T> decode)
        {
            IEnumerable<T> decoded = buffer.UnpackAll(decode);
            foreach (T delta in decoded)
            {
                received.Add(delta);
            }
        }
    }

    /// <summary>
    ///     Packs a maximum of 255 elements into a bit buffer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RailPackedListOutgoing<T>
        where T : IRailPoolable<T>
    {
        private readonly List<T> pending;
        private readonly List<T> sent;

        public RailPackedListOutgoing()
        {
            pending = new List<T>();
            sent = new List<T>();
        }

        public IEnumerable<T> Pending => pending;
        public IEnumerable<T> Sent => sent;

        /// <summary>
        ///     Discards all elements added as pending, regardless whether they have been sent or not.
        /// </summary>
        public void Clear()
        {
            // Everything in sent is also in pending, so only free pending
            foreach (T value in pending)
            {
                RailPool.Free(value);
            }

            pending.Clear();
            sent.Clear();
        }

        public void AddPending(T value)
        {
            pending.Add(value);
        }

        public void AddPending(IEnumerable<T> values)
        {
            pending.AddRange(values);
        }

        public void Encode(
            RailBitBuffer buffer,
            int maxTotalSize,
            int maxIndividualSize,
            Action<T, RailBitBuffer> encode)
        {
            // TODO: prevent loss of data if there are too many pending or the individual size is too large.
            buffer.PackToSize(
                maxTotalSize,
                maxIndividualSize,
                pending,
                encode,
                val => sent.Add(val));
        }
    }
}
