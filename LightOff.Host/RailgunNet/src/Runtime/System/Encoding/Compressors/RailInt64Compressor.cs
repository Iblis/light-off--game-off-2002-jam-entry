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
using RailgunNet.Util.Debug;

namespace RailgunNet.System.Encoding.Compressors
{
    public static class RailInt64CompressorExtensions
    {
        public static void WriteInt64(
            this RailBitBuffer buffer,
            RailInt64Compressor compressor,
            long value)
        {
            if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
            {
                buffer.WriteUInt64(compressor.Pack(value));
            }
            else
            {
                buffer.Write(compressor.RequiredBits, (uint)compressor.Pack(value));
            }
        }

        public static long ReadInt64(this RailBitBuffer buffer, RailInt64Compressor compressor)
        {
            if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
            {
                return compressor.Unpack(buffer.ReadUInt64());
            }

            return compressor.Unpack(buffer.Read(compressor.RequiredBits));
        }

        public static long PeekInt64(this RailBitBuffer buffer, RailInt64Compressor compressor)
        {
            if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
            {
                return compressor.Unpack(buffer.PeekUInt64());
            }

            return compressor.Unpack(buffer.Peek(compressor.RequiredBits));
        }

        #region Array
        public static void WriteInt64s(
            this RailBitBuffer buffer,
            RailInt64Compressor compressor,
            long[] values)
        {
            if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
            {
                foreach (long value in values)
                {
                    buffer.WriteUInt64(compressor.Pack(value));
                }
            }
            else
            {
                foreach (long value in values)
                {
                    buffer.Write(compressor.RequiredBits, (uint)compressor.Pack(value));
                }
            }
        }

        public static void ReadInt64s(
            this RailBitBuffer buffer,
            RailInt64Compressor compressor,
            long[] toStore)
        {
            if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
            {
                for (int i = 0; i < toStore.Length; i++)
                {
                    toStore[i] = compressor.Unpack(buffer.ReadUInt64());
                }
            }
            else
            {
                for (int i = 0; i < toStore.Length; i++)
                {
                    toStore[i] = compressor.Unpack(buffer.Read(compressor.RequiredBits));
                }
            }
        }
        #endregion
    }

    public class RailInt64Compressor
    {
        private readonly ulong mask;
        private readonly long maxValue;
        private readonly long minValue;

        public RailInt64Compressor(long minValue, long maxValue)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;

            RequiredBits = ComputeRequiredBits();
            mask = RequiredBits < 64 ? 1UL << RequiredBits - 1 : ulong.MaxValue;
        }

        public int RequiredBits { get; }

        public ulong Pack(long value)
        {
            if (value < minValue || value > maxValue)
            {
                RailDebug.LogWarning(
                    "Clamping value for send! " +
                    value +
                    " vs. [" +
                    minValue +
                    "," +
                    maxValue +
                    "]");
            }

            return (ulong)(value - minValue) & mask;
        }

        public long Unpack(ulong data)
        {
            return (long)data + minValue;
        }

        private int ComputeRequiredBits()
        {
            if (minValue >= maxValue) return 0;

            unchecked
            {
                ulong range = (ulong)(maxValue - minValue);
                return RailUtil.Log2(range) + 1;
            }
        }
    }
}
