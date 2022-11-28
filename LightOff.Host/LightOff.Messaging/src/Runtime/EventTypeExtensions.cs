// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using RailgunNet.System.Encoding;
using System.Text;

namespace LightOff.Messaging.src.Runtime
{
    public static class EventTypeExtensions
    {
        [Encoder]
        public static void WriteEventType(this RailBitBuffer buffer, EventMessageType eventType)
        {
            buffer.WriteUInt((uint)eventType);
        }

        [Decoder]
        public static EventMessageType ReadEventType(this RailBitBuffer buffer)
        {
            return (EventMessageType)buffer.ReadUInt();
        }
    }
}
