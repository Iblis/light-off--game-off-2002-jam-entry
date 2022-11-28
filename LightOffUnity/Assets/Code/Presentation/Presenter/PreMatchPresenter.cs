// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using LightOff.ClientLogic;
using LightOff.IO;
using LightOff.Presentation.View;
using System.Threading;
using VContainer.Unity;

namespace LightOff.Presentation.Presenter
{
    internal class PreMatchPresenter : IStartable
    {
        public PreMatchPresenter(ConnectionView connectionView, ReadyView readyView, IGameSession session, IClientWorld world) 
        {
            _connectionView  = connectionView;
            _readyView = readyView;
            _gameSession  = session;
            _world = world;
        }

        public void Start()
        {
            BindElements();
            _gameSession.SessionState.Subscribe(SetViewState);
        }

        private void SetViewState(SessionConnectionState state)
        {
            _connectionView.SetActive(state < SessionConnectionState.Connected);
            _readyView.SetActive(state == SessionConnectionState.Connected);
        }

        private void BindElements()
        {
            _connectionView.ConnectButton.OnClickAsAsyncEnumerable(CancellationToken.None).ForEachAwaitAsync(async _ =>
            {
                if (_connectionView.ConnectButton.enabledSelf)
                {
                    _connectionView.ConnectButton.SetEnabled(false);
                    await _gameSession.Join(_connectionView.MatchNameInput.text, _connectionView.PlayerNameInput.text);
                    _connectionView.ConnectButton.SetEnabled(true);
                }
            });

            _world.LocalPlayer.Subscribe(entity =>
            {
                _readyView.Toggle.SetEnabled(entity != null);
            });

            _readyView.Toggle.OnValueChangedAsAsyncEnumerable(CancellationToken.None).ForEachAsync(value =>
            {
                _gameSession.SetReadyState(value);
                if(value == true)
                {
                    _readyView.WaitingForPlayers.text = "Waiting for other Players...";
                }
                else
                {
                    _readyView.WaitingForPlayers.text = string.Empty;
                }
            });
        }

        readonly ConnectionView _connectionView;
        readonly ReadyView _readyView;
        readonly IGameSession _gameSession;
        readonly IClientWorld _world;
    }
}
