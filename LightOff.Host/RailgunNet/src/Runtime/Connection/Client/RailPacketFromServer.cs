using System.Collections.Generic;
using RailgunNet.Factory;
using RailgunNet.Logic.Wrappers;
using RailgunNet.System.Encoding;
using RailgunNet.Util;

namespace RailgunNet.Connection.Client
{
    /// <summary>
    ///     Packet sent from server to client.
    /// </summary>
    [OnlyIn(Component.Client)]
    public sealed class RailPacketFromServer : RailPacketIncoming
    {
        private readonly RailPackedListIncoming<RailStateDelta> deltas;

        public RailPacketFromServer()
        {
            deltas = new RailPackedListIncoming<RailStateDelta>();
        }

        public IEnumerable<RailStateDelta> Deltas => deltas.Received;

        public override void Reset()
        {
            base.Reset();

            deltas.Clear();
        }

        #region Encode/Decode
        public override void DecodePayload(
            IRailCommandConstruction commandCreator,
            IRailStateConstruction stateCreator,
            RailBitBuffer buffer)
        {
            // Read: [Deltas]
            DecodeDeltas(stateCreator, buffer);
        }

        private void DecodeDeltas(IRailStateConstruction stateCreator, RailBitBuffer buffer)
        {
            deltas.Decode(
                buffer,
                buf => RailStateDeltaSerializer.DecodeDelta(stateCreator, buf, SenderTick));
        }
        #endregion
    }
}
