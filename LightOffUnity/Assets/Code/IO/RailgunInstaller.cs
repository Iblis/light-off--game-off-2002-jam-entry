// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.IO.Entity;
using LightOff.IO.WebSocket;
using LightOff.Messaging;
using RailgunNet.Connection.Client;
using RailgunNet.Factory;
using VContainer;
using VContainer.Unity;

namespace LightOff.IO
{
    public class RailgunInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            var registry = new RailRegistry(RailgunNet.Component.Client);
            registry.SetCommandType<MoveCommand>();
            //_registry.AddEventType<>();
            builder.RegisterFactory<RailClient>((IObjectResolver container) =>
            {
                var signals = container.Resolve<IEntitySignals>();
                registry.AddEntityType<EntityClient, EntityState>(new[] { signals });
                return () =>
                {
                    return new RailClient(registry);
                };
            }, Lifetime.Singleton);

            builder.RegisterEntryPoint<RailgunWebSocketConnect>(Lifetime.Singleton);
        }
    }
}
