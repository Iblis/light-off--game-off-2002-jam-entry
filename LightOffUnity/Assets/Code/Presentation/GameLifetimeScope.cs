// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.IO;
using LightOff.IO.Entity;
using LightOff.Level;
using LightOff.Logic;
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

            builder.Register<IWorld, World>(Lifetime.Transient);
            builder.Register<IServer, DummyServer>(Lifetime.Singleton);

            builder.Register<IEntitySignals, EntitySignals>(Lifetime.Singleton);
            builder.RegisterEntryPoint<InputSystem>(Lifetime.Singleton);
            builder.RegisterEntryPoint<ClientWorld>(Lifetime.Singleton);
            builder.RegisterEntryPoint<TestRailgun>(Lifetime.Singleton);

            builder.RegisterFactory<Player>((IObjectResolver container) =>
            {
                return () =>
                {
                    return container.Instantiate(_prefabSettings.PlayerPrefab);
                };
            }, Lifetime.Singleton);

            builder.RegisterFactory<GameObject>((IObjectResolver container) =>
            {
                return () =>
                {
                    return container.Instantiate(_prefabSettings.ObstaclePrefab);
                };
            }, Lifetime.Singleton);
        }
    }
}
