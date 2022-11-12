using System.Collections.Generic;
using RailgunNet.Factory;
using RailgunNet.System.Types;
using RailgunNet.Util;

namespace RailgunNet.Logic.Wrappers
{
    public static class RailStateDeltaFactory
    {
        private const uint FLAGS_ALL = 0xFFFFFFFF; // All values different
        private const uint FLAGS_NONE = 0x00000000; // No values different

        /// <summary>
        ///     Creates a delta between a state and a record. If forceUpdate is set
        ///     to false, this function will return null if there is no change between
        ///     the current and basis.
        /// </summary>
        [OnlyIn(Component.Server)]
        public static RailStateDelta Create(
            IRailStateConstruction stateCreator,
            EntityId entityId,
            RailState current,
            IEnumerable<RailStateRecord> basisStates,
            bool includeControllerData,
            bool includeImmutableData,
            Tick commandAck,
            Tick removedTick,
            bool forceAllMutable)
        {
            bool shouldReturn = forceAllMutable ||
                                includeControllerData && current.HasControllerData ||
                                includeImmutableData ||
                                removedTick.IsValid;

            // We don't know what the client has and hasn't received from us since
            // the acked state. As a result, we'll build diff flags across all 
            // states sent *between* the latest and current. This accounts for
            // situations where a value changes and then quickly changes back,
            // while appearing as no change on just the current-latest diff.
            uint flags = 0;
            if (forceAllMutable == false && basisStates != null)
            {
                foreach (RailStateRecord record in basisStates)
                {
                    flags |= current.DataSerializer.CompareMutableData(record.State.DataSerializer);
                }
            }
            else
            {
                flags = FLAGS_ALL;
            }

            if (flags == FLAGS_NONE && !shouldReturn) return null;

            RailState deltaState = stateCreator.CreateState(current.FactoryType);

            deltaState.HasImmutableData = includeImmutableData;
            if (includeImmutableData)
            {
                deltaState.DataSerializer.ApplyImmutableFrom(current.DataSerializer);
            }

            deltaState.Flags = flags;
            deltaState.DataSerializer.ApplyMutableFrom(current.DataSerializer, deltaState.Flags);

            deltaState.HasControllerData = includeControllerData;
            if (includeControllerData)
            {
                deltaState.DataSerializer.ApplyControllerFrom(current.DataSerializer);
            }

            // We don't need to include a tick when sending -- it's in the packet
            RailStateDelta delta = stateCreator.CreateDelta();
            delta.Initialize(Tick.INVALID, entityId, deltaState, removedTick, commandAck, false);
            return delta;
        }
    }
}
