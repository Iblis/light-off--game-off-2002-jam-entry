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
    public static class RailIntCompressorExtensions
    {
        public static void WriteInt(
            this RailBitBuffer buffer,
            RailIntCompressor compressor,
            int value)
        {
            if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
            {
                buffer.WriteUInt(compressor.Pack(value));
            }
            else
            {
                buffer.Write(compressor.RequiredBits, compressor.Pack(value));
            }
        }

        public static int ReadInt(this RailBitBuffer buffer, RailIntCompressor compressor)
        {
            if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
            {
                return compressor.Unpack(buffer.ReadUInt());
            }

            return compressor.Unpack(buffer.Read(compressor.RequiredBits));
        }

        public static int PeekInt(this RailBitBuffer buffer, RailIntCompressor compressor)
        {
            if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
            {
                return compressor.Unpack(buffer.PeekUInt());
            }

            return compressor.Unpack(buffer.Peek(compressor.RequiredBits));
        }

        #region Array
        public static void WriteInts(
            this RailBitBuffer buffer,
            RailIntCompressor compressor,
            int[] values)
        {
            if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
            {
                foreach (int value in values)
                {
                    buffer.WriteUInt(compressor.Pack(value));
                }
            }
            else
            {
                foreach (int value in values)
                {
                    buffer.Write(compressor.RequiredBits, compressor.Pack(value));
                }
            }
        }

        public static void ReadInts(
            this RailBitBuffer buffer,
            RailIntCompressor compressor,
            int[] toStore)
        {
            if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
            {
                for (int i = 0; i < toStore.Length; i++)
                {
                    toStore[i] = compressor.Unpack(buffer.ReadUInt());
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

    public class RailIntCompressor
    {
        private readonly uint mask;
        private readonly int maxValue;
        private readonly int minValue;

        public RailIntCompressor(int minValue, int maxValue)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;

            RequiredBits = ComputeRequiredBits();
            mask = (uint)((1UL << RequiredBits) - 1);
        }

        public int RequiredBits { get; }

        public uint Pack(int value)
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

            return (uint)(value - minValue) & mask;
        }

        public int Unpack(uint data)
        {
            return (int)(data + minValue);
        }

        private int ComputeRequiredBits()
        {
            if (minValue >= maxValue) return 0;

            unchecked
            {
                uint range = (uint)(maxValue - minValue);
                return RailUtil.Log2(range) + 1;
            }
        }
    }
}
