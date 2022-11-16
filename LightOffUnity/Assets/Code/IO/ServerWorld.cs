// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.IO.Entity;
using LightOff.Logic;
using LightOff.Messaging;
using UnityEngine;

namespace LightOff.IO
{
    // TODO: maybe rename this class to avoid confusion with IWorld / PhysicWorld
    internal class ServerWorld : CommandHandler
    {
        public ServerWorld(IWorld world) : base(world) { }

        internal void ApplyCommand(EntityServer entity, MoveCommand command)
        {
            command.EnsureCorrectPrecision();           
            base.ApplyCommand(entity.State, command, Time.fixedDeltaTime);
        }
    }
}
