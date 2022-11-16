// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Logic;
using LightOff.Level.Grid;
using SharpMath2;
using System.Numerics;

namespace LightOff.Level
{
    internal class HashableShape : IHashable, IObstacle
    {
        public HashableShape(Rect2 rect, Vector2 vector2)
        {
            Rect = rect;
            Position = vector2;
            Min = rect.Min + Position;
            Max = rect.Max + Position;
        }

        public Vector2 Min { get; private set; }
        public Vector2 Max { get; private set; }

        public float SizeX => Max.X - Min.X;

        public float SizeY => Max.Y - Min.Y;

        public Rect2 Rect { get; private set; }

        public Vector2 Position { get; private set; }

        (CellIndex, CellIndex)? IHashable.RegisteredHashBounds { get ; set; }
        int IHashable.QueryId { get ; set ; }
    }
}
