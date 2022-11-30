// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Logic;
using RailgunNet.Logic;
using RailgunNet.Util.Pooling;
using System.Numerics;

namespace LightOff.Messaging
{
    public class EntityState : RailState, IRailPoolable<RailState>, IEntityState
    {
        [Mutable] public int PlayerSlot { get; set; }
        [Mutable] public uint Health { get; set; }
        [Mutable] public uint HitCooldown { get; set; }
        [Mutable] public bool ExecuteAction { get; set; }
        [Mutable] public uint Visibility { get; set; }
        [Mutable] public bool IsReady { get; set; }
        [Mutable][Compressor(typeof(CoordinateCompressor))] public float PosX { get => _position.X; set => _position.X = value; }
        [Mutable][Compressor(typeof(CoordinateCompressor))] public float PosY { get => _position.Y; set => _position.Y = value; }
        [Mutable][Compressor(typeof(CoordinateCompressor))] public float Angle { get; set; }

        public Vector2 Position { get => _position; set => _position = value; }

        public void ApplyHit()
        {
            HitCooldown = 10;
            Health = 0;
        }

        public bool IsHit => HitCooldown > 0;

        Vector2 _position;
    }
}
