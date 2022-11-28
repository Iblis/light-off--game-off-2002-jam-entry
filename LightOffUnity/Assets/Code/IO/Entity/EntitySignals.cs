// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Messaging;

namespace LightOff.IO.Entity
{
    public class EntitySignals : IEntitySignals
    {
        public EntitySignals(IInputSystem inputSystem, IClientWorld world) 
        {
            _inputSystem = inputSystem;
            _world = world;
        }

        public void OnAddEntity(EntityClient entity)
        {
            _world.AddEntity(entity);
        }

        public void OnRemoveEntity(EntityClient entity)
        {
            _world.RemoveEntity(entity.Id);
        }

        public void OnLocallyControlled(EntityClient entity) 
        {
            if (entity.IsControlled)
            {
                _inputSystem.SetLocallyControlledEntity(entity);
                _world.SetLocallControlledEntity(entity);
            }
        }

        public void OnApplyCommand(EntityClient entity, MoveCommand command) 
        {
            _world.ApplyCommand(entity, command);
        }

        public void OnWriteCommand(EntityClient entity, MoveCommand command)
        {
            _inputSystem.WriteCommand(entity, command);
        }

        readonly IInputSystem _inputSystem;
        readonly IClientWorld _world;
    }
}
