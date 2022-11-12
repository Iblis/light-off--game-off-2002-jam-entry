// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using System;
using UnityEngine;
using VContainer.Unity;

namespace LightOff.Presentation
{
    internal class Bootstrapper : IStartable
    {
        public Bootstrapper(Func<GameObject> playerFactory, IMovementSystem movementSystem)
        {
            _playerFactory = playerFactory;
            _movementSystem = movementSystem;
        }

        public void Start()
        {
            var player = _playerFactory();
            _movementSystem.SetPlayer(player);
        }

        readonly Func<GameObject> _playerFactory;
        readonly IMovementSystem _movementSystem;
    }
}
