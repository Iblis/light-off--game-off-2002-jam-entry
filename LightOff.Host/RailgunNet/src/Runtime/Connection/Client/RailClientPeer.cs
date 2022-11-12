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
using System.Linq;
using RailgunNet.Connection.Traffic;
using RailgunNet.Factory;
using RailgunNet.Logic;
using RailgunNet.Logic.Wrappers;
using RailgunNet.System;
using RailgunNet.System.Types;
using RailgunNet.Util;

namespace RailgunNet.Connection.Client
{
    /// <summary>
    ///     The peer created by the client representing the server.
    /// </summary>
    [OnlyIn(Component.Client)]
    public class RailClientPeer : RailPeer<RailPacketFromServer, RailPacketToServer>
    {
        private readonly RailView localView;

        public RailClientPeer(
            RailResource resource,
            IRailNetPeer netPeer,
            RailInterpreter interpreter) : base(
            resource,
            netPeer,
            ExternalEntityVisibility.All,
            RailConfig.SERVER_SEND_RATE,
            interpreter)
        {
            localView = new RailView();
        }

        public event Action<RailPacketFromServer> PacketReceived;

        public void SendPacket(Tick localTick, IEnumerable<RailEntityBase> controlledEntities)
        {
            // TODO: Sort controlledEntities by most recently sent

            RailPacketToServer packet = PrepareSend<RailPacketToServer>(localTick);
            packet.Populate(ProduceCommandUpdates(controlledEntities), localView);

            // Send the packet
            base.SendPacket(packet);

            foreach (RailCommandUpdate commandUpdate in packet.Sent)
            {
                commandUpdate.Entity.LastSentCommandTick = localTick;
            }
        }

        protected override void ProcessPacket(RailPacketIncoming packetBase, Tick localTick)
        {
            base.ProcessPacket(packetBase, localTick);

            RailPacketFromServer packetFromServer = (RailPacketFromServer) packetBase;
            foreach (RailStateDelta delta in packetFromServer.Deltas)
            {
                localView.RecordUpdate(
                    delta.EntityId,
                    packetBase.SenderTick,
                    localTick,
                    delta.IsFrozen);
            }

            PacketReceived?.Invoke(packetFromServer);
        }

        private IEnumerable<RailCommandUpdate> ProduceCommandUpdates(
            IEnumerable<RailEntityBase> entities)
        {
            // If we have too many entities to fit commands for in a packet,
            // we want to round-robin sort them to avoid starvation
            return entities.Select(e => e as RailEntityClient)
                           .OrderBy(e => e.LastSentCommandTick, Tick.CreateComparer())
                           .Select(e => RailCommandUpdate.Create(Resource, e, e.OutgoingCommands));
        }
    }
}
