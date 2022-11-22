// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Cysharp.Threading;
using LightOff.Host.Client;
using LightOff.Logic;
using RailgunNet.Connection.Server;

namespace LightOff.Host.Session
{
    internal class HostedGameSession : IHostedGameSession
    {
        public string SessionName { get; init; }
        public bool CanBeJoined => true;

        public HostedGameSession(string sessionName, RailServer server, IWorld world)
        {
            SessionName = sessionName;
            _world = world;
            _commandHandler = new Messaging.Server.CommandHandler(_world);
            _server = server;
            _server.ClientAdded += OnClientAdded;
            _room = _server.StartRoom();
        }

        public void Join(IClient client)
        {
            _server.AddClient(client, client.PlayerName);
        }

        private void OnClientAdded(RailServerPeer client)
        {
            Messaging.Server.EntityServer entityServerSide = _room.AddNewEntity<Messaging.Server.EntityServer>();
            entityServerSide.AssignController(client);
            entityServerSide.CommandHandler = _commandHandler;
            entityServerSide.State.Position = new System.Numerics.Vector2(10, 10);
            entityServerSide.State.ExecutesAction = true;
            _world.AddTracker(entityServerSide);
            _world.SetGhost(new DummyGhost());
        }

        internal bool Update(in LogicLooperActionContext ctx)
        {
            _commandHandler.UpdateDeltaTime((float)ctx.ElapsedTimeFromPreviousFrame.TotalSeconds);
            _server.Update();
            // TODO: check if match has ended. If so, return false so that this session can be removed from the gameLoop
            return true;
        }

        readonly RailServer _server;
        readonly RailServerRoom _room;
        readonly IWorld _world;
        readonly Messaging.Server.CommandHandler _commandHandler;
    }
}
