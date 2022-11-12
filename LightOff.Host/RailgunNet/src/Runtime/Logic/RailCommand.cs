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

using RailgunNet.Factory;
using RailgunNet.System.Buffer;
using RailgunNet.System.Encoding;
using RailgunNet.System.Types;
using RailgunNet.Util;
using RailgunNet.Util.Pooling;

namespace RailgunNet.Logic
{
    /// <summary>
    ///     Commands contain input data from the client to be applied to entities.
    /// </summary>
    public abstract class RailCommand : IRailPoolable<RailCommand>, IRailTimedValue
    {
        /// <summary>
        ///     The client's local tick (not server predicted) at the time of sending.
        /// </summary>
        public Tick ClientTick { get; set; } // Synchronized

        public bool IsNewCommand { get; set; }
        private RailCommandDataSerializer DataSerializer { get; set; }
        Tick IRailTimedValue.Tick => ClientTick;

        #region Implementation: IRailPoolable
        IRailMemoryPool<RailCommand> IRailPoolable<RailCommand>.Pool { get; set; }

        void IRailPoolable<RailCommand>.Reset()
        {
            Reset();
        }

        void IRailPoolable<RailCommand>.Allocated()
        {
            DataSerializer = new RailCommandDataSerializer(this);
        }
        #endregion

        #region Encode/Decode/internals
        private void Reset()
        {
            ClientTick = Tick.INVALID;
            DataSerializer.ResetData();
        }

        [OnlyIn(Component.Client)]
        public void Encode(RailBitBuffer buffer)
        {
            // Write: [SenderTick]
            buffer.WriteTick(ClientTick);

            // Write: [Command Data]
            DataSerializer.EncodeData(buffer);
        }

        [OnlyIn(Component.Server)]
        public static RailCommand Decode(
            IRailCommandConstruction commandCreator,
            RailBitBuffer buffer)
        {
            RailCommand command = commandCreator.CreateCommand();

            // Read: [SenderTick]
            command.ClientTick = buffer.ReadTick();

            // Read: [Command Data]
            command.DataSerializer.DecodeData(buffer);

            return command;
        }
        #endregion
    }
}
