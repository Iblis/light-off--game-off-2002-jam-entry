// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.ClientLogic;
using LightOff.IO.Entity;
using LightOff.IO.WebSocket;
using LightOff.Messaging;
using MessagePipe;
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

            builder.RegisterFactory<RailClient>((IObjectResolver container) =>
            {
                IEntitySignals signals = null; 
                return () =>
                {
                    if (signals == null)
                    {
                        signals = container.Resolve<IEntitySignals>();
                        registry.AddEventType<EventMessage>(new[] { GlobalMessagePipe.GetPublisher<EventMessage>() });
                        registry.AddEntityType<EntityClient, EntityState>(new[] { signals });
                    }
                    return new RailClient(registry);
                };
            }, Lifetime.Singleton);

            builder.Register<ISessionConnection, RailgunWebSocketConnect>(Lifetime.Singleton);
        }
    }
}
