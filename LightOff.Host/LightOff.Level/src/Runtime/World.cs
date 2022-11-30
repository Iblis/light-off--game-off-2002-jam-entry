// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Level.Grid;
using LightOff.Logic;
using SharpMath2;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace LightOff.Level
{
    public class World : IWorld
    {
        public IEnumerable<IObstacle> Obstacles { get; }
        public IEnumerable<IEntity> Players => _players;

        public bool IsPreparedForMatch => _ghost != null;

        public World() 
        {
            _spatialHash = LevelGeometry.Create();
            Obstacles = new List<IObstacle>(_spatialHash.GetAllColliders().Distinct());
            _players = new List<IEntity>();
        }

        public void AddPlayer(IEntity entity)
        {
            _players.Add(entity);
        }

        public void RemovePlayer(IEntity player)
        {
            // TODO: log if we try to remove a player that does not exist in this list
            _players.Remove(player);
        }

        public void SetGhost(IEntity ghost)
        {
            _ghost = ghost;
            _trackers = _players.Where(p => p != ghost).ToList();
        }

        // TODO: only done by Server, move to a 'ServerWorld' class
        public WinState DetermineWinState()
        {
            // when this gets called, the ghost is definetly set
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var ghostIsDefeatded = _ghost.State.Health == 0;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            var trackesAreDefeated = _trackers.Any() && _trackers.All(p =>  p.State.Health == 0);
            if (ghostIsDefeatded && trackesAreDefeated)
            {
                return WinState.Draw;
            }
            if (ghostIsDefeatded)
            {
                return WinState.Trackers;
            }
            if (trackesAreDefeated)
            {
                return WinState.Ghost;
            }
            return WinState.None;
        }

        // TODO: only done by Server, move to a 'ServerWorld' class
        public void ApplyHitsBetweenTrackersAndGhost()
        {
            if(_ghost == null || _trackers == null)
            {
                return;
            }

            var ghostState = _ghost.State;

            foreach (var tracker in _trackers)
            {
                var trackerState = tracker.State;
                var ghostPos = new Vector2(ghostState.Position.X - 0.5f, ghostState.Position.Y - 0.5f);
                var trackerPos = new Vector2(trackerState.Position.X - 0.5f, trackerState.Position.Y - 0.5f);
                
                if(!trackerState.IsHit
                    && Circle2.Intersects(_playerCircle, _playerCircle, trackerPos, ghostPos, false))
                {
                    trackerState.ApplyHit();
                }

                // early out if lightcone is off or ghost is too far away
                if (!trackerState.ExecuteAction || Vector2.Distance(trackerPos, ghostPos) > 3)
                {
                    continue;
                }

                // check lightcone
                if (Shape2.Intersects(_flashlightCone, _playerCircle, trackerState.Position, ghostPos, new Rotation2(trackerState.Angle * -1), false))
                {
                    // make sure there is no obtacle bewteen lightcone and ghost
                    var line = new Line2(trackerState.Position, ghostState.Position);
                    var cellIndex = _spatialHash.GetCellIndex(trackerState.Position);
                    var colliders = _spatialHash.FindNearbyColliders(cellIndex, 4);
                    if (colliders.Count == 0)
                    {
                        ghostState.ApplyHit();
                    }
                    else
                    {
                        bool obstacleIntersection = false;
                        foreach (var collider in colliders)
                        {
                            obstacleIntersection |= AxisAlignedLine2.Intersects(line.MinX, line.MaxX, collider.Min.X, collider.Max.X, true)
                                && AxisAlignedLine2.Intersects(line.MinY, line.MaxY, collider.Min.Y, collider.Max.Y, true);
                        }
                        if (!obstacleIntersection)
                        {
                            ghostState.ApplyHit();
                        }
                    }                    
                }
            }
        }

        public Vector2 GetValidMovementVectorFor(IEntityState entityState, Vector2 movement)
        {
            var futureEntityPosition = entityState.Position + movement;
            var cellIndex = _spatialHash.GetCellIndex(futureEntityPosition);
            var colliders = _spatialHash.FindNearbyColliders(cellIndex, 2);
            
            futureEntityPosition.X -= 0.5f;
            futureEntityPosition.Y -= 0.5f;

            foreach (var collider in colliders)
            {
                var mtv = Rect2.IntersectMTV(_playerCircle, collider.Rect, futureEntityPosition, collider.Position);
                if(mtv != null)
                {
                    return mtv.Item1 * mtv.Item2;
                }
            }
            return movement;
        }

        public void Clear()
        {
            _players.Clear();
            _trackers?.Clear();
            _ghost = null;
        }

        readonly SpatialHash<HashableShape> _spatialHash;
        readonly List<IEntity> _players = new();
        readonly static Circle2 _playerCircle = new Circle2(0.5f);
        readonly static Polygon2 _flashlightCone = new Polygon2(new[] { new Vector2(0, 0), new Vector2(-1, 2.5f), new Vector2(0, 3), new Vector2(1, 2.5f) }, Vector2.Zero);
        
        List<IEntity>? _trackers;
        IEntity? _ghost;
    }
}
