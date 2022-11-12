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
using JetBrains.Annotations;
using RailgunNet.Connection.Traffic;
using RailgunNet.Factory;
using RailgunNet.Logic;
using RailgunNet.System;
using RailgunNet.System.Encoding;
using RailgunNet.System.Types;
using RailgunNet.Util.Debug;
using RailgunNet.Util.Pooling;

namespace RailgunNet.Connection
{
    public delegate void EventReceived(RailEvent evnt, RailPeer sender);

    public abstract class RailPeer : RailController
    {
        /// <summary>
        ///     Interpreter for converting byte input to a BitBuffer.
        /// </summary>
        private readonly RailInterpreter interpreter;

        private readonly RailPacketIncoming reusableIncoming;
        private readonly RailPacketOutgoing reusableOutgoing;

        protected RailPeer(
            RailResource resource,
            IRailNetPeer netPeer,
            ExternalEntityVisibility visibility,
            uint remoteSendRate,
            RailInterpreter interpreter,
            RailPacketIncoming reusableIncoming,
            RailPacketOutgoing reusableOutgoing) : base(resource, visibility, netPeer)
        {
            Resource = resource;
            RemoteClock = new RailClock(remoteSendRate);
            this.interpreter = interpreter;

            outgoingEvents = new Queue<RailEvent>();
            this.reusableIncoming = reusableIncoming;
            this.reusableOutgoing = reusableOutgoing;
            lastQueuedEventId = SequenceId.Start.Next;
            eventHistory = new RailHistory(RailConfig.HISTORY_CHUNKS);

            LocalTick = Tick.START;
            netPeer.PayloadReceived += OnPayloadReceived;
        }

        /// <summary>
        ///     An estimator for the remote peer's current tick.
        /// </summary>
        [PublicAPI]
        [NotNull]
        public RailClock RemoteClock { get; private set; }

        /// <summary>
        ///     Our local tick. Set during update.
        /// </summary>
        [PublicAPI]
        public Tick LocalTick { get; private set; }

        /// <summary>
        ///     The last event id this peer received.
        /// </summary>
        [PublicAPI]
        public SequenceId LastAckEventId {  get; private set; }

        protected RailResource Resource { get; }

        public override Tick EstimatedRemoteTick => RemoteClock.EstimatedRemote;

        public event EventReceived EventReceived;

        public virtual void Update(Tick localTick)
        {
            RemoteClock.Update();
            LocalTick = localTick;
        }

        protected void SendPacket(RailPacketOutgoing packet)
        {
            interpreter.SendPacket(Resource, NetPeer, packet);
        }

        private void OnPayloadReceived(IRailNetPeer peer, ArraySegment<byte> buffer)
        {
            try
            {
                RailBitBuffer bitBuffer = interpreter.LoadData(buffer);
                reusableIncoming.Reset();
                reusableIncoming.Decode(Resource, bitBuffer);

                if (bitBuffer.IsFinished)
                {
                    ProcessPacket(reusableIncoming, LocalTick);
                }
                else
                {
                    RailDebug.LogError("Bad packet read, discarding...");
                }
            }
            catch (Exception e)
            {
                RailDebug.LogError("Error during packet read: " + e);
            }
        }

        /// <summary>
        ///     Allocates a packet and writes common boilerplate information to it.
        /// </summary>
        protected T PrepareSend<T>(Tick localTick)
            where T : RailPacketOutgoing
        {
            // It would be best to reset after rather than before, but that's
            // error prone as it would require a second post-send function call.
            reusableOutgoing.Reset();
            reusableOutgoing.Initialize(
                localTick,
                RemoteClock.LatestRemote,
                eventHistory.Latest,
                FilterOutgoingEvents());
            return (T) reusableOutgoing;
        }

        /// <summary>
        ///     Records acknowledging information for the packet.
        /// </summary>
        protected virtual void ProcessPacket(RailPacketIncoming packetBase, Tick localTick)
        {
            RemoteClock.UpdateLatest(packetBase.SenderTick);
            foreach (RailEvent evnt in FilterIncomingEvents(packetBase.Events))
            {
                ProcessEvent(evnt);
            }

            CleanOutgoingEvents(packetBase.LastAckEventId);
        }

        #region Event-Related
        /// <summary>
        ///     Used for uniquely identifying outgoing events.
        /// </summary>
        private SequenceId lastQueuedEventId;

        /// <summary>
        ///     A rolling queue for outgoing reliable events, in order.
        /// </summary>
        private readonly Queue<RailEvent> outgoingEvents;

