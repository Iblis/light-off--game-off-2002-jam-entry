// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Host.Client;

namespace LightOff.Host.Session
{
    public interface IHostedGameSession
    {
        bool CanBeJoined { get; }

        void Join(IClient client);
    }
}
