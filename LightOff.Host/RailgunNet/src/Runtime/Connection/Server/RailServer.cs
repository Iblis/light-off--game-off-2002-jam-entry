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
using JetBrains.Annotations;
using RailgunNet.Connection.Traffic;
using RailgunNet.Factory;
using RailgunNet.Logic;
using RailgunNet.Logic.Wrappers;
using RailgunNet.System.Types;
using RailgunNet.Util;
using RailgunNet.Util.Pooling;

namespace RailgunNet.Connection.Server
{
    /// <summary>
    ///     Server is the core executing class on the server. It is responsible for
    ///     managing connection contexts and payload I/O.
    /// </summary>
    [PublicAPI]
    [OnlyIn(Component.Server)]
    public class RailServer : RailConnection
    {
        /// <summary>
        ///     Collection of all participating clients.
        /// </summary>
        private readonly Dictionary<IRailNetPeer, RailServerPeer> clients = new Dictionary<IRailNetPeer, RailServerPeer>();

        /// <summary>
        ///     Collection of all participating clients.
        /// </summary>
        [PublicAPI] [NotNull] public IReadOnlyCollection<RailServerPeer> ConnectedClients => clients.Values;

        /// <summary>
        ///     Entities that have been removed or are about to be.
        /// </summary>
        private readonly Dictionary<EntityId, RailEntityServer> removedEntities = new Dictionary<EntityId, RailEntityServer>();

        private readonly List<RailEntityServer> toRemove = new List<RailEntityServer>(); // Pre-allocated list for reuse

        public RailServer(RailRegistry registry) : base(registry)
        {
        }

        /// <summary>
        ///     The server's room instance. TODO: Multiple rooms?
        /// </summary>
        [CanBeNull]
        public RailServerRoom Room { get; set; }

        /// <summary>
        ///     Starts the server's room.
        /// </summary>
        [PublicAPI]
        public RailServerRoom StartRoom()
        {
            Room = new RailServerRoom(Resource, this);
            SetRoom(Room, Tick.START);
            return Room;
        }

        /// <summary>
        ///     Wraps an incoming connection in a peer and stores it. To be called
        ///     from the network API wrapper.
        /// </summary>
        [PublicAPI]
        public void AddClient(IRailNetPeer netPeer, string identifier)
        {
            if (clients.ContainsKey(netPeer) == false)
            {
                RailServerPeer client = new RailServerPeer(Resource, netPeer, Interpreter)
                {
                    Identifier = identifier
                };
                client.EventReceived += OnEventReceived;
                client.PacketReceived += OnPacketReceived;
                clients.Add(netPeer, client);
                Room.AddClient(client);
                ClientAdded?.Invoke(client);
            }
        }

        [PublicAPI] public event Action<RailServerPeer> ClientAdded;

        /// <summary>
        ///     Wraps an incoming connection in a peer and stores it.
        /// </summary>
        [PublicAPI]
        public void RemoveClient(IRailNetPeer netClient)
        {
            if (clients.ContainsKey(netClient))
            {
                RailServerPeer client = clients[netClient];
                clients.Remove(netClient);
                Room.RemoveClient(client);

                // Revoke control of all the entities controlled by that client
                client.Shutdown();
                ClientRemoved?.Invoke(client);
            }
        }

        [PublicAPI] public event Action<RailServerPeer> ClientRemoved;

        /// <summary>
        ///     Updates all entities and dispatches a snapshot if applicable. Should
        ///     be called once per game simulation tick (e.g. during Unity's
        ///     FixedUpdate pass).
        /// </summary>
        [PublicAPI]
        public override void Update()
        {
            DoStart();

            foreach (RailServerPeer client in clients.Values)
            {
                client.Update(Room.Tick);
            }

            Room.ServerUpdate();
            if (Room.Tick.IsSendTick(RailConfig.SERVER_SEND_RATE))
            {
                Room.StoreStates();
                BroadcastPackets();
            }

            CleanRemovedEntities();
        }

        public void LogRemovedEntity(RailEntityServer entity)
        {
            removedEntities.Add(entity.Id, entity);
        }

        /// <summary>
        ///     Cleans out any removed entities from the removed list
        ///     if they have been acked by all clients.
        /// </summary>
        private void CleanRemovedEntities()
        {
            // TODO: Retire the Id in all of the views as well?

            foreach (KeyValuePair<EntityId, RailEntityServer> kvp in removedEntities)
            {
                bool canRemove = true;
                EntityId id = kvp.Key;
                RailEntityServer entity = kvp.Value;

                foreach (RailServerPeer peer in clients.Values)
                {
                    Tick lastSent = peer.Scope.GetLastSent(id);
                    if (lastSent.IsValid == false) continue; // Was never sent in the first place

                    Tick lastAcked = peer.Scope.GetLastAckedByClient(id);
                    if (lastAcked.IsValid && lastAcked >= entity.RemovedTick)
                    {
                        continue; // Remove tick was acked by the client
                    }

                    // Otherwise, not safe to remove
                    canRemove = false;
                    break;
                }

                if (canRemove) toRemove.Add(entity);
            }

            toRemove.ForEach(entityToRemove => removedEntities.Remove(entityToRemove.Id));
            toRemove.Clear();
        }

        /// <summary>
        ///     Packs and sends a server-to-client packet to each peer.
        /// </summary>
        private void BroadcastPackets()
        {
            foreach (RailServerPeer clientPeer in clients.Values)
            {
                clientPeer.SendPacket(
                    Room.Tick,
                    Room.Entities.Values.Select(e => e as RailEntityServer),
                    removedEntities.Values);
            }
        }

        #region Packet Receive
        private void OnPacketReceived(RailServerPeer peer, IRailClientPacket packet)
        {
            foreach (RailCommandUpdate update in packet.CommandUpdates)
            {
                ProcessCommandUpdate(peer, update);
            }
        }

        private void ProcessCommandUpdate(RailServerPeer peer, RailCommandUpdate update)
        {
            if (Room.TryGet(update.EntityId, out RailEntityServer entity))
            {
                bool canReceive = entity.Controller == peer && entity.IsRemoving == false;

                if (canReceive)
                {
                    foreach (RailCommand command in update.Commands)
                    {
                        entity.ReceiveCommand(command);
                    }
                }
                else // Can't send commands to that entity, so dump them
                {
                    foreach (RailCommand command in update.Commands)
                    {
                        RailPool.Free(command);
                    }
                }
            }
        }
        #endregion
    }
}
