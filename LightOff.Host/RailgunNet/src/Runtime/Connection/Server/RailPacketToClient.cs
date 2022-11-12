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
using RailgunNet.Logic.Wrappers;
using RailgunNet.System.Encoding;
using RailgunNet.System.Types;
using RailgunNet.Util;

namespace RailgunNet.Connection.Server
{
    /// <summary>
    ///     Packet sent from server to client. Corresponding packet on client
    ///     side is RailPacketFromServer.
    /// </summary>
    [OnlyIn(Component.Server)]
    public sealed class RailPacketToClient : RailPacketOutgoing
    {
        private readonly RailPackedListOutgoing<RailStateDelta> deltas;

        public RailPacketToClient()
        {
            deltas = new RailPackedListOutgoing<RailStateDelta>();
        }

        public IEnumerable<RailStateDelta> Sent => deltas.Sent;

        public override void Reset()
        {
            base.Reset();

            deltas.Clear();
        }

        public void Populate(
            IEnumerable<RailStateDelta> activeDeltas,
            IEnumerable<RailStateDelta> frozenDeltas,
            IEnumerable<RailStateDelta> removedDeltas)
        {
            deltas.AddPending(removedDeltas);
            deltas.AddPending(frozenDeltas);
            deltas.AddPending(activeDeltas);
        }

        #region Encode/Decode
        public override void EncodePayload(
            IRailStateConstruction stateCreator,
            RailBitBuffer buffer,
            Tick localTick,
            int reservedBytes)
        {
            // Write: [Deltas]
            EncodeDeltas(stateCreator, buffer, reservedBytes);
        }

        private void EncodeDeltas(
            IRailStateConstruction stateCreator,
            RailBitBuffer buffer,
            int reservedBytes)
        {
            deltas.Encode(
                buffer,
                RailConfig.PACKCAP_MESSAGE_TOTAL - reservedBytes,
                RailConfig.MAXSIZE_ENTITY,
                (delta, buf) => RailStateDeltaSerializer.EncodeDelta(stateCreator, buf, delta));
        }
        #endregion
    }
}
