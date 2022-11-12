using System.Collections.Generic;
using RailgunNet.Factory;
using RailgunNet.Logic;
using RailgunNet.System.Encoding;
using RailgunNet.System.Types;
using RailgunNet.Util.Debug;

namespace RailgunNet.Connection
{
    public static class RailPacketSerializer
    {
        public static void Encode(
            this RailPacketOutgoing packet,
            RailResource resource,
            RailBitBuffer buffer)
        {
            packet.Encode(resource, resource, buffer);
        }

        public static void Decode(
            this RailPacketIncoming packet,
            RailResource resource,
            RailBitBuffer buffer)
        {
            packet.Decode(resource, resource, resource, buffer);
        }

        /// <summary>
        ///     After writing the header we write the packet data in three passes.
        ///     The first pass is a fill of events up to a percentage of the packet.
        ///     The second pass is the payload value, which will try to fill the
        ///     remaining packet space. If more space is available, we will try
        ///     to fill it with any remaining events, up to the maximum packet size.
        /// </summary>
        public static void Encode(
            this RailPacketOutgoing packet,
            IRailStateConstruction stateCreator,
            IRailEventConstruction eventCreator,
            RailBitBuffer buffer)
        {
            // Write: [Header]
            EncodeHeader(packet, buffer);

            // Write: [Events] (Early Pack)
            EncodeEvents(packet, eventCreator, buffer, RailConfig.PACKCAP_EARLY_EVENTS);

            // Write: [Payload] (+1 byte for the event count)
            packet.EncodePayload(stateCreator, buffer, packet.SenderTick, 1);

            // Write: [Events] (Fill Pack)
            EncodeEvents(packet, eventCreator, buffer, RailConfig.PACKCAP_MESSAGE_TOTAL);
        }

        public static void Decode(
            this RailPacketIncoming packet,
            IRailCommandConstruction commandCreator,
            IRailStateConstruction stateCreator,
            IRailEventConstruction eventCreator,
            RailBitBuffer buffer)
        {
            // Read: [Header]
            DecodeHeader(packet, buffer);

            // Read: [Events] (Early Pack)
            DecodeEvents(packet, eventCreator, buffer);

            // Read: [Payload]
            packet.DecodePayload(commandCreator, stateCreator, buffer);

            // Read: [Events] (Fill Pack)
            DecodeEvents(packet, eventCreator, buffer);
        }

        #region Header
        private static void EncodeHeader(RailPacketOutgoing packet, RailBitBuffer buffer)
        {
            RailDebug.Assert(packet.SenderTick.IsValid);

            // Write: [LocalTick]
            buffer.WriteTick(packet.SenderTick);

            // Write: [LastAckTick]
            buffer.WriteTick(packet.LastAckTick);

            // Write: [AckReliableEventId]
            buffer.WriteSequenceId(packet.LastAckEventId);
        }

        private static void DecodeHeader(RailPacketIncoming packet, RailBitBuffer buffer)
        {
            // Read: [LocalTick]
            packet.SenderTick = buffer.ReadTick();

            // Read: [LastAckTick]
            packet.LastAckTick = buffer.ReadTick();

            // Read: [AckReliableEventId]
            packet.LastAckEventId = buffer.ReadSequenceId();
        }
        #endregion

        #region Events
        /// <summary>
        ///     Writes as many events as possible up to maxSize and returns the number
        ///     of events written in the batch. Also increments the total counter.
        /// </summary>
        private static void EncodeEvents(
            RailPacketOutgoing packet,
            IRailEventConstruction eventCreator,
            RailBitBuffer buffer,
            int maxSize)
        {
            packet.EventsWritten += buffer.PackToSize(
                maxSize,
                RailConfig.MAXSIZE_EVENT,
                packet.GetNextEvents(),
                (evnt, buf) => evnt.Encode(
                    eventCreator.EventTypeCompressor,
                    buf,
                    packet.SenderTick),
                evnt => evnt.RegisterSent());
        }

        private static void DecodeEvents(
            RailPacketIncoming packet,
            IRailEventConstruction eventCreator,
            RailBitBuffer buffer)
        {
            IEnumerable<RailEvent> decoded = buffer.UnpackAll(
                buf => RailEvent.Decode(
                    eventCreator,
                    eventCreator.EventTypeCompressor,
                    buf,
                    packet.SenderTick));
            foreach (RailEvent evnt in decoded)
            {
                packet.Events.Add(evnt);
            }
        }
        #endregion
    }
}
