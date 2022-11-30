// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using LightOff.ClientLogic;
using LightOff.IO;
using LightOff.Presentation.View;
using MessagePipe;
using System.Threading;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace LightOff.Presentation.Presenter
{
    internal class PreMatchPresenter : IStartable
    {
        public PreMatchPresenter(ConnectionView connectionView,
                                    ReadyView readyView,
                                    MatchEndedView matchEndedView,
                                    IGameSession session,
                                    IClientWorld world)
        {
            _connectionView = connectionView;
            _readyView = readyView;
            _matchEndedView = matchEndedView;
            _gameSession = session;
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
            _matchEndedView.SetActive(state == SessionConnectionState.AfterMatch);
            if(state == SessionConnectionState.AfterMatch)
            {
                var localPlayer = _world.LocalPlayer.Value;
                var playerIsHealthy = localPlayer.State.Health == 100;
                //UnityEngine.Debug.Log($"Player health: {localPlayer.State.Health} Player Slot: {localPlayer.State.PlayerSlot} HitCoolDown: {localPlayer.State.HitCooldown}");
                var localPlayerIsGhost = localPlayer.State.PlayerSlot == 5;
                var winnerIsGhost = (localPlayerIsGhost && playerIsHealthy) || (!localPlayerIsGhost && !playerIsHealthy);
                var winnerName = winnerIsGhost ? "Ghost" : "Trackers";
                _matchEndedView.WinnerName.text = winnerName;
            }
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

            _matchEndedView.LeaveButton.OnClickAsAsyncEnumerable(CancellationToken.None).ForEachAsync(_ =>
            {
                if (_matchEndedView.LeaveButton.enabledSelf)
                {
                    _matchEndedView.LeaveButton.SetEnabled(false);
                    _gameSession.Close();
                    _matchEndedView.LeaveButton.SetEnabled(true);
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

            _connectionView.MatchNameInput.RegisterValueChangedCallback(NameValidation);
            _connectionView.PlayerNameInput.RegisterValueChangedCallback(NameValidation);
        }

        void NameValidation(ChangeEvent<string> changeEvent)
        {
            var textField = changeEvent.target as TextField;
            if (changeEvent.newValue.Length > 12 || CheckForInvalidCharacters(changeEvent.newValue, "<>/{}[]"))
            {
                textField.value = changeEvent.previousValue;
            }
        }

        bool CheckForInvalidCharacters(string value, string invalidCharacters)
        {
            foreach (var c in value)
            {
                if (invalidCharacters.Contains(c))
                {
                    return true;
                }
            }
            return false;
        }

        readonly ConnectionView _connectionView;
        readonly ReadyView _readyView;
        readonly MatchEndedView _matchEndedView;
        readonly IGameSession _gameSession;
        readonly IClientWorld _world;
    }
}
