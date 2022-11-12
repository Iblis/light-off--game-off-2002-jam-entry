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
using RailgunNet.System.Buffer;
using RailgunNet.System.Types;
using RailgunNet.Util.Pooling;

namespace RailgunNet.Logic.Wrappers
{
    public class RailStateDelta : IRailPoolable<RailStateDelta>, IRailTimedValue
    {
        private RailState state;

        public RailStateDelta()
        {
            Reset();
        }

        public Tick Tick { get; private set; }

        public EntityId EntityId { get; private set; }

        public RailState State => state;
        public bool IsFrozen { get; private set; }

        public bool HasControllerData => state.HasControllerData;
        public bool HasImmutableData => state.HasImmutableData;
        public bool IsRemoving => RemovedTick.IsValid;
        public Tick RemovedTick { get; private set; }
        public Tick CommandAck { get; private set; } // Controller only

        #region Interface
        Tick IRailTimedValue.Tick => Tick;
        #endregion

        public static RailStateDelta CreateFrozen(
            IRailStateConstruction stateCreator,
            Tick tick,
            EntityId entityId)
        {
            RailStateDelta delta = stateCreator.CreateDelta();
            delta.Initialize(tick, entityId, null, Tick.INVALID, Tick.INVALID, true);
            return delta;
        }

        public RailEntityBase ProduceEntity(RailResource resource)
        {
            return RailEntityBase.Create(resource, state.FactoryType);
        }

        public void Initialize(
            Tick tick,
            EntityId entityId,
            RailState state,
            Tick removedTick,
            Tick commandAck,
            bool isFrozen)
        {
            Tick = tick;
            EntityId = entityId;
            this.state = state;
            RemovedTick = removedTick;
            CommandAck = commandAck;
            IsFrozen = isFrozen;
        }

        private void Reset()
        {
            Tick = Tick.INVALID;
            EntityId = EntityId.INVALID;
            RailPool.SafeReplace(ref state, null);
            IsFrozen = false;
        }

        #region Pooling
        IRailMemoryPool<RailStateDelta> IRailPoolable<RailStateDelta>.Pool { get; set; }

        void IRailPoolable<RailStateDelta>.Reset()
        {
            Reset();
        }

        void IRailPoolable<RailStateDelta>.Allocated()
        {
        }
        #endregion
    }
}
