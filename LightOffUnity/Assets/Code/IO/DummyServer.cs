// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Messaging;
using RailgunNet.Connection.Server;
using RailgunNet.Factory;
using LightOff.Logic;
using LightOff.Messaging.Server;
using UnityEngine;

namespace LightOff.IO
{
    public class DummyServer : IServer
    {
        public DummyServer(IWorld world)
        {
            _world = world;
            _commandHandler = new Messaging.Server.CommandHandler(world);
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
            //var newClient = _server.ConnectedClients.Last();
        }

        private void OnClientAdded(RailServerPeer client)
        {
            EntityServer entityServerSide = _room.AddNewEntity<EntityServer>();
            entityServerSide.AssignController(client);
            entityServerSide.CommandHandler = _commandHandler;
            entityServerSide.State.Position = new System.Numerics.Vector2(10, 10);
            entityServerSide.State.ExecuteAction = true;
            _world.AddPlayer(entityServerSide);
            _world.SetGhost(new DummyGhost());
            // TODO: evaluate if ghost data should be send to client
            //client.Scope.Evaluator = new GhostScopeEvaluator(entityServerSide);
        }

        public void Update()
        {
            _commandHandler.UpdateDeltaTime(Time.deltaTime);
            _server.Update();
        }

        readonly IWorld _world;
        readonly Messaging.Server.CommandHandler _commandHandler;
        readonly RailRegistry _registry = new RailRegistry(RailgunNet.Component.Server);
        readonly RailServer _server;
        readonly RailServerRoom _room;
    }
}
