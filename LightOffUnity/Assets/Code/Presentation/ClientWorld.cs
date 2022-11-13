// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.IO;
using LightOff.IO.Entity;
using LightOff.Logic;
using LightOff.Messaging;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace LightOff.Presentation
{
    internal class ClientWorld : World, IClientWorld, ITickable
    {
        public ClientWorld(Func<GameObject> playerFactory)
        {
            _playerFactory = playerFactory;
        }

        public void Tick()
        {
            foreach (var player in _players)
            {
                var transform = player.Value.transform;
                transform.position = player.Key.Position;
                transform.rotation = Quaternion.AngleAxis(player.Key.AngleInDegrees, Vector3.back);
            }
        }

        void IClientWorld.AddEntity(EntityClient entity)
        {
            var newPlayer = _playerFactory();
            _players.Add(entity, newPlayer);
        }

        void IClientWorld.ApplyCommand(EntityClient entity, MoveCommand command)
        {
            base.ApplyCommand(entity, command, Time.fixedDeltaTime);
        }

        void IClientWorld.RemoveEntity(EntityClient entity)
        {
            if(_players.TryGetValue(entity, out var player))
            {
                // TODO: Destroy GameObject
                _players.Remove(entity);
            }

        }

        readonly Func<GameObject> _playerFactory;
        readonly Dictionary<EntityClient, GameObject> _players = new ();
    }
}
