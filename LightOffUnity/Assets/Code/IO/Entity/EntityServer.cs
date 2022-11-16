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
        IEntityState IEntity.State => State;

        public ServerWorld CommandHandler { get; set; }
        
        protected override void ApplyCommand(MoveCommand toApply)
        {
            CommandHandler.ApplyCommand(this, toApply);       
        }
    }
}
