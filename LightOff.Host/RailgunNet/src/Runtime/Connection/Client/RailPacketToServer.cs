/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016-2018 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
 */

using System.Collections.Generic;
using RailgunNet.Factory;
using RailgunNet.Logic.Wrappers;
using RailgunNet.System;
using RailgunNet.System.Encoding;
using RailgunNet.System.Types;
using RailgunNet.Util;

namespace RailgunNet.Connection.Client
{
    /// <summary>
    ///     Packet sent from client to server.
    /// </summary>
    [OnlyIn(Component.Client)]
    public sealed class RailPacketToServer : RailPacketOutgoing
    {
        private readonly RailPackedListOutgoing<RailCommandUpdate> commandUpdates;
        private readonly RailView view;

        public RailPacketToServer()
        {
            view = new RailView();
            commandUpdates = new RailPackedListOutgoing<RailCommandUpdate>();
        }

        public IEnumerable<RailCommandUpdate> Sent => commandUpdates.Sent;

        public override void Reset()
        {
            base.Reset();

            view.Clear();
            commandUpdates.Clear();
        }

        public void Populate(IEnumerable<RailCommandUpdate> commandUpdates, RailView view)
        {
            this.commandUpdates.AddPending(commandUpdates);

            // We don't care about sending/storing the local tick
            this.view.Integrate(view);
        }

        #region Encode/Decode
        public override void EncodePayload(
            IRailStateConstruction stateCreator,
            RailBitBuffer buffer,
            Tick localTick,
            int reservedBytes)
        {
            // Write: [Commands]
            EncodeCommands(buffer);

            // Write: [View]
            EncodeView(buffer, localTick, reservedBytes);
        }

        private void EncodeCommands(RailBitBuffer buffer)
        {
            commandUpdates.Encode(
                buffer,
                RailConfig.PACKCAP_COMMANDS,
                RailConfig.MAXSIZE_COMMANDUPDATE,
                (commandUpdate, buf) => commandUpdate.Encode(buf));
        }

        private void EncodeView(RailBitBuffer buffer, Tick localTick, int reservedBytes)
        {
            buffer.PackToSize(
                RailConfig.PACKCAP_MESSAGE_TOTAL - reservedBytes,
                int.MaxValue,
                view.GetOrdered(localTick),
                (pair, buf) =>
                {
                    buf.WriteEntityId(pair.Key); // Write: [EntityId]
                    buf.WriteTick(pair.Value.LastReceivedTick); // Write: [LastReceivedTick]
                    // (Local tick not transmitted)
                    buf.WriteBool(pair.Value.IsFrozen); // Write: [IsFrozen]
                });
        }
        #endregion
    }
}
