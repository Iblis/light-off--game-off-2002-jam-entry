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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RailgunNet.Util
{
    public static class RailUtil
    {
        // http://stackoverflow.com/questions/15967240/fastest-implementation-of-log2int-and-log2float#answer-58497416

        [StructLayout(LayoutKind.Explicit)]
        private struct Double2Long
        {
            [FieldOffset(0)] public ulong asLong;
            [FieldOffset(0)] public double asDouble;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Log2(ulong val)
        {
            if (val == 0) return 0;

            Double2Long a;
            a.asLong = 0;
            a.asDouble = val;

            return (int)((a.asLong >> 52) + 1) & 0xFF;
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max) value = max;

            return value;
        }
    }
}
