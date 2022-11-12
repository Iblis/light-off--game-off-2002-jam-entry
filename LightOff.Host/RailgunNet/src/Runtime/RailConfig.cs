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

using RailgunNet.Util;

namespace RailgunNet
{
    public enum Component
    {
        Client,
        Server
    }

    public enum ExternalEntityVisibility
    {
        All, // All entities are sent. 
        [OnlyIn(Component.Server)] Scoped // Only the entities within scope are sent.
    }

    public static class RailConfig
    {
        public enum RailApplication
        {
            Client,
            Server
        }

        /// <summary>
        ///     Network send rate in ticks/packet.
        /// </summary>
        public const int SERVER_SEND_RATE = 1;

        /// <summary>
        ///     Network send rate in ticks/packet.
        /// </summary>
        public const int CLIENT_SEND_RATE = 1;

        /// <summary>
        ///     Number of outgoing commands to send per packet.
        /// </summary>
        public const int COMMAND_SEND_COUNT = 40;

        /// <summary>
        ///     Number of commands to buffer for prediction.
        /// </summary>
        public const int COMMAND_BUFFER_COUNT = 40;

        /// <summary>
        ///     Number of entries to store in a dejitter buffer.
        /// </summary>
        public const int DEJITTER_BUFFER_LENGTH = 50;

        /// <summary>
        ///     Number of ticks we'll resend a view entry for without receiving
        ///     an update on that entity.
        /// </summary>
        public const int VIEW_TICKS = 100;

        /// <summary>
        ///     How many chunks to keep in the history bit array. The resulting
        ///     max history length will be EVENT_HISTORY_CHUNKS * 32.
        /// </summary>
        public const int HISTORY_CHUNKS = 6;

        #region Message Sizes
        /// <summary>
        ///     Data buffer size used for packet I/O.
        ///     Don't change this without a good reason.
        /// </summary>
        public const int DATA_BUFFER_SIZE = 16384;

        /// <summary>
        ///     The maximum message size that a packet can contain, based on known
        ///     MTUs for internet traffic. Don't change this without a good reason.
        ///     If using MiniUDP, this should be equal to NetConfig.DATA_MAXIMUM
        /// </summary>
        public const int PACKCAP_MESSAGE_TOTAL = 8700;

        /// <summary>
        ///     The max byte size when doing a first pass on packing events.
        /// </summary>
        public const int PACKCAP_EARLY_EVENTS = 370;

        /// <summary>
        ///     The max byte size when packing commands. (Client-only.)
        /// </summary>
        public const int PACKCAP_COMMANDS = 670;

        /// <summary>
        ///     Maximum bytes for a single entity. Used when packing entity deltas.
        /// </summary>
        public const int MAXSIZE_ENTITY = 100;

        /// <summary>
        ///     Maximum bytes for a single event.
        /// </summary>
        public const int MAXSIZE_EVENT = 100;

        /// <summary>
        ///     Maximum bytes for a single command update.
        /// </summary>
        public const int MAXSIZE_COMMANDUPDATE = 335;

        /// <summary>
        ///     Number of bits before doing VarInt fallback in compression.
        /// </summary>
        public const int VARINT_FALLBACK_SIZE = 10;

        /// <summary>
        ///     Maximum size for an encoded string.
        /// </summary>
        public const int STRING_LENGTH_MAX = 63;
        #endregion
    }
}
