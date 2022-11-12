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
using RailgunNet.System.Encoding;
using RailgunNet.Util.Debug;

namespace RailgunNet.System.Types
{
    public static class SequenceIdExtensions
    {
        public static void WriteSequenceId(this RailBitBuffer buffer, SequenceId sequenceId)
        {
            sequenceId.Write(buffer);
        }

        public static SequenceId ReadSequenceId(this RailBitBuffer buffer)
        {
            return SequenceId.Read(buffer);
        }

        public static SequenceId PeekSequenceId(this RailBitBuffer buffer)
        {
            return SequenceId.Peek(buffer);
        }
    }

    /// <summary>
    ///     A rolling sequence counter for ordering values. Repeats indefinitely
    ///     with 65535 possible unique values (0 is treated as invalid internally).
    ///     Consumes 16 bits when encoded for transmission.
    /// </summary>
    public readonly struct SequenceId
    {
        #region Encoding/Decoding
        public void Write(RailBitBuffer buffer)
        {
            buffer.Write(BITS_USED, rawValue);
        }

        public static SequenceId Read(RailBitBuffer buffer)
        {
            return new SequenceId(buffer.Read(BITS_USED));
        }

        public static SequenceId Peek(RailBitBuffer buffer)
        {
            return new SequenceId(buffer.Peek(BITS_USED));
        }
        #endregion

        private class SequenceIdComparer : IEqualityComparer<SequenceId>
        {
            public bool Equals(SequenceId x, SequenceId y)
            {
                return x == y;
            }

            public int GetHashCode(SequenceId x)
            {
                return x.GetHashCode();
            }
        }

        public static IEqualityComparer<SequenceId> CreateEqualityComparer()
        {
            return new SequenceIdComparer();
        }

        private const int BITS_USED = 16; // Max: 65535 unique (0 is invalid)
        private const int MAX_VALUE = (1 << BITS_USED) - 1;
        private const int BIT_SHIFT = 32 - BITS_USED;

        public static readonly SequenceId Invalid = new SequenceId(0);
        public static readonly SequenceId Start = new SequenceId(1);

        #region Operators
        private static int GetDifference(SequenceId a, SequenceId b)
        {
            RailDebug.Assert(a.IsValid);
            RailDebug.Assert(b.IsValid);

            int difference = (int) ((a.rawValue << BIT_SHIFT) - (b.rawValue << BIT_SHIFT));
            return difference;
        }

        private static int WrapValue(int rawValue)
        {
            // We need to skip 0 since it's not a valid number
            if (rawValue > MAX_VALUE) return rawValue % MAX_VALUE;
            if (rawValue < 1) return rawValue % MAX_VALUE + MAX_VALUE;
            return rawValue;
        }

        public static SequenceId operator +(SequenceId a, int b)
        {
            RailDebug.Assert(a.IsValid);
            return new SequenceId((uint) WrapValue((int) a.rawValue + b));
        }

        public static SequenceId operator -(SequenceId a, int b)
        {
            RailDebug.Assert(a.IsValid);
            return new SequenceId((uint) WrapValue((int) a.rawValue - b));
        }

        public static int operator -(SequenceId a, SequenceId b)
        {
            int difference = GetDifference(a, b) >> BIT_SHIFT;

            // We need to skip 0 since it's not a valid number
            if (a.rawValue < b.rawValue)
            {
                if (difference > 0) difference--;
            }
            else
            {
                if (difference < 0) difference++;
            }

            return difference;
        }

        public static SequenceId operator ++(SequenceId a)
        {
            RailDebug.Assert(a.IsValid);

            return a.Next;
        }

        public static bool operator >(SequenceId a, SequenceId b)
        {
            int difference = GetDifference(a, b);
            return difference > 0;
        }

        public static bool operator <(SequenceId a, SequenceId b)
        {
            int difference = GetDifference(a, b);
            return difference < 0;
        }

        public static bool operator >=(SequenceId a, SequenceId b)
        {
            int difference = GetDifference(a, b);
            return difference >= 0;
        }

        public static bool operator <=(SequenceId a, SequenceId b)
        {
            int difference = GetDifference(a, b);
            return difference <= 0;
        }

        public static bool operator ==(SequenceId a, SequenceId b)
        {
            RailDebug.Assert(a.IsValid);
            RailDebug.Assert(b.IsValid);

            return a.rawValue == b.rawValue;
        }

        public static bool operator !=(SequenceId a, SequenceId b)
        {
            RailDebug.Assert(a.IsValid);
            RailDebug.Assert(b.IsValid);

            return a.rawValue != b.rawValue;
        }
        #endregion

        public SequenceId Next
        {
            get
            {
                RailDebug.Assert(IsValid);

                uint nextValue = rawValue + 1;
                if (nextValue > MAX_VALUE) nextValue = 1;
                return new SequenceId(nextValue);
            }
        }

        public bool IsValid => rawValue > 0;

        private readonly uint rawValue;

        private SequenceId(uint rawValue)
        {
            this.rawValue = rawValue;
        }

        public override int GetHashCode()
        {
            return (int) rawValue;
        }

        public override bool Equals(object obj)
        {
            if (obj is SequenceId) return ((SequenceId) obj).rawValue == rawValue;
            return false;
        }

        public override string ToString()
        {
            if (IsValid) return "SequenceId:" + (rawValue - 1);
            return "SequenceId:Invalid";
        }
    }
}
