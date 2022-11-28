// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Messaging.Server;
using RailgunNet.Logic;
using RailgunNet.Logic.Scope;

namespace LightOff.Host.Session
{
    internal class GhostScopeEvaluator : RailScopeEvaluator
    {
        public override bool Evaluate(RailEntityBase entity, int ticksSinceSend, int ticksSinceAck, out float priority)
        {
            priority = 0.0f;
            if (entity is EntityServer ent)
            {
                return ent.State.PlayerSlot != 5 || ent.State.Visibility > 20;
            }
            return true;
        }
    }
}