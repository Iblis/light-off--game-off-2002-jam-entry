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

using RailgunNet.Factory;
using RailgunNet.System.Buffer;
using RailgunNet.System.Types;
using RailgunNet.Util;
using RailgunNet.Util.Debug;
using RailgunNet.Util.Pooling;

namespace RailgunNet.Logic.Wrappers
{
    /// <summary>
    ///     Used to differentiate/typesafe state records. Not strictly necessary.
    /// </summary>
    [OnlyIn(Component.Server)]
    public class RailStateRecord : IRailTimedValue, IRailPoolable<RailStateRecord>
    {
        private RailState state;

        private Tick tick;

        public RailStateRecord()
        {
            state = null;
            tick = Tick.INVALID;
        }

        public bool IsValid => tick.IsValid;
        public RailState State => state;
        private Tick Tick => tick;

        #region Interface
        Tick IRailTimedValue.Tick => tick;
        #endregion

        public void Overwrite(IRailStateConstruction stateCreator, Tick tick, RailState state)
        {
            RailDebug.Assert(tick.IsValid);

            this.tick = tick;
            if (this.state == null)
            {
                this.state = state.Clone(stateCreator);
            }
            else
            {
                this.state.OverwriteFrom(state);
            }
        }

        public void Invalidate()
        {
            tick = Tick.INVALID;
        }

        private void Reset()
        {
            tick = Tick.INVALID;
            RailPool.SafeReplace(ref state, null);
        }

        #region Pooling
        IRailMemoryPool<RailStateRecord> IRailPoolable<RailStateRecord>.Pool { get; set; }

        void IRailPoolable<RailStateRecord>.Reset()
        {
            Reset();
        }

        void IRailPoolable<RailStateRecord>.Allocated()
        {
        }
        #endregion
    }
}
