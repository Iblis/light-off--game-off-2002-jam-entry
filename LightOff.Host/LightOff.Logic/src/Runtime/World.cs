// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using System;

namespace LightOff.Logic
{
    public abstract class World
    {
        protected void ApplyCommand(IEntity entity, IMoveCommand command, float fixedDeltaTime)
        {
            entity.PosX += command.DirectionX * MOVEMENT_SPEED * fixedDeltaTime;
            entity.PosY += command.DirectionY * MOVEMENT_SPEED * fixedDeltaTime;
            
            if (command.DirectionX != 0 || command.DirectionY != 0)
            {
                entity.Angle = MathF.Atan2(command.DirectionX, command.DirectionY);
            }
        }

        const float MOVEMENT_SPEED = 1.5f;
    }
}
