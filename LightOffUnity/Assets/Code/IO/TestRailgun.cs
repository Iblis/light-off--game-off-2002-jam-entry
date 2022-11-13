// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Messaging;
using LightOff.IO.Entity;
using RailgunNet.Connection.Client;
using RailgunNet.Factory;
using VContainer.Unity;

namespace LightOff.IO
{
    public class TestRailgun : ITickable
    {
        public TestRailgun(IEntitySignals signals) 
        {
            _registry.AddEntityType<EntityClient, EntityState>(new []{ signals });
            _registry.SetCommandType<MoveCommand>();
            //_registry.AddEventType<>();
            _client = new RailClient(_registry);
            _room = _client.StartRoom();
            var peerClientSide = new DummyPeer();
            _server.Connect(peerClientSide);
            _client.SetPeer(peerClientSide);
        }

        public void Tick()
        {
            _server.Update();
            _client.Update();   
        }

        static readonly RailRegistry _registry = new (RailgunNet.Component.Client);
        readonly RailClient _client;
        readonly DummyServer _server = new ();
        readonly RailClientRoom _room;
    }
}
