// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Logic;
using LightOff.Logic.src.Runtime;
using RailgunNet.Logic;

namespace LightOff.Messaging
{
    public class MoveCommand : RailCommand, IMoveCommand
    {
        [CommandData][Compressor(typeof(CoordinateCompressor))] public float DirectionX { get; set; }
        [CommandData][Compressor(typeof(CoordinateCompressor))] public float DirectionY { get; set; }

        public void EnsureCorrectPrecision()
        {
            if(DirectionX.IsNearZero())
            {
                DirectionX = 0;
            }
            if(DirectionY.IsNearZero()) 
            {
                DirectionY = 0;
            }
        }
    }
}
