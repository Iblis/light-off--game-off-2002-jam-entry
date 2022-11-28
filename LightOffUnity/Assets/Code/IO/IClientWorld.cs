// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Cysharp.Threading.Tasks;
using LightOff.IO.Entity;
using LightOff.Messaging;
using RailgunNet.System.Types;

namespace LightOff.IO
{
    public interface IClientWorld
    {
        ReadOnlyAsyncReactiveProperty<EntityClient> LocalPlayer { get; }

        void AddEntity(EntityClient entity);
        void ApplyCommand(EntityClient entity, MoveCommand command);
        void RemoveEntity(EntityId entityId);
        void SetLocallControlledEntity(EntityClient entity);
    }
}
