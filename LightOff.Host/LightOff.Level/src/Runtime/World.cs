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
        public IEnumerable<IObstacle> Obstacles { get; private set; }
        
        public World() 
        {
            _spatialHash = LevelGeometry.Create();
            Obstacles = new List<IObstacle>(_spatialHash.GetAllColliders().Distinct());
        }

        public void AddTracker(IEntity tracker)
        {
            _trackers.Add(tracker);
        }

        public void SetGhost(IEntity ghost)
        {
            _ghost = ghost;
        }
        
        public void ApplyHitsBetweenTrackersAndGhost()
        {
            var ghostState = _ghost.State;
            foreach (var tracker in _trackers)
            {
                var trackerState = tracker.State;
                if(!trackerState.IsHit
                    && Circle2.Intersects(_playerRect, _playerRect, ghostState.Position, trackerState.Position, false))
                {
                    trackerState.ApplyHit();
                }
                // TODO: cache rotation in tracker!
                if (trackerState.ExecutesAction
                    && Polygon2.Intersects(_flashlightCone, _playerRect, trackerState.Position, ghostState.Position, new Rotation2(trackerState.Angle * -1), false))
                {
                    ghostState.ApplyHit();
                }
            }
        }

        public Vector2 GetValidMovementVectorFor(IEntityState entityState, Vector2 movement)
        {
            var futureEntityPosition = entityState.Position + movement;
            var cellIndex = _spatialHash.GetCellIndex(futureEntityPosition);
            var colliders = _spatialHash.FindNearbyColliders(cellIndex, 2);

            foreach (var collider in colliders)
            {
                var mtv = Rect2.IntersectMTV(_playerRect, collider.Rect, futureEntityPosition, collider.Position);
                if(mtv != null)
                {
                    return mtv.Item1 * mtv.Item2;
                }
            }
            return movement;
        }

        readonly SpatialHash<HashableShape> _spatialHash;
        readonly List<IEntity> _trackers = new ();
        
        readonly static Circle2 _playerRect = new Circle2(1);
        readonly Polygon2 _flashlightCone = new Polygon2(new[] { new Vector2(0, 0), new Vector2(-1, 2.5f), new Vector2(0, 3), new Vector2(1, 2.5f) }, Vector2.Zero);
        IEntity? _ghost;
    }
}
