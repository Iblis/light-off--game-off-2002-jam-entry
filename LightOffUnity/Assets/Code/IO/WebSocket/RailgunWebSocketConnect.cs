// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Cysharp.Threading.Tasks;
using RailgunNet.Connection.Client;
using System;
using System.Threading;
using VContainer.Unity;

namespace LightOff.IO.WebSocket
{
    public class RailgunWebSocketConnect : IAsyncStartable, ITickable
    {
        public RailgunWebSocketConnect(Func<RailClient> clientFactory) 
        {
            _client = clientFactory();
            _peer = new WebSocketPeer(_client);
                        
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            
            _room = await _peer.ConnectTo("localhost:7212", "testX", "GhostTracker");
            if(_room == null)
            {
                UnityEngine.Debug.LogError("Unable to start Client Room");
            }
        }

        public void Tick()
        {
            _client.Update();
        }

        RailClientRoom _room;
        readonly WebSocketPeer _peer;
        readonly RailClient _client;
    }
}
