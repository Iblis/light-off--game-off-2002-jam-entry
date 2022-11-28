// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.ClientLogic;
using LightOff.IO;
using LightOff.IO.Entity;
using LightOff.Level;
using LightOff.Logic;
using LightOff.Messaging;
using MessagePipe;
using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace LightOff.Presentation
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField]
        PrefabSettings _prefabSettings;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_prefabSettings);
            var options = builder.RegisterMessagePipe(/* configure option */);
            builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));
            builder.RegisterMessageBroker<EventMessage>(options);


            builder.Register<IWorld, World>(Lifetime.Transient);
            builder.Register<IServer, DummyServer>(Lifetime.Singleton);

            builder.Register<IEntitySignals, EntitySignals>(Lifetime.Singleton);
            builder.RegisterEntryPoint<InputSystem>(Lifetime.Singleton);
            builder.RegisterEntryPoint<ClientWorld>(Lifetime.Singleton);
            //builder.RegisterEntryPoint<TestRailgun>(Lifetime.Singleton);

            builder.Register<IGameSession, GameSession>(Lifetime.Singleton);

            var guiInstaller = new GuiInstaller(_prefabSettings);
            guiInstaller.Install(builder);

            var installer = new RailgunInstaller();
            installer.Install(builder);


            builder.RegisterFactory<GameObject>((IObjectResolver container) =>
            {
                return () =>
                {
                    return container.Instantiate(_prefabSettings.ObstaclePrefab);
                };
            }, Lifetime.Singleton);

            builder.RegisterFactory<Tracker>((IObjectResolver container) =>
            {
                return () =>
                {
                    return container.Instantiate(_prefabSettings.TrackerPrefab);
                };
            }, Lifetime.Singleton);

            builder.RegisterFactory<Ghost>((IObjectResolver container) =>
            {
                return () =>
                {
                    return container.Instantiate(_prefabSettings.GhostPrefab);
                };
            }, Lifetime.Singleton);

            builder.RegisterFactory<EntityClient, PlayerEntity>((IObjectResolver container) =>
            {
                var trackerFactory = container.Resolve<Func<Tracker>>();
                var ghostFactory = container.Resolve<Func<Ghost>>();
                return (EntityClient entity) =>
                {
                    return new PlayerEntity(entity, _prefabSettings, trackerFactory, ghostFactory);
                };
            }, Lifetime.Singleton);
        }
    }
}
