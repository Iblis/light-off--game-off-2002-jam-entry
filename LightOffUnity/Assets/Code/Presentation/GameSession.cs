// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Cysharp.Threading.Tasks;
using LightOff.ClientLogic;
using LightOff.Messaging;
using MessagePipe;
using RailgunNet.Connection.Client;
using System;
using System.Threading;

namespace LightOff.Presentation
{
    public class GameSession : IGameSession
    {
        public IReadOnlyAsyncReactiveProperty<SessionConnectionState> SessionState { get; }

        public GameSession(ISessionConnection connection, ISubscriber<EventMessage> eventMessageSubscriber)
        {
            _connection = connection;
            _eventMessageSubscription = eventMessageSubscriber.Subscribe(OnEventMessage);
            SessionState = _connectionState.ToReadOnlyAsyncReactiveProperty(CancellationToken.None);
        }

        private void OnEventMessage(EventMessage eventMessage)
        {
            if(eventMessage.EventMessageType == EventMessageType.MatchStarted)
            {
                _connectionState.Value = SessionConnectionState.InMatch;
            }
            else if(eventMessage.EventMessageType == EventMessageType.MatchEnded)
            {
                _connectionState.Value = SessionConnectionState.AfterMatch;
            }
        }

        public void Close()
        {
            _connection.Disconnect();
        }

        public async UniTask Join(string sessionName, string playerName)
        {
            _connectionState.Value = SessionConnectionState.Connecting;
            var success = await _connection.ConnectTo(sessionName, playerName);
            if (success)
            {
                AwaitCancellation().Forget();
                _connectionState.Value = SessionConnectionState.Connected;
            }
            else
            {
                _connectionState.Value = SessionConnectionState.Disconnected;
                UnityEngine.Debug.LogError("Connection could not be established");
            }
        }

        async UniTask AwaitCancellation()
        {
            await _connection.CompletionDueToDisconnect;
            await UniTask.SwitchToMainThread();
            CloseGameSession();
        }

        void CloseGameSession()
        {
            if (_connectionState.Value != SessionConnectionState.Disconnected)
            {
                _connection.Disconnect();
                _connectionState.Value = SessionConnectionState.Disconnected;
            }
        }

        public void SetReadyState(bool value)
        {
            _connection.SetReadyState(value);
        }

        public void Update()
        {
            _connection.Update();
        }

        readonly AsyncReactiveProperty<SessionConnectionState> _connectionState = new AsyncReactiveProperty<SessionConnectionState>(SessionConnectionState.Disconnected);
        readonly ISessionConnection _connection;
        readonly RailClient _client;
        readonly IDisposable _eventMessageSubscription;
    }
}
