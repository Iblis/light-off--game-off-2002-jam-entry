// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using System;
using System.Numerics;

namespace LightOff.Logic
{
    public abstract class CommandHandler
    {
        protected CommandHandler(IWorld world) 
        {
            _world = world;
        }

        protected void ApplyCommand(IEntityState entityState, IMoveCommand command, float fixedDeltaTime)
        {
            // TODO: command.Direction to Vector2
            var movement = new Vector2(command.DirectionX, command.DirectionY) * MOVEMENT_SPEED * fixedDeltaTime;
            var actualMovement = _world.GetValidMovementVectorFor(entityState, movement);
            entityState.Position += actualMovement;
            
            if (command.DirectionX != 0 || command.DirectionY != 0)
            {
                // TODO: does the angle change if we 'bounce' of the wall?
                // Probably not so this should still be correct!
                entityState.Angle = MathF.Atan2(command.DirectionX, command.DirectionY);
            }
        }

        protected readonly IWorld _world;

        const float MOVEMENT_SPEED = 3.5f;
    }
}
