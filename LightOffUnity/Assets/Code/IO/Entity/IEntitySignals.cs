// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Messaging;

namespace LightOff.IO.Entity
{
    public interface IEntitySignals
    {
        void OnAddEntity(EntityClient entity);
        void OnApplyCommand(EntityClient entity, MoveCommand command);
        void OnLocallyControlled(EntityClient entity);
        void OnRemoveEntity(EntityClient entity);
        void OnWriteCommand(EntityClient entity, MoveCommand command);
    }
}