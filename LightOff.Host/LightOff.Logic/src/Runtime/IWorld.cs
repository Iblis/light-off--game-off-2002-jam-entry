// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using System.Collections.Generic;
using System.Numerics;

namespace LightOff.Logic
{
    public interface IWorld
    {
        IEnumerable<IObstacle> Obstacles { get; }

        void AddTracker(IEntity tracker);
        void ApplyHitsBetweenTrackersAndGhost();
        Vector2 GetValidMovementVectorFor(IEntityState entityState, Vector2 movement);
        void SetGhost(IEntity ghost);
    }
}
