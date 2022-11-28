// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using RailgunNet.Connection.Client;

namespace LightOff.IO.WebSocket
{
    internal class ConnectionResult
    {
        public static ConnectionResult Error = new ConnectionResult(null, null);
        public RailClient Client { get; }
        public RailClientRoom Room { get;}

        public bool Success => Client != null && Room != null;

        internal ConnectionResult(RailClient client, RailClientRoom room)
        {
            Client = client; 
            Room = room;
        }
    }
}
