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

using JetBrains.Annotations;
using RailgunNet.Util;
using RailgunNet.Util.Debug;

namespace RailgunNet.System.Encoding.Compressors
{
    public static class RailFloatCompressorExtensions
    {
        public static void WriteFloat(
            this RailBitBuffer buffer,
            RailFloatCompressor compressor,
            float value)
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

        public static float ReadFloat(this RailBitBuffer buffer, RailFloatCompressor compressor)
        {
            if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
            {
                return compressor.Unpack(buffer.ReadUInt());
            }

            return compressor.Unpack(buffer.Read(compressor.RequiredBits));
        }

        public static float PeekFloat(this RailBitBuffer buffer, RailFloatCompressor compressor)
        {
            if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
            {
                return compressor.Unpack(buffer.PeekUInt());
            }

            return compressor.Unpack(buffer.Peek(compressor.RequiredBits));
        }

        #region Array
        public static void WriteFloats(
            this RailBitBuffer buffer,
            RailFloatCompressor compressor,
            float[] values)
        {
            if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
            {
                foreach (float value in values)
                {
                    buffer.WriteUInt(compressor.Pack(value));
                }
            }
            else
            {
                foreach (float value in values)
                {
                    buffer.Write(compressor.RequiredBits, compressor.Pack(value));
                }
            }
        }

        public static void ReadFloats(
            this RailBitBuffer buffer,
            RailFloatCompressor compressor,
            float[] toStore)
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

    /// <summary>
    ///     Compresses floats to a given range with a given precision.
    ///     http://stackoverflow.com/questions/8382629/compress-floating-point-numbers-with-specified-range-and-precision
    /// </summary>
    public class RailFloatCompressor
    {
        private readonly float invPrecision;
        private readonly uint mask;
        private readonly float maxValue;

        private readonly float minValue;
        private readonly float precision;

        [PublicAPI]
        public RailFloatCompressor(float minValue, float maxValue, float precision)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.precision = precision;

            invPrecision = 1.0f / precision;
            RequiredBits = ComputeRequiredBits();
            mask = (uint) ((1L << RequiredBits) - 1);
        }

        public int RequiredBits { get; }

        public uint Pack(float value)
        {
            float newValue = RailUtil.Clamp(value, minValue, maxValue);
            if (newValue != value)
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

            float adjusted = (value - minValue) * invPrecision;
            return (uint) (adjusted + 0.5f) & mask;
        }

        public float Unpack(uint data)
        {
            float adjusted = data * precision + minValue;
            return RailUtil.Clamp(adjusted, minValue, maxValue);
        }

        private int ComputeRequiredBits()
        {
            float range = maxValue - minValue;
            float maxVal = range * (1.0f / precision);
            return RailUtil.Log2((ulong) (maxVal + 0.5f)) + 1;
        }
    }
}
