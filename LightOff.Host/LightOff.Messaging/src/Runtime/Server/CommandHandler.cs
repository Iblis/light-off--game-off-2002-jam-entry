// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Logic;

namespace LightOff.Messaging.Server
{
    public class CommandHandler : Logic.CommandHandler
    {
        public CommandHandler(IWorld world) : base(world)
        {
        }

        public void UpdateDeltaTime(float deltaTime)
        {
            _deltaTime = deltaTime;
        }

        internal void ApplyCommand(EntityServer entity, MoveCommand command)
        {
            if(entity.State.Health == 0)
            {
                return;
            }
            command.EnsureCorrectPrecision();
            base.ApplyCommand(entity.State, command, _deltaTime);
            _world.ApplyHitsBetweenTrackersAndGhost();
        }

        internal void ApplyRemoveCommand(EntityServer entity)
        {
            _world.RemovePlayer(entity);
        }

        float _deltaTime;
    }
}
