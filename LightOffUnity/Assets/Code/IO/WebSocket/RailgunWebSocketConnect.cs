// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Cysharp.Threading.Tasks;
using LightOff.ClientLogic;
using LightOff.Messaging;
using RailgunNet.Connection.Client;
using System;
using System.Linq;

namespace LightOff.IO.WebSocket
{
    public class RailgunWebSocketConnect : ISessionConnection
    {
        public RailgunWebSocketConnect(Func<RailClient> clientFactory) 
        {
            _peer = new WebSocketPeer(clientFactory);                     
        }

        public async UniTask<bool> ConnectTo(string sessionName, string playerName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var hostName = "lightoff.mudmatch.de";
#else
            var hostName = "localhost:7212";
#endif
            var connectionResult = await _peer.ConnectTo(hostName, sessionName, playerName);
            if(connectionResult.Success)
            {
                _client = connectionResult.Client;
                _room = connectionResult.Room;
            }
            return connectionResult.Success;
        }

        public void Disconnect()
        {
            _peer.Disconnect();
            if (_client != null)
            {
                _client.ServerPeer.Shutdown();
                _client.SetPeer(null);
                _client = null;
                _room = null;
            }
        }

        public void SetReadyState(bool value)
        {
            var sourceEntity = _room.LocalEntities.First();
            _room.RaiseEvent<EventMessage>(evt =>
            {
                evt.EventMessageType = EventMessageType.PlayerReady;
                evt.SourceId = sourceEntity.Id;
            });
        }

        public void Update()
        {
            if(_client != null)
            {
                _client.Update();
            }
        }

        RailClientRoom _room;
        RailClient _client;
        readonly WebSocketPeer _peer;

        public UniTask CompletionDueToDisconnect => _peer.CompletionDueToDisconnect;
    }
}
