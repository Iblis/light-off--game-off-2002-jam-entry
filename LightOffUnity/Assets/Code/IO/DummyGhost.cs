// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Logic;
using System.Numerics;

namespace LightOff.IO
{
    public class DummyGhost : IEntity
    {
        public DummyGhost() 
        {
            State = new DummyState();
            State.Position = new Vector2(3,3);
        }

        public IEntityState State { get; private set; }
    }

    internal class DummyState : IEntityState
    {
        public Vector2 Position { get; set; }
        public int HitCooldown { get; set; }
        public bool ExecutesAction { get ; set; }
        public float Angle { get; set ; }

        public bool IsHit => HitCooldown > 0;

        public bool IsVisible => false;

        public void ApplyHit()
        {
            //UnityEngine.Debug.Log("Ghost was hit!");
            HitCooldown = 10;
        }
    }
}