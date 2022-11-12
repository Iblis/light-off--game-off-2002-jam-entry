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
using RailgunNet.System.Types;

namespace RailgunNet
{
    /// <summary>
    ///     Used for keeping track of the remote peer's clock.
    /// </summary>
    public class RailClock
    {
        public enum EDesiredDelay
        {
            Minimal,
            Midpoint
        }

        private const uint DELAY_MIN = 2;
        private const uint DELAY_MAX = 20;
        private readonly uint delayMax;
        private readonly uint delayMin;
        private readonly int remoteRate;
        private bool shouldUpdateEstimate;

        public RailClock(
            uint remoteSendRate,
            EDesiredDelay eDelay = EDesiredDelay.Minimal,
            uint delayMin = DELAY_MIN,
            uint delayMax = DELAY_MAX)
        {
            remoteRate = (int) remoteSendRate;
            EstimatedRemote = Tick.INVALID;
            LatestRemote = Tick.INVALID;

            this.delayMin = delayMin;
            this.delayMax = delayMax;
            switch (eDelay)
            {
                case EDesiredDelay.Midpoint:
                    DelayDesired = (delayMax - delayMin) / 2 + delayMin;
                    break;
                case EDesiredDelay.Minimal:
                    DelayDesired = delayMin;
                    break;
            }

            shouldUpdateEstimate = false;
            ShouldTick = false;
        }

        public uint DelayDesired { get; }

        private bool ShouldTick { get; set; }
        public Tick EstimatedRemote { get; private set; }
        public Tick LatestRemote { get; private set; }

        public void UpdateLatest(Tick latestTick)
        {
            if (LatestRemote.IsValid == false) LatestRemote = latestTick;
            if (EstimatedRemote.IsValid == false)
            {
                EstimatedRemote = Tick.Subtract(LatestRemote, DelayDesired);
            }

            if (latestTick > LatestRemote)
            {
                LatestRemote = latestTick;
                shouldUpdateEstimate = true;
                ShouldTick = true;
            }
        }

        // See http://www.gamedev.net/topic/652186-de-jitter-buffer-on-both-the-client-and-server/
        public void Update()
        {
            if (!ShouldTick) return;

            ++EstimatedRemote;
            if (!shouldUpdateEstimate) return;

            int delta = LatestRemote - EstimatedRemote;

            if (ShouldSnapTick(delta))
            {
                EstimatedRemote = LatestRemote - DelayDesired;
                return;
            }

            EstimatedRemote += ComputeOffset(delta, DelayDesired);
            shouldUpdateEstimate = false;
        }

        private static int ComputeOffset(int current, uint desired)
        {
            long delta = current - desired;
            if (Math.Abs(delta) <= 1)
            {
                return (int) delta;
            }

            return (int) Math.Round(delta / 2.0);
        }

        private bool ShouldSnapTick(float delta)
        {
            if (delta < delayMin - remoteRate) return true;
            if (delta > delayMax + remoteRate) return true;
            return false;
        }
    }
}
