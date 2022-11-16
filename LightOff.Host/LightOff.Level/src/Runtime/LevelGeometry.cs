// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Level.Grid;
using SharpMath2;
using System.Numerics;

namespace LightOff.Level
{
    internal static class LevelGeometry
    {
        internal static SpatialHash<HashableShape> Create()
        {
            var spatialHash =  new SpatialHash<HashableShape>(32, 20, 1);
            CreateLevelGeometry(spatialHash);
            return spatialHash;
        }

        private static void CreateLevelGeometry(SpatialHash<HashableShape> spatialHash) 
        {
            // Left Boundary
            spatialHash.Insert(new HashableShape(_leftRight, new Vector2(0.25f, 10)));
            // Right Boundary
            spatialHash.Insert(new HashableShape(_leftRight, new Vector2(35.75f, 10)));
            // Top Boundary
            spatialHash.Insert(new HashableShape(_topBottom, new Vector2(18, 19.75f)));
            // Bottom boundary
            spatialHash.Insert(new HashableShape(_topBottom, new Vector2(18, 0.25f)));
            
            // Walls in Level
            spatialHash.Insert(new HashableShape(_smallHorizontal, new Vector2(7.25f, 16)));
            spatialHash.Insert(new HashableShape(_smallVertical, new Vector2(4, 14.25f)));
        }
 
        public static void Insert(this SpatialHash<HashableShape> spatialHash, HashableShape shape)
        {
            spatialHash.Insert(shape, shape.Min, shape.Max);
        }

        readonly static Rect2 _leftRight = new Rect2(-0.75f, -10, 0.75f, 10);
        readonly static Rect2 _topBottom = new Rect2(-18, -0.75f, 18, 0.75f);
        readonly static Rect2 _smallHorizontal = new Rect2(-2.5f, -0.75f, 2.5f, 0.75f);
        readonly static Rect2 _smallVertical = new Rect2(-0.75f, -2.5f, 0.75f, 2.5f);
    }
}
