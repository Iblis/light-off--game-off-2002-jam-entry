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
    internal class ClientWorld : CommandHandler, IClientWorld, ITickable
    {
        public ClientWorld(IWorld world, Func<Player> playerFactory, Func<GameObject> obstacleFactory ) : base(world)
        {
            _world = world;
            _playerFactory = playerFactory;
            foreach(var obstacle in _world.Obstacles)
            {
                var obstacleGO = obstacleFactory();
                obstacleGO.transform.position = new Vector3(obstacle.Position.X, obstacle.Position.Y, 0);
                obstacleGO.transform.localScale = new Vector3(obstacle.SizeX, obstacle.SizeY, 1);
            }
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
            if (entity.State.EntityTypeId == 0)
            {
                _world.AddTracker(entity);
            }
            else
            {
                _world.SetGhost(entity);
            }
            _world.SetGhost(new DummyGhost());
        }

        void IClientWorld.ApplyCommand(EntityClient entity, MoveCommand command)
        {
            ApplyCommand(entity.State, command, Time.fixedDeltaTime);
        }

        void IClientWorld.RemoveEntity(EntityClient entity)
        {
            if(_players.TryGetValue(entity, out var player))
            {
                // TODO: Destroy GameObject
                _players.Remove(entity);
            }
        }

        readonly IWorld _world;
        readonly Func<Player> _playerFactory;
        readonly Func<GameObject> _obstacleFactory;
        readonly Dictionary<EntityClient, Player> _players = new ();
    }
}
