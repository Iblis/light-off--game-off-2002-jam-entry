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
using RailgunNet.Connection.Traffic;
using RailgunNet.Factory;
using RailgunNet.Logic;
using RailgunNet.Logic.Wrappers;
using RailgunNet.System.Types;
using RailgunNet.Util;

namespace RailgunNet.Connection.Server
{
    /// <summary>
    ///     A peer created by the server representing a connected client.
    /// </summary>
    [OnlyIn(Component.Server)]
    public class RailServerPeer : RailPeer<RailPacketFromClient, RailPacketToClient>
    {
        public RailServerPeer(
            RailResource resource,
            IRailNetPeer netPeer,
            RailInterpreter interpreter) : base(
            resource,
            netPeer,
            ExternalEntityVisibility.Scoped,
            RailConfig.CLIENT_SEND_RATE,
            interpreter)
        {
        }

        /// <summary>
        ///     A connection identifier string. (TODO: Temporary)
        /// </summary>
        public string Identifier { get; set; }

        public event Action<RailServerPeer, IRailClientPacket> PacketReceived;

        public void SendPacket(
            Tick localTick,
            IEnumerable<RailEntityServer> active,
            IEnumerable<RailEntityServer> removed)
        {
            RailPacketToClient packetToClient = PrepareSend<RailPacketToClient>(localTick);
            Scope.PopulateDeltas(localTick, packetToClient, active, removed);
            base.SendPacket(packetToClient);

            foreach (RailStateDelta delta in packetToClient.Sent)
            {
                Scope.RegisterSent(delta.EntityId, localTick, delta.IsFrozen);
            }
        }

        protected override void ProcessPacket(RailPacketIncoming packetBase, Tick localTick)
        {
            base.ProcessPacket(packetBase, localTick);

            RailPacketFromClient clientPacket = (RailPacketFromClient) packetBase;
            Scope.IntegrateAcked(clientPacket.View);
            PacketReceived?.Invoke(this, clientPacket);
        }

        public void GrantControl(RailEntityServer entity)
        {
            GrantControlInternal(entity);
        }

        public void RevokeControl(RailEntityServer entity)
        {
            RevokeControlInternal(entity);
        }
    }
}
