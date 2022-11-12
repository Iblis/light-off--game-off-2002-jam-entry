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
using System.Text;
using RailgunNet.System.Types;

namespace RailgunNet.System
{
    /// <summary>
    ///     A rolling history buffer to keep track of seen SequenceIds.
    /// </summary>
    public class RailHistory
    {
        private readonly HistoryBits history;

        private SequenceId latest;

        public RailHistory(int chunks)
        {
            latest = SequenceId.Start;
            history = new HistoryBits(chunks);
        }

        public SequenceId Latest => latest;
        public bool IsValid => latest.IsValid;

        public void Store(SequenceId value)
        {
            int difference = latest - value;
            if (difference > 0)
            {
                history.Set(difference - 1);
            }
            else
            {
                int offset = -difference;
                history.Shift(offset);

                // We might shift so far we need to clear everything
                if (offset - 1 < history.Capacity) history.Set(offset - 1);
                latest = value;
            }
        }

        private bool Contains(SequenceId value)
        {
            int difference = Latest - value;
            if (difference < 0) return false;
            if (difference == 0) return true;
            return history.Get(difference - 1);
        }

        public bool IsNewId(SequenceId id)
        {
            if (ValueTooOld(id)) return false;
            if (Contains(id)) return false;
            return true;
        }

        public bool AreInRange(SequenceId lowest, SequenceId highest)
        {
            return highest - lowest <= history.Capacity;
        }

        private bool ValueTooOld(SequenceId value)
        {
            return Latest - value > history.Capacity;
        }

        /// <summary>
        ///     A multi-chunk bit array that supports shifting.
        /// </summary>
        private class HistoryBits
        {
            private const int CHUNK_SIZE = 32;

            private readonly uint[] chunks;

            public HistoryBits(int chunks)
            {
                this.chunks = new uint[chunks];
                Capacity = chunks * CHUNK_SIZE;
            }

            public int Capacity { get; }

            public void Set(int index)
            {
                int chunk = index / CHUNK_SIZE;
                int position = index % CHUNK_SIZE;

                if (chunk >= chunks.Length)
                {
                    throw new ArgumentOutOfRangeException("index (" + index + ")");
                }

                chunks[chunk] |= 0x1U << position;
            }

            public bool Get(int index)
            {
                int chunk = index / CHUNK_SIZE;
                int position = index % CHUNK_SIZE;

                if (chunk >= chunks.Length)
                {
                    throw new ArgumentOutOfRangeException("index (" + index + ")");
                }

                return (chunks[chunk] & (1U << position)) != 0;
            }

            public void Shift(int count)
            {
                int numChunks = Math.Min(count / CHUNK_SIZE, chunks.Length);
                int numBits = count % CHUNK_SIZE;

                // Clear the top chunks since they're shifted out
                for (int i = 0; i < numChunks; i++)
                {
                    chunks[chunks.Length - 1 - i] = 0;
                }

                // Perform the shift
                for (int i = chunks.Length - 1; i >= numChunks; i--)
                {
                    // Get the high and low bits for shifting
                    ulong bits = chunks[i - numChunks] | ((ulong) chunks[i] << CHUNK_SIZE);

                    // Perform the mini-shift
                    bits <<= numBits;

                    // Separate and re-apply
                    chunks[i] = (uint) bits;
                    if (i + 1 < chunks.Length) chunks[i + 1] |= (uint) (bits >> CHUNK_SIZE);
                }

                // Clear the bottom chunks since they're shifted out
                for (int i = 0; i < numChunks; i++)
                {
                    chunks[i] = 0;
                }
            }

            public override string ToString()
            {
                StringBuilder raw = new StringBuilder();
                for (int i = chunks.Length - 1; i >= 0; i--)
                {
                    raw.Append(Convert.ToString(chunks[i], 2).PadLeft(CHUNK_SIZE, '0'));
                }

                StringBuilder spaced = new StringBuilder();
                for (int i = 0; i < raw.Length; i++)
                {
                    spaced.Append(raw[i]);
                    if ((i + 1) % 8 == 0) spaced.Append(" ");
                }

                return spaced.ToString();
            }
        }
    }
}
