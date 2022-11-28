// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using LightOff.ClientLogic;
using LightOff.IO;
using LightOff.IO.Entity;
using LightOff.Logic;
using LightOff.Messaging;
using MessagePipe;
using RailgunNet.System.Types;
using SharpMath2;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using VContainer.Unity;

namespace LightOff.Presentation
{
    internal class ClientWorld : CommandHandler, IClientWorld, ITickable
    {
        public ReadOnlyAsyncReactiveProperty<EntityClient> LocalPlayer { get; }

        public ClientWorld(IWorld world, 
                            IGameSession session, 
                            Func<EntityClient, PlayerEntity> playerFactory, 
                            Func<GameObject> obstacleFactory ) 
            : base(world)
        {
            _playerFactory = playerFactory;
            _session = session;
            LocalPlayer = _localPlayer.ToReadOnlyAsyncReactiveProperty(CancellationToken.None);
            foreach(var obstacle in _world.Obstacles)
            {
                var obstacleGO = obstacleFactory();
                obstacleGO.transform.position = new Vector3(obstacle.Position.X, obstacle.Position.Y, 0);
                obstacleGO.transform.localScale = new Vector3(obstacle.SizeX, obstacle.SizeY, 1);
            }
            // TODO: dispose
            _subscription = session.SessionState.Subscribe(OnSessionStateChanged);
        }

        void OnSessionStateChanged(SessionConnectionState sessionState)
        {
            if(sessionState == SessionConnectionState.InMatch)
            {
                UnityEngine.Debug.Log("Match started");
                // make sure world is in a clean state
                _world.Clear();
                // marker to inform update-loop to spawn entites in next run
                _spawnPlayer = true;
            }
            else if(sessionState == SessionConnectionState.AfterMatch)
            {
                foreach (var player in _players.Values)
                {
                    player.RemoveFrom(_world);
                }
                _players.Clear();
            }
        }

        public void Tick()
        {
            _session.Update();
            
            foreach (var player in _players.Values)
            {
                if(_spawnPlayer)
                {
                    UnityEngine.Debug.Log("Spawning players");
                    player.SpawnIn(_world);
                }
                player.Update();
            }
            if(_spawnPlayer)
            {
                _spawnPlayer = false;
            }
        }

        void IClientWorld.AddEntity(EntityClient entity)
        {
            if(entity.IsControlled)
            {
                _localPlayer.Value = entity;
                UnityEngine.Debug.Log("local entity set in ClientWorld");
            }
            var newPlayer = _playerFactory(entity);
            _players.Add(entity.Id, newPlayer);
        }

        void IClientWorld.ApplyCommand(EntityClient entity, MoveCommand command)
        {
            ApplyCommand(entity.State, command, Time.fixedDeltaTime);

            /*var conePosition = new System.Numerics.Vector2(entity.State.PosX, entity.State.PosY);
            var rotation = new Rotation2(entity.State.Angle * -1);
            var vertices = Polygon2.ActualizePolygon(_flashlightCone, conePosition, rotation);
            var color = UnityEngine.Color.yellow;
            UnityEngine.Debug.DrawLine(new UnityEngine.Vector3(vertices[0].X, vertices[0].Y),
                new UnityEngine.Vector3(vertices[1].X, vertices[1].Y), color);
            UnityEngine.Debug.DrawLine(new UnityEngine.Vector3(vertices[1].X, vertices[1].Y),
                            new UnityEngine.Vector3(vertices[2].X, vertices[2].Y), color);
            UnityEngine.Debug.DrawLine(new UnityEngine.Vector3(vertices[2].X, vertices[2].Y),
                            new UnityEngine.Vector3(vertices[3].X, vertices[3].Y), color);
            UnityEngine.Debug.DrawLine(new UnityEngine.Vector3(vertices[3].X, vertices[3].Y),
                            new UnityEngine.Vector3(vertices[0].X, vertices[0].Y), color);
            var ghostPosition = new System.Numerics.Vector2(3, 3);
            var detectedGhost = Polygon2.Intersects(_flashlightCone, _playerRect, conePosition, ghostPosition, rotation, false);
            
            if (detectedGhost)
            {
                //UnityEngine.Debug.Log("Ghost was detected!");
                color = UnityEngine.Color.red;
            }
            
            UnityEngine.Debug.DrawLine(new UnityEngine.Vector3(ghostPosition.X -0.5f, ghostPosition.Y -0.5f),
                new UnityEngine.Vector3(ghostPosition.X + 0.5f, ghostPosition.Y + 0.5f), color);
            */
        }
        // For debug drawings
        readonly static Circle2 _playerRect = new Circle2(0.5f);
        readonly static Polygon2 _flashlightCone = new Polygon2(new[] { new System.Numerics.Vector2(0, 0), new System.Numerics.Vector2(-1, 2.5f), new System.Numerics.Vector2(0, 3), new System.Numerics.Vector2(1, 2.5f) }, System.Numerics.Vector2.Zero);

        void IClientWorld.RemoveEntity(EntityId entityId)
        {
            if(_players.TryGetValue(entityId, out var player))
            {
                player.RemoveFrom(_world);
                _players.Remove(entityId);
            }
        }

        public void SetLocallControlledEntity(EntityClient entity)
        {
            _localPlayer.Value = entity;
        }

        bool _spawnPlayer;
        readonly IDisposable _subscription;
        readonly Func<EntityClient, PlayerEntity> _playerFactory;
        readonly IGameSession _session;
        readonly Dictionary<EntityId, PlayerEntity> _players = new ();
        readonly AsyncReactiveProperty<EntityClient> _localPlayer = new AsyncReactiveProperty<EntityClient>(null);

    }
}
