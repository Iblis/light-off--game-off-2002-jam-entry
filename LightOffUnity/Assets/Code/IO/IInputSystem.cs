// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.IO.Entity;
using LightOff.Messaging;

namespace LightOff.IO
{
    public interface IInputSystem
    {
        void SetLocallyControlledEntity(EntityClient entity);
        void WriteCommand(EntityClient entity, MoveCommand command);
    }
}