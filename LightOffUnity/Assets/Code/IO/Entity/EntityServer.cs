// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Logic;
using LightOff.Messaging;
using RailgunNet.Logic;

namespace LightOff.IO.Entity
{
    internal class EntityServer : RailEntityServer<EntityState, MoveCommand>, IEntity
    {
        public ServerWorld World { get; set; }
        
        protected override void ApplyCommand(MoveCommand toApply)
        {
            World.ApplyCommand(this, toApply);       
        }

        float IEntity.PosX { get => State.PosX; set => State.PosX = value; }
        float IEntity.PosY { get => State.PosY; set => State.PosY = value; }
        float IEntity.Angle { get => State.Angle; set => State.Angle = value; }

    }
}
