// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Cysharp.Threading.Tasks;

namespace LightOff.ClientLogic
{
    public interface ISessionConnection
    {
        UniTask<bool> ConnectTo(string sessionName, string playerName);
        
        void Disconnect();

        void SetReadyState(bool value);
        void Update();

        UniTask CompletionDueToDisconnect { get; }
    }
}
