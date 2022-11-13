// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using RailgunNet.Logic;
using RailgunNet.Util.Pooling;

namespace LightOff.Messaging
{
    public class EntityState : RailState, IRailPoolable<RailState>
    {        
        [Mutable] public int EntityId { get; set; }
        [Mutable][Compressor(typeof(CoordinateCompressor))] public float PosX { get; set; }
        [Mutable][Compressor(typeof(CoordinateCompressor))] public float PosY { get; set; }
        [Mutable][Compressor(typeof(CoordinateCompressor))] public float Angle { get; set; }
    }
}
