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
using JetBrains.Annotations;
using RailgunNet.Connection.Traffic;
using RailgunNet.Factory;
using RailgunNet.Logic;
using RailgunNet.Logic.Wrappers;
using RailgunNet.System.Types;
using RailgunNet.Util;
using RailgunNet.Util.Debug;
using RailgunNet.Util.Pooling;

namespace RailgunNet.Connection.Client
{
    [PublicAPI]
    [OnlyIn(Component.Client)]
    public class RailClient : RailConnection
    {
        /// <summary>
        ///     The local simulation tick, used for commands
        /// </summary>
        private Tick localTick;

        /// <summary>
        ///     The peer for our connection to the server.
        /// </summary>
        [PublicAPI] [CanBeNull] public RailClientPeer ServerPeer { get; private set; }

        public RailClient(RailRegistry registry) : base(registry)
        {
            ServerPeer = null;
            localTick = Tick.START;
            Room = null;
        }

        /// <summary>
        ///     The client's room instance. TODO: Multiple rooms?
        /// </summary>
        [CanBeNull]
        private RailClientRoom Room { get; set; }

        public RailClientRoom StartRoom()
        {
            Room = new RailClientRoom(Resource, this);
            SetRoom(Room, Tick.INVALID);
            return Room;
        }

        /// <summary>
        ///     Sets the current server peer.
        /// </summary>
        public void SetPeer(IRailNetPeer netPeer)
        {
            if (netPeer == null)
            {
                if (ServerPeer != null)
                {
                    ServerPeer.PacketReceived -= OnPacketReceived;
                    ServerPeer.EventReceived -= OnEventReceived;
                    Disconnected?.Invoke(ServerPeer);
                }

                ServerPeer = null;
            }
            else
            {
                RailDebug.Assert(ServerPeer == null, "Overwriting peer");
                ServerPeer = new RailClientPeer(Resource, netPeer, Interpreter);
                ServerPeer.PacketReceived += OnPacketReceived;
                ServerPeer.EventReceived += OnEventReceived;
                Connected?.Invoke(ServerPeer);
            }
        }

        [PublicAPI] public event Action<RailClientPeer> Connected;
        [PublicAPI] public event Action<RailClientPeer> Disconnected;

        [PublicAPI]
        public override void Update()
        {
            if (ServerPeer != null)
            {
                DoStart();
                ServerPeer.Update(localTick);

                if (Room != null)
                {
                    Room.ClientUpdate(localTick, ServerPeer.EstimatedRemoteTick);

                    int sendRate = RailConfig.CLIENT_SEND_RATE;
                    if (localTick.IsSendTick(sendRate))
                    {
                        ServerPeer.SendPacket(localTick, Room.LocalEntities);
                    }

                    localTick++;
                }
            }
        }

        private void OnPacketReceived(RailPacketFromServer packet)
        {
            if (Room == null)
            {
                foreach (RailStateDelta delta in packet.Deltas)
                {
                    RailPool.Free(delta);
                }
            }
            else
            {
                foreach (RailStateDelta delta in packet.Deltas)
                {
                    if (Room.ProcessDelta(delta) == false)
                    {
                        RailPool.Free(delta);
                    }
                }
            }

            foreach (RailEvent @event in packet.Events)
            {
                @event.Free();
            }
        }
    }
}
