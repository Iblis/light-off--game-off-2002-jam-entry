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

using RailgunNet.Factory;
using RailgunNet.System.Encoding;
using RailgunNet.System.Encoding.Compressors;
using RailgunNet.System.Types;
using RailgunNet.Util;
using RailgunNet.Util.Pooling;

namespace RailgunNet.Logic
{
    public enum RailPolicy
    {
        [OnlyIn(Component.Server)] NoProxy,
        [OnlyIn(Component.Client)] NoFrozen
    }

    /// <summary>
    ///     Events are sent attached to entities and represent temporary changes
    ///     in status. They can be sent to specific controllers or broadcast to all
    ///     controllers for whom the entity is in scope.
    /// </summary>
    public abstract class RailEvent : IRailPoolable<RailEvent>
    {
        private RailEventDataSerializer DataSerializer;

        public int FactoryType { get; set; }

        // Synchronized
        public SequenceId EventId { get; set; }

        // Local only
        public int Attempts { get; set; }

        private RailRoom Room { get; set; }

        private RailController Sender { get; set; }

        public TEntity Find<TEntity>(EntityId id, RailPolicy? policy = null)
            where TEntity : RailEntityBase
        {
            if (Room == null) return null;
            if (id.IsValid == false) return null;

            if (policy.HasValue && policy == RailPolicy.NoProxy && Sender == null) return null;

            if (Room.TryGet(id, out RailEntityBase entity) == false) return null;

            if (policy.HasValue && policy == RailPolicy.NoFrozen && entity.IsFrozen) return null;

            if (policy.HasValue && policy == RailPolicy.NoProxy && entity.Controller != Sender) return null;

            if (entity is TEntity cast) return cast;
            return null;
        }

        public bool TryFind<TEntity>(EntityId id, out TEntity entity, RailPolicy? policy = null)
            where TEntity : RailEntityBase
        {
            entity = Find<TEntity>(id, policy);
            return entity != null;
        }

        protected virtual bool Validate()
        {
            return true;
        }

        protected abstract void Execute(RailRoom room, RailController sender);

        public void Free()
        {
            RailPool.Free(this);
        }

        public RailEvent Clone(RailResource resource)
        {
            RailEvent clone = resource.CreateEvent(FactoryType);
            clone.EventId = EventId;
            clone.Attempts = Attempts;
            clone.Room = Room;
            clone.Sender = Sender;
            clone.DataSerializer.SetDataFrom(DataSerializer);
            return clone;
        }

        public void Invoke(RailRoom room, RailController sender)
        {
            Room = room;
            Sender = sender;
            if (Validate()) Execute(room, sender);
        }

        public void RegisterSent()
        {
            if (Attempts > 0) Attempts--;
        }

        public void RegisterSkip()
        {
            RegisterSent();
        }

        #region Pooling
        IRailMemoryPool<RailEvent> IRailPoolable<RailEvent>.Pool { get; set; }

        void IRailPoolable<RailEvent>.Reset()
        {
            EventId = SequenceId.Invalid;
            Attempts = 0;
            Room = null;
            Sender = null;
            DataSerializer.ResetData();
        }

        void IRailPoolable<RailEvent>.Allocated()
        {
            DataSerializer = new RailEventDataSerializer(this);
        }
        #endregion

        #region Encode/Decode/etc.
        /// <summary>
        ///     Note that the packetTick may not be the tick this event was created on
        ///     if we're re-trying to send this event in subsequent packets. This tick
        ///     is intended for use in tick diffs for compression.
        /// </summary>
        public void Encode(RailIntCompressor compressor, RailBitBuffer buffer, Tick packetTick)
        {
            // Write: [EventType]
            buffer.WriteInt(compressor, FactoryType);

            // Write: [EventId]
            buffer.WriteSequenceId(EventId);

            // Write: [EventData]
            DataSerializer.WriteData(buffer, packetTick);
        }

        /// <summary>
        ///     Note that the packetTick may not be the tick this event was created on
        ///     if we're re-trying to send this event in subsequent packets. This tick
        ///     is intended for use in tick diffs for compression.
        /// </summary>
        public static RailEvent Decode(
            IRailEventConstruction eventCreator,
            RailIntCompressor compressor,
            RailBitBuffer buffer,
            Tick packetTick)
        {
            // Read: [EventType]
            int factoryType = buffer.ReadInt(compressor);

            RailEvent evnt = eventCreator.CreateEvent(factoryType);

            // Read: [EventId]
            evnt.EventId = buffer.ReadSequenceId();

            // Read: [EventData]
            evnt.DataSerializer.ReadData(buffer, packetTick);

            return evnt;
        }
        #endregion
    }
}
