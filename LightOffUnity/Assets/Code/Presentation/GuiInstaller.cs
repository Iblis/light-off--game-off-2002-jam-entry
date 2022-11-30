// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Presentation.Presenter;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace LightOff.Presentation
{
    internal class GuiInstaller : IInstaller
    {
        public GuiInstaller(PrefabSettings settings) 
        {
            _settings = settings;
        }

        public void Install(IContainerBuilder builder)
        {
            RegisterView(_settings.ConnectionViewPrefab, builder);
            RegisterView(_settings.ReadyViewPrefab, builder);
            RegisterView(_settings.MatchEndedPrefab, builder);
            builder.RegisterEntryPoint<PreMatchPresenter>(Lifetime.Singleton);
        }

        void RegisterView<T>(T prefab, IContainerBuilder builder) where T : MonoBehaviour
        {
            var view = Object.Instantiate(prefab);
            builder.RegisterComponent(view);
        }
        PrefabSettings _settings;
    }
}
