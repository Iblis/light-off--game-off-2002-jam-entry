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

using System;
using RailgunNet.Factory;
using RailgunNet.System.Encoding;
using RailgunNet.Util.Debug;

namespace RailgunNet.Connection.Traffic
{
    /// <summary>
    ///     Responsible for encoding and decoding packet information.
    /// </summary>
    public class RailInterpreter
    {
        private readonly RailBitBuffer bitBufferSend;
        private readonly RailBitBuffer bitBufferReceive;
        private readonly byte[] bytes;

        public RailInterpreter()
        {
            bytes = new byte[RailConfig.DATA_BUFFER_SIZE];
            bitBufferSend = new RailBitBuffer();
            bitBufferReceive = new RailBitBuffer();
        }

        public void SendPacket(RailResource resource, IRailNetPeer peer, RailPacketOutgoing packet)
        {
            bitBufferSend.Clear();
            packet.Encode(resource, bitBufferSend);
            int length = bitBufferSend.Store(bytes);
            RailDebug.Assert(length <= RailConfig.PACKCAP_MESSAGE_TOTAL);
            peer.SendPayload(new ArraySegment<byte>(bytes, 0, length));
        }

        public RailBitBuffer LoadData(ArraySegment<byte> buffer)
        {
            bitBufferReceive.Load(buffer);
            return bitBufferReceive;
        }
    }
}
