// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using RailgunNet.Connection.Traffic;
using System;

namespace LightOff.IO
{
    public class DummyPeer : IRailNetPeer
    {
        public object PlayerData { get; set; }

        public float? Ping => 0.0f;

        public event RailNetPeerEvent PayloadReceived;

        public DummyPeer ReceivingPeer { get; set; }

        public void SendPayload(ArraySegment<byte> buffer)
        {
            if(ReceivingPeer != null)
            {
                ReceivingPeer.ReceivePayload(buffer);
            }
        }

        internal void ReceivePayload(ArraySegment<byte> buffer)
        {
            PayloadReceived.Invoke(this, buffer);
        }
    }
}