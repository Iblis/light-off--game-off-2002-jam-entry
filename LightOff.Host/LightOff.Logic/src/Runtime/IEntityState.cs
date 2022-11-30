// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using System.Numerics;

namespace LightOff.Logic
{
    public interface IEntityState
    {
        Vector2 Position { get; set; }
        uint HitCooldown { get; set; }
        bool ExecuteAction { get; set; }
        float Angle { get; set; }
        bool IsHit { get; }

        uint Visibility { get; set; }
        bool IsReady { get; set; }
        int PlayerSlot { get; set; }
        uint Health { get; set; }

        void ApplyHit();
    }
}
