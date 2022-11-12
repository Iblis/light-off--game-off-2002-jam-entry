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
using System.Collections.Generic;
using System.Text;
using RailgunNet.Util;
using RailgunNet.Util.Debug;

namespace RailgunNet.System.Encoding
{
    /// <summary>
    ///     A first-in-first-out (FIFO) bit encoding buffer.
    /// </summary>
    public class RailBitBuffer
    {
        private const int GROW_FACTOR = 2;
        private const int MIN_GROW = 1;
        private const int DEFAULT_CAPACITY = 8;
        public const int MAX_LIST_COUNT = 255;

        /// <summary>
        ///     Buffer of chunks for storing data.
        /// </summary>
        private uint[] chunks;

        /// <summary>
        ///     The position of the next-to-be-read bit.
        /// </summary>
        private int readPos;

        /// <summary>
        ///     The position of the next-to-be-written bit.
        /// </summary>
        private int writePos;

        /// <summary>
        ///     Capacity is in data chunks: uint = 4 bytes
        /// </summary>
        public RailBitBuffer(int capacity = DEFAULT_CAPACITY)
        {
            chunks = new uint[capacity];
            readPos = 0;
            writePos = 0;
        }

        public bool Empty => writePos == 0;

        /// <summary>
        ///     Size the buffer will require in bytes.
        /// </summary>
        public int ByteSize => ((writePos - 1) >> 3) + 1;

        /// <summary>
        ///     Returns true iff we have read everything off of the buffer.
        /// </summary>
        public bool IsFinished => writePos == readPos;

        public static int PutBytes(uint value, byte[] buffer, int start)
        {
            int first = start;

            while (value > 0x7Fu)
            {
                buffer[start] = (byte)(value | 0x80u);
                value >>= 7;
                start++;
            }

            buffer[start] = (byte)value;
            return start - first + 1;
        }

        public static uint ReadBytes(byte[] buffer, ref int position)
        {
            byte dataByte;
            uint value = 0;

            do
            {
                value <<= 7;
                dataByte = buffer[position];
                value |= dataByte & 0x7Fu;
                position++;
            }
            while ((dataByte & 0x80u) != 0);

            return value;
        }

        private static int FindHighestBitPosition(byte data)
        {
            int shiftCount = 0;
            while (data > 0)
            {
                data >>= 1;
                shiftCount++;
            }

            return shiftCount;
        }

        private static byte ToASCII(char character)
        {
            byte value;

            try
            {
                value = Convert.ToByte(character);
            }
            catch (OverflowException)
            {
                RailDebug.LogWarning("Cannot convert to simple ASCII: " + character);
                return 0;
            }

            if (value > 127)
            {
                RailDebug.LogWarning("Cannot convert to simple ASCII: " + character);
                return 0;
            }

            return value;
        }

        /// <summary>
        ///     Clears the buffer (does not overwrite values, but doesn't need to).
        /// </summary>
        public void Clear()
        {
            readPos = 0;
            writePos = 0;
        }

        /// <summary>
        ///     Takes the lower numBits from the value and stores them in the buffer.
        /// </summary>
        public void Write(int numBits, uint value)
        {
            if (numBits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numBits), "Pushing negative bits");
            }

