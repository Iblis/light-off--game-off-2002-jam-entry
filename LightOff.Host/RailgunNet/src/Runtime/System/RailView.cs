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

namespace RailgunNet.System
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public readonly struct RailViewEntry
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public static readonly RailViewEntry INVALID = new RailViewEntry(
            Tick.INVALID,
            Tick.INVALID,
            true);

        public bool IsValid => lastReceivedTick.IsValid;
        public Tick LastReceivedTick => lastReceivedTick;
        public Tick LocalUpdateTick { get; }

        public bool IsFrozen { get; }

        private readonly Tick lastReceivedTick;

        public RailViewEntry(Tick lastReceivedTick, Tick localUpdateTick, bool isFrozen)
        {
            this.lastReceivedTick = lastReceivedTick;
            LocalUpdateTick = localUpdateTick;
            IsFrozen = isFrozen;
        }
    }

    public class RailView
    {
        private readonly Dictionary<EntityId, RailViewEntry> latestUpdates;
        private readonly List<KeyValuePair<EntityId, RailViewEntry>> sortList;

        private readonly ViewComparer viewComparer;

        public RailView()
        {
            viewComparer = new ViewComparer();
            latestUpdates = new Dictionary<EntityId, RailViewEntry>();
            sortList = new List<KeyValuePair<EntityId, RailViewEntry>>();
        }

        /// <summary>
        ///     Returns the latest tick the peer has acked for this entity ID.
        /// </summary>
        public RailViewEntry GetLatest(EntityId id)
        {
            if (latestUpdates.TryGetValue(id, out RailViewEntry result)) return result;
            return RailViewEntry.INVALID;
        }

        public void Clear()
        {
            latestUpdates.Clear();
        }

        /// <summary>
        ///     Records an acked status from the peer for a given entity ID.
        /// </summary>
        public void RecordUpdate(
            EntityId entityId,
            Tick receivedTick,
            Tick localTick,
            bool isFrozen)
        {
            RecordUpdate(entityId, new RailViewEntry(receivedTick, localTick, isFrozen));
        }

        /// <summary>
        ///     Records an acked status from the peer for a given entity ID.
        /// </summary>
        public void RecordUpdate(EntityId entityId, RailViewEntry entry)
        {
            if (latestUpdates.TryGetValue(entityId, out RailViewEntry currentEntry))
            {
                if (currentEntry.LastReceivedTick > entry.LastReceivedTick)
                {
                    return;
                }
            }

            latestUpdates[entityId] = entry;
        }

        public void Integrate(RailView other)
        {
            foreach (KeyValuePair<EntityId, RailViewEntry> pair in other.latestUpdates)
            {
                RecordUpdate(pair.Key, pair.Value);
            }
        }

        /// <summary>
        ///     Views sort in descending tick order. When sending a view to the server
        ///     we send the most recent updated entities since they're the most likely
        ///     to actually matter to the server/client scope.
        /// </summary>
        public IEnumerable<KeyValuePair<EntityId, RailViewEntry>> GetOrdered(Tick localTick)
        {
            sortList.Clear();
            foreach (KeyValuePair<EntityId, RailViewEntry> pair in latestUpdates)
                // If we haven't received an update on an entity for too long, don't
                // bother sending a view for it (the server will update us eventually)
            {
                if (localTick - pair.Value.LocalUpdateTick < RailConfig.VIEW_TICKS)
                {
                    sortList.Add(pair);
                }
            }

            sortList.Sort(viewComparer);
            sortList.Reverse();
            return sortList;
        }

        private class ViewComparer : Comparer<KeyValuePair<EntityId, RailViewEntry>>
        {
            private readonly Comparer<Tick> comparer;

            public ViewComparer()
            {
                comparer = Tick.CreateComparer();
            }

            public override int Compare(
                KeyValuePair<EntityId, RailViewEntry> x,
                KeyValuePair<EntityId, RailViewEntry> y)
            {
                return comparer.Compare(x.Value.LastReceivedTick, y.Value.LastReceivedTick);
            }
        }
    }
}
