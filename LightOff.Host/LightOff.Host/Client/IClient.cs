﻿// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using RailgunNet.Connection.Traffic;

namespace LightOff.Host.Client
{
    public interface IClient : IRailNetPeer
    {
        string PlayerName { get; init; }
    }
}