            if (numBits > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(numBits), "Pushing too many bits");
            }

            int index = writePos >> 5;
            int used = writePos & 0x0000001F;

            if (index + 1 >= chunks.Length) ExpandArray();

            ulong chunkMask = (1UL << used) - 1;
            ulong scratch = chunks[index] & chunkMask;
            ulong result = scratch | ((ulong)value << used);

            chunks[index] = (uint)result;
            chunks[index + 1] = (uint)(result >> 32);

            writePos += numBits;
        }

        /// <summary>
        ///     Reads the next numBits from the buffer.
        /// </summary>
        public uint Read(int numBits)
        {
            uint result = Peek(numBits);
            readPos += numBits;
            return result;
        }

        /// <summary>
        ///     Peeks at the next numBits from the buffer.
        /// </summary>
        public uint Peek(int numBits)
        {
            if (numBits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numBits), "Pushing negative bits");
            }

            if (numBits > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(numBits), "Pushing too many bits");
            }

            int index = readPos >> 5;
            int used = readPos & 0x0000001F;

            ulong chunkMask = ((1UL << numBits) - 1) << used;
            ulong scratch = chunks[index];
            if (index + 1 < chunks.Length) scratch |= (ulong)chunks[index + 1] << 32;
            ulong result = (scratch & chunkMask) >> used;

            return (uint)result;
        }

        /// <summary>
        ///     Copies the buffer to a byte buffer.
        /// </summary>
        public int Store(byte[] data)
        {
            // Write a sentinel bit to find our position and flash out bad data
            Write(1, 1);

            int numChunks = (writePos >> 5) + 1;
            RailDebug.Assert(data.Length >= numChunks * 4, "Buffer too small");

            for (int i = 0; i < numChunks; i++)
            {
                int dataIdx = i * 4;
                uint chunk = chunks[i];
                data[dataIdx] = (byte)chunk;
                data[dataIdx + 1] = (byte)(chunk >> 8);
                data[dataIdx + 2] = (byte)(chunk >> 16);
                data[dataIdx + 3] = (byte)(chunk >> 24);
            }

            return ByteSize;
        }

        /// <summary>
        ///     Overwrites this buffer with an array of byte data.
        /// </summary>
        public void Load(ArraySegment<byte> data)
        {
            int numChunks = data.Count / 4 + 1;
            if (chunks.Length < numChunks) chunks = new uint[numChunks];

            for (int i = 0; i < numChunks; i++)
            {
                int dataIdx = i * 4 + data.Offset;
                uint chunk = 0;

                if (dataIdx < data.Array.Length)
                {
                    chunk = data.Array[dataIdx];
                }

                if (dataIdx + 1 < data.Array.Length)
                {
                    chunk |= (uint)data.Array[dataIdx + 1] << 8;
                }

                if (dataIdx + 2 < data.Array.Length)
                {
                    chunk |= (uint)data.Array[dataIdx + 2] << 16;
                }

                if (dataIdx + 3 < data.Array.Length)
                {
                    chunk |= (uint)data.Array[dataIdx + 3] << 24;
                }

                chunks[i] = chunk;
            }

            int positionInByte = FindHighestBitPosition(data.Array[data.Count - 1 + data.Offset]);

            // Take one off the position to backtrack from the sentinel bit
            writePos = (data.Count - 1) * 8 + (positionInByte - 1);
            readPos = 0;
        }

        /// <summary>
        ///     Inserts data at a given position. Reserve the space first by writing
        ///     a given number of zero bits and storing the position.
        /// </summary>
        private void Insert(int position, int numBits, uint value)
        {
            if (numBits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numBits), "Pushing negative bits");
            }

            if (numBits > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(numBits), "Pushing too many bits");
            }

            int index = position >> 5;
            int used = position & 0x0000001F;

            ulong valueMask = (1UL << numBits) - 1;
            ulong prepared = (value & valueMask) << used;
            ulong scratch = chunks[index] | ((ulong)chunks[index + 1] << 32);
            ulong result = scratch | prepared;

            chunks[index] = (uint)result;
            chunks[index + 1] = (uint)(result >> 32);
        }

        private void ExpandArray()
        {
            int newCapacity = chunks.Length * GROW_FACTOR + MIN_GROW;

            uint[] newChunks = new uint[newCapacity];
            Array.Copy(chunks, newChunks, chunks.Length);
            chunks = newChunks;
        }

        public override string ToString()
        {
            StringBuilder raw = new StringBuilder();
            for (int i = chunks.Length - 1; i >= 0; i--)
            {
                raw.Append(Convert.ToString(chunks[i], 2).PadLeft(32, '0'));
            }

            StringBuilder spaced = new StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                spaced.Append(raw[i]);
                if ((i + 1) % 8 == 0) spaced.Append(" ");
            }

            return spaced.ToString();
        }

        #region Enumerables
        /// <summary>
        ///     Packs all elements of an enumerable.
        ///     Max MAX_LIST_COUNT elements, by default 255.
        /// </summary>
        public byte
            PackAll<T>(IEnumerable<T> elements, Action<T> encode) // TODO: Make this take a buffer!
        {
            byte count = 0;

            // Reserve: [Count]
            int countPosition = writePos;
            Write(8, 0);

            // Write: [Elements]
            foreach (T val in elements)
            {
                if (count == MAX_LIST_COUNT) break;

                encode.Invoke(val);
                count++;
            }

            // Deferred Write: [Count]
            Insert(countPosition, 8, count);
            return count;
        }

        /// <summary>
        ///     Packs all elements of an enumerable up to a given size.
        ///     Max MAX_LIST_COUNT elements, by default 255.
        /// </summary>
        public byte PackToSize<T>(
            int maxTotalBytes,
            int maxIndividualBytes,
            IEnumerable<T> elements,
            Action<T, RailBitBuffer> encode,
            Action<T> packed = null)
        {
            const int SIZE_BITS = 8;

            maxTotalBytes -= 1; // Sentinel bit can blow this up
            byte count = 0;

            // Reserve: [Count]
            int countWritePos = writePos;
            Write(SIZE_BITS, 0);

            // Write: [Elements]
            foreach (T val in elements)
            {
                if (count == MAX_LIST_COUNT) break;
                int rollback = writePos;
                int startByteSize = ByteSize;

                encode.Invoke(val, this);

                int endByteSize = ByteSize;
                int writeByteSize = endByteSize - startByteSize;
                if (writeByteSize > maxIndividualBytes)
                {
                    writePos = rollback;
                    RailDebug.LogWarning("Skipping " + val + " (" + writeByteSize + "B)");
                }
                else if (endByteSize > maxTotalBytes)
                {
                    writePos = rollback;
                    break;
                }
                else
                {
                    packed?.Invoke(val);
                    count++;
                }
            }

            // Deferred Write: [Count]
            Insert(countWritePos, SIZE_BITS, count);
            return count;
        }

        /// <summary>
        ///     Decodes all packed items.
        ///     Max MAX_LIST_COUNT elements, by default 255.
        /// </summary>
        public IEnumerable<T> UnpackAll<T>(Func<RailBitBuffer, T> decode)
        {
            // Read: [Count]
            byte count = ReadByte();

            // Read: [Elements]
            for (uint i = 0; i < count; i++)
            {
                yield return decode.Invoke(this);
            }
        }
        #endregion

        #region Encode/Decode
        #region Byte
        [Encoder]
        public void WriteByte(byte val)
        {
            Write(8, val);
        }

        [Decoder]
        public byte ReadByte()
        {
            return (byte)Read(8);
        }

        public byte PeekByte()
        {
            return (byte)Peek(8);
        }
        #endregion
        
        #region Byte array
        [Encoder]
        public void WriteByteArray(byte[] val)
        {
            // Length
            WriteUInt((uint) val.Length);
            
            // Content
            foreach (byte b in val)
            {
                WriteByte(b);
            }
        }

        [Decoder]
        public byte[] ReadByteArray()
        {
            // Length
            uint count = ReadUInt();
            
            // Content
            byte[] bytes = new byte[count];
            for (uint i = 0; i < count; i++)
            {
                bytes[i] = ReadByte();
            }
            return bytes;
        }
        #endregion

        #region UInt
        /// <summary>
        ///     Writes using an elastic number of bytes based on number size:
        ///     Bits   Min Dec    Max Dec     Max Hex     Bytes Used
        ///     0-7    0          127         0x0000007F  1 byte
        ///     8-14   128        1023        0x00003FFF  2 bytes
        ///     15-21  1024       2097151     0x001FFFFF  3 bytes
        ///     22-28  2097152    268435455   0x0FFFFFFF  4 bytes
        ///     28-32  268435456  4294967295  0xFFFFFFFF  5 bytes
        /// </summary>
        [Encoder]
        public void WriteUInt(uint val)
        {
            do
            {
                // Take the lowest 7 bits
                uint buffer = val & 0x7Fu;
                val >>= 7;

                // If there is more data, set the 8th bit to true
                if (val > 0) buffer |= 0x80u;

                // Store the next byte
                Write(8, buffer);
            }
            while (val > 0);
        }

        [Decoder]
        public uint ReadUInt()
        {
            uint buffer;
            uint val = 0x0u;
            int s = 0;

            do
            {
                buffer = Read(8);

                // Add back in the shifted 7 bits
                val |= (buffer & 0x7Fu) << s;
                s += 7;

                // Continue if we're flagged for more
            }
            while ((buffer & 0x80u) > 0);

            return val;
        }

        public uint PeekUInt()
        {
            int tempPosition = readPos;
            uint val = ReadUInt();
            readPos = tempPosition;
            return val;
        }
        #endregion

        #region Int
        [Encoder]
        public void WriteInt(int val)
        {
            uint zigzag = (uint)((val << 1) ^ (val >> 31));
            WriteUInt(zigzag);
        }

        [Decoder]
        public int ReadInt()
        {
            uint val = ReadUInt();
            int zagzig = (int)((val >> 1) ^ -(val & 1));
            return zagzig;
        }

        public int PeekInt()
        {
            uint val = PeekUInt();
            int zagzig = (int)((val >> 1) ^ -(val & 1));
            return zagzig;
        }
        #endregion

        #region Bool
        [Encoder]
        public void WriteBool(bool value)
        {
            Write(1, value ? 1U : 0U);
        }

        [Decoder]
        public bool ReadBool()
        {
            return Read(1) > 0;
        }

        public bool PeekBool()
        {
            return Peek(1) > 0;
        }
        #endregion

        #region UShort
        [Encoder]
        public void WriteFull(ushort value)
        {
            Write(16, value);
        }

        [Decoder]
        public ushort ReadFullU16()
        {
            return (ushort)Read(16);
        }
        #endregion

        #region String
        // 7 bits for 0-127 on the simple ASCII table
        private const int ASCII_BITS = 7;

        private static readonly int
            STRING_LENGTH_BITS = RailUtil.Log2(RailConfig.STRING_LENGTH_MAX);

        [Encoder]
        public void WriteString(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            uint length = (uint)value.Length;
            RailDebug.Assert(length <= RailConfig.STRING_LENGTH_MAX, value);
            if (value.Length > RailConfig.STRING_LENGTH_MAX) length = RailConfig.STRING_LENGTH_MAX;

            Write(STRING_LENGTH_BITS, length);
            for (int i = 0; i < length; i++)
            {
                Write(ASCII_BITS, ToASCII(value[i]));
            }
        }

        [Decoder]
        public string ReadString()
        {
            StringBuilder builder = new StringBuilder("");
            uint length = Read(STRING_LENGTH_BITS);
            for (int i = 0; i < length; i++)
            {
                builder.Append((char)Read(ASCII_BITS));
            }

            return builder.ToString();
        }
        #endregion

        #region UInt64
        /// <summary>
        ///     Writes using an elastic number of bytes based on number size:
        ///     Bits   Min Dec    Max Dec     Max Hex     Bytes Used
        ///     0-7    0          127         0x0000007F  1 byte
        ///     8-14   128        1023        0x00003FFF  2 bytes
        ///     15-21  1024       2097151     0x001FFFFF  3 bytes
        ///     22-28  2097152    268435455   0x0FFFFFFF  4 bytes
        ///     28-32  268435456  4294967295  0xFFFFFFFF  5 bytes
        /// </summary>
        [Encoder]
        public void WriteUInt64(ulong val)
        {
            do
            {
                // Take the lowest 7 bits
                uint buffer = (uint)(val & 0x7FL);
                val >>= 7;

                // If there is more data, set the 8th bit to true
                if (val > 0) buffer |= 0x80u;

                // Store the next byte
                Write(8, buffer);
            }
            while (val > 0);
        }

        [Decoder]
        public ulong ReadUInt64()
        {
            uint buffer;
            ulong val = 0x0L;
            int s = 0;

            do
            {
                buffer = Read(8);

                // Add back in the shifted 7 bits
                val |= (ulong)(buffer & 0x7Fu) << s;
                s += 7;

                // Continue if we're flagged for more
            }
            while ((buffer & 0x80u) > 0);

            return val;
        }

        public ulong PeekUInt64()
        {
            int tempPosition = readPos;
            ulong val = ReadUInt64();
            readPos = tempPosition;
            return val;
        }
        #endregion

        #region Int64
        [Encoder]
        public void WriteInt64(long val)
        {
            ulong zigzag = (ulong)((val << 1) ^ (val >> 63));
            WriteUInt64(zigzag);
        }

        [Decoder]
        public long ReadInt64()
        {
            ulong val = ReadUInt64();
            long zagzig = (long)(val >> 1) ^ -(long)(val & 1);
            return zagzig;
        }

        public long PeekInt64()
        {
            ulong val = PeekUInt64();
            long zagzig = (long)(val >> 1) ^ -(long)(val & 1);
            return zagzig;
        }
        #endregion
        #endregion
    }
}
