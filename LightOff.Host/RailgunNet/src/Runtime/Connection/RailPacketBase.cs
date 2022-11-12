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
using RailgunNet.Factory;
using RailgunNet.Logic;
using RailgunNet.System.Encoding;
using RailgunNet.System.Types;
using RailgunNet.Util.Pooling;

namespace RailgunNet.Connection
{
    public abstract class RailPacketOutgoing : RailPacketBase
    {
        public abstract void EncodePayload(
            IRailStateConstruction stateCreator,
            RailBitBuffer buffer,
            Tick localTick,
            int reservedBytes);
    }

    public abstract class RailPacketIncoming : RailPacketBase
    {
        public abstract void DecodePayload(
            IRailCommandConstruction commandCreator,
            IRailStateConstruction stateCreator,
            RailBitBuffer buffer);
    }

    public abstract class RailPacketBase : IRailPoolable<RailPacketBase>
    {
        private readonly List<RailEvent> pendingEvents;

        protected RailPacketBase()
        {
            SenderTick = Tick.INVALID;
            LastAckTick = Tick.INVALID;
            LastAckEventId = SequenceId.Invalid;

            pendingEvents = new List<RailEvent>();
            Events = new List<RailEvent>();
            EventsWritten = 0;
        }

        public int EventsWritten { get; set; }

        /// <summary>
        ///     The latest tick from the sender.
        /// </summary>
        public Tick SenderTick { get; set; }

        /// <summary>
        ///     The last tick the sender received.
        /// </summary>
        public Tick LastAckTick { get; set; }

        /// <summary>
        ///     The last event id the sender received.
        /// </summary>
        public SequenceId LastAckEventId { get; set; }

        /// <summary>
        ///     All received events from the sender, in order.
        /// </summary>
        public List<RailEvent> Events { get; }

        public void Initialize(
            Tick senderTick,
            Tick lastAckTick,
            SequenceId lastAckEventId,
            IEnumerable<RailEvent> events)
        {
            SenderTick = senderTick;
            LastAckTick = lastAckTick;
            LastAckEventId = lastAckEventId;

            pendingEvents.AddRange(events);
            EventsWritten = 0;
        }

        public virtual void Reset()
        {
            SenderTick = Tick.INVALID;
            LastAckTick = Tick.INVALID;
            LastAckEventId = SequenceId.Invalid;

            pendingEvents.Clear();
            Events.Clear();
            EventsWritten = 0;
        }

        #region Encoding/Decoding
        public IEnumerable<RailEvent> GetNextEvents()
        {
            for (int i = EventsWritten; i < pendingEvents.Count; i++)
            {
                yield return pendingEvents[i];
            }
        }
        #endregion

        #region Pooling
        IRailMemoryPool<RailPacketBase> IRailPoolable<RailPacketBase>.Pool { get; set; }

        void IRailPoolable<RailPacketBase>.Reset()
        {
            Reset();
        }

        void IRailPoolable<RailPacketBase>.Allocated()
        {
        }
        #endregion
    }
}
