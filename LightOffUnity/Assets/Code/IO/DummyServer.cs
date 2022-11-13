// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Messaging;
using LightOff.IO.Entity;
using RailgunNet.Connection.Server;
using RailgunNet.Factory;
using System.Linq;

namespace LightOff.IO
{
    internal class DummyServer
    {
        public DummyServer()
        {
            _registry.AddEntityType<EntityServer, EntityState>();
            _registry.SetCommandType<MoveCommand>();
            _server = new RailServer(_registry);
            _server.ClientAdded += OnClientAdded;
            _room = _server.StartRoom();
        }

        public void Connect(DummyPeer clientSidePeer)
        {
            var serverSidePeer = new DummyPeer();
            serverSidePeer.ReceivingPeer = clientSidePeer;
            clientSidePeer.ReceivingPeer = serverSidePeer;
            _server.AddClient(serverSidePeer, "dummy");
            var newClient = _server.ConnectedClients.Last();
        }

        private void OnClientAdded(RailServerPeer client)
        {
            EntityServer entityServerSide = _room.AddNewEntity<EntityServer>();
            entityServerSide.AssignController(client);
            entityServerSide.World = _world;
            // TODO: evaluate if ghost data should be send to client
            //client.Scope.Evaluator = new GhostScopeEvaluator(entityServerSide);
        }

        public void Update()
        {
            _server.Update();
        }

        readonly ServerWorld _world = new();
        readonly RailRegistry _registry = new RailRegistry(RailgunNet.Component.Server);
        readonly RailServer _server;
        readonly RailServerRoom _room;
    }
}
