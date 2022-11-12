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
using RailgunNet.System.Encoding;
using RailgunNet.Util.Debug;

namespace RailgunNet.System.Types
{
    public static class TickExtensions
    {
        public static void WriteTick(this RailBitBuffer buffer, Tick tick)
        {
            tick.Write(buffer);
        }

        public static Tick ReadTick(this RailBitBuffer buffer)
        {
            return Tick.Read(buffer);
        }

        public static Tick PeekTick(this RailBitBuffer buffer)
        {
            return Tick.Peek(buffer);
        }

        #region Array
        public static void WriteTicks(this RailBitBuffer buffer, Tick[] ticks)
        {
            for (int i = 0; i < ticks.Length; i++)
            {
                ticks[i].Write(buffer);
            }
        }

        public static void ReadTicks(this RailBitBuffer buffer, Tick[] toStore)
        {
            for (int i = 0; i < toStore.Length; i++)
            {
                toStore[i] = Tick.Read(buffer);
            }
        }
        #endregion
    }

    /// <summary>
    ///     A type-safe and zero-safe wrapper for a tick int. Supports basic
    ///     operations and encoding. All values are offset by +1 (zero
    ///     is invalid, 1 is tick zero, etc.).
    /// </summary>
    public readonly struct Tick
    {
        #region Encoding/Decoding
        public void Write(RailBitBuffer buffer)
        {
            buffer.WriteUInt(tickValue);
        }

        public static Tick Read(RailBitBuffer buffer)
        {
            return new Tick(buffer.ReadUInt());
        }

        public static Tick Peek(RailBitBuffer buffer)
        {
            return new Tick(buffer.PeekUInt());
        }
        #endregion

        private class TickComparer : Comparer<Tick>, IEqualityComparer<Tick>
        {
            private readonly Comparer<uint> comparer;

            public TickComparer()
            {
                comparer = Comparer<uint>.Default;
            }

            public bool Equals(Tick x, Tick y)
            {
                RailDebug.Assert(x.IsValid);
                RailDebug.Assert(y.IsValid);
                return x == y;
            }

            public int GetHashCode(Tick x)
            {
                RailDebug.Assert(x.IsValid);
                return x.GetHashCode();
            }

            public override int Compare(Tick x, Tick y)
            {
                RailDebug.Assert(x.IsValid);
                RailDebug.Assert(y.IsValid);
                return comparer.Compare(x.tickValue, y.tickValue);
            }
        }

        public static Comparer<Tick> CreateComparer()
        {
            return new TickComparer();
        }

        public static IEqualityComparer<Tick> CreateEqualityComparer()
        {
            return new TickComparer();
        }

        public static Tick Subtract(Tick a, uint b, bool warnClamp = false)
        {
            long result = a.tickValue - (int) b;
            if (result < 1)
            {
                if (warnClamp) RailDebug.LogWarning("Clamping tick subtraction");
                result = 1;
            }

            return new Tick((uint) result);
        }

        public static readonly Tick INVALID = new Tick(0);
        public static readonly Tick START = new Tick(1);

        #region Operators
        // Can't find references on these, so just delete and build to find uses

        public static Tick operator ++(Tick a)
        {
            return a.GetNext();
        }

        public static bool operator ==(Tick a, Tick b)
        {
            return a.tickValue == b.tickValue;
        }

        public static bool operator !=(Tick a, Tick b)
        {
            return a.tickValue != b.tickValue;
        }

        public static bool operator <(Tick a, Tick b)
        {
            RailDebug.Assert(a.IsValid && b.IsValid);
            return a.tickValue < b.tickValue;
        }

        public static bool operator <=(Tick a, Tick b)
        {
            RailDebug.Assert(a.IsValid && b.IsValid);
            return a.tickValue <= b.tickValue;
        }

        public static bool operator >(Tick a, Tick b)
        {
            RailDebug.Assert(a.IsValid && b.IsValid);
            return a.tickValue > b.tickValue;
        }

        public static bool operator >=(Tick a, Tick b)
        {
            RailDebug.Assert(a.IsValid && b.IsValid);
            return a.tickValue >= b.tickValue;
        }

        public static int operator -(Tick a, Tick b)
        {
            RailDebug.Assert(a.IsValid && b.IsValid);
            long difference = a.tickValue - (long) b.tickValue;
            return (int) difference;
        }

        public static int operator +(Tick a, Tick b)
        {
            RailDebug.Assert(a.IsValid && b.IsValid);
            long sum = a.tickValue + (long) b.tickValue;
            return (int) sum;
        }

        public static Tick operator +(Tick a, uint b)
        {
            RailDebug.Assert(a.IsValid);
            return new Tick(a.tickValue + b);
        }

        public static Tick operator +(Tick a, int b)
        {
            if (b < 0)
            {
                return a - (uint) Math.Abs(b);
            }

            return a + (uint) b;
        }

        public static Tick operator -(Tick a, uint b)
        {
            return Subtract(a, b, true);
        }
        #endregion

        #region Properties
        public bool IsValid => tickValue > 0;

        public float ToTime(float tickDeltaTime)
        {
            RailDebug.Assert(IsValid);
            return (tickValue - 1) * tickDeltaTime;
        }
        #endregion

        /// <summary>
        ///     Should be used very sparingly. Otherwise it defeats type safety.
        /// </summary>
        public uint RawValue
        {
            get
            {
                RailDebug.Assert(IsValid);
                return tickValue - 1;
            }
        }

        private readonly uint tickValue;

        private Tick(uint tickValue)
        {
            this.tickValue = tickValue;
        }

        public Tick GetNext()
        {
            RailDebug.Assert(IsValid);
            return new Tick(tickValue + 1);
        }

        public override int GetHashCode()
        {
            return (int) tickValue;
        }

        public override bool Equals(object obj)
        {
            if (obj is Tick) return ((Tick) obj).tickValue == tickValue;
            return false;
        }

        public override string ToString()
        {
            if (tickValue == 0) return "Tick:Invalid";
            return "Tick:" + (tickValue - 1);
        }

        public bool IsSendTick(int tickRate)
        {
            if (IsValid) return RawValue % tickRate == 0;
            return false;
        }
    }
}
