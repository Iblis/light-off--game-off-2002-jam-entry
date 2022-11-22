﻿// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
namespace LightOff.Host.Session
{
    public interface IHostedGameSessions
    {
        IHostedGameSession GetFor(string sessionName);
    }
}