        /// <summary>
        ///     A history buffer of received events.
        /// </summary>
        private readonly RailHistory eventHistory;
        
        /// <summary>
        ///     Queues an event to send directly to this peer (used internally).
        /// </summary>
        internal SequenceId SendEvent([NotNull] RailEvent evnt, ushort attempts, bool bMakeCopy)
        {
            RailEvent toSend = evnt;
            if (bMakeCopy)
            {
                toSend = evnt.Clone(Resource);
            }
            toSend.EventId = lastQueuedEventId;
            toSend.Attempts = attempts;

            outgoingEvents.Enqueue(toSend);
            lastQueuedEventId = lastQueuedEventId.Next;
            return toSend.EventId;
        }

        /// <summary>
        ///     Removes any acked or expired outgoing events.
        /// </summary>
        private void CleanOutgoingEvents(SequenceId ackedEventId)
        {
            if (ackedEventId.IsValid == false) return;

            LastAckEventId = ackedEventId;

            // Stop attempting to send acked events
            foreach (RailEvent evnt in outgoingEvents)
            {
                if (evnt.EventId <= ackedEventId)
                {
                    evnt.Attempts = 0;
                }
            }

            // Clean out any events with zero attempts left
            while (outgoingEvents.Count > 0)
            {
                if (outgoingEvents.Peek().Attempts > 0) break;
                RailPool.Free(outgoingEvents.Dequeue());
            }
        }

        /// <summary>
        ///     Selects outgoing events to send.
        /// </summary>
        private IEnumerable<RailEvent> FilterOutgoingEvents()
        {
            // The receiving client can only store a limited size sequence history
            // of events in its received buffer, and will skip any events older than
            // its latest received minus that history length, including reliable
            // events. In order to make sure we don't force the client to skip an
            // event with attempts remaining, we will throttle the outgoing events
            // if we've been sending them too fast. For example, if we have a live 
            // event with ID 3 pending, and a maximum history length of 64 (see
            // RailConfig.HISTORY_CHUNKS) then the highest ID we can send would be 
            // ID 67. Were we to send an event with ID 68, then the client may ignore
            // ID 3 when it comes in for being too old, even though it's still live.
            //
            // In practice this shouldn't be a problem unless we're sending way 
            // more events than is reasonable(/possible) in a single packet, or 
            // something is wrong with reliable event acking. You can always increase
            // the number of history chunks if this becomes an issue.

            SequenceId firstId = SequenceId.Invalid;
            foreach (RailEvent evnt in outgoingEvents)
            {
                // Ignore dead events, they'll be cleaned up eventually
                if (evnt.Attempts <= 0) continue;

                // Don't send an event if it's out of scope for this peer
                if (Scope != null && Scope.Includes(evnt) == false)
                {
                    // Skipping due to out of scope counts as an attempt
                    evnt.RegisterSkip();
                    continue;
                }

                if (firstId.IsValid == false) firstId = evnt.EventId;
                RailDebug.Assert(firstId <= evnt.EventId);

                if (eventHistory.AreInRange(firstId, evnt.EventId) == false)
                {
                    RailDebug.LogWarning("Throttling events due to lack of ack");
                    break;
                }

                yield return evnt;
            }
        }

        /// <summary>
        ///     Gets all events that we haven't processed yet, in order with no gaps.
        /// </summary>
        private IEnumerable<RailEvent> FilterIncomingEvents(IEnumerable<RailEvent> events)
        {
            foreach (RailEvent evnt in events)
            {
                if (eventHistory.IsNewId(evnt.EventId))
                {
                    yield return evnt;
                }
            }
        }

        /// <summary>
        ///     Handles the execution of an incoming event.
        /// </summary>
        private void ProcessEvent(RailEvent evnt)
        {
            EventReceived?.Invoke(evnt, this);
            eventHistory.Store(evnt.EventId);
        }
        #endregion
    }

    public class RailPeer<TIncoming, TOutgoing> : RailPeer
        where TIncoming : RailPacketIncoming, new()
        where TOutgoing : RailPacketOutgoing, new()
    {
        protected RailPeer(
            RailResource resource,
            IRailNetPeer netPeer,
            ExternalEntityVisibility visibility,
            uint remoteSendRate,
            RailInterpreter interpreter) : base(
            resource,
            netPeer,
            visibility,
            remoteSendRate,
            interpreter,
            new TIncoming(),
            new TOutgoing())
        {
        }
    }
}
