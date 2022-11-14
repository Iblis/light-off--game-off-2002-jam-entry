﻿// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using SharpMath2;
using System;
using System.Diagnostics;
using System.Numerics;

namespace LightOff.Logic
{
    public abstract class World
    {
        protected World() 
        {
            _ghost = new Rect2(0, 0, 1, 1);
            _ghostPosition = new Vector2(-1f, -4f);
        }

        protected void ApplyCommand(IEntity entity, IMoveCommand command, float fixedDeltaTime)
        {
            entity.PosX += command.DirectionX * MOVEMENT_SPEED * fixedDeltaTime;
            entity.PosY += command.DirectionY * MOVEMENT_SPEED * fixedDeltaTime;
            
            if (command.DirectionX != 0 || command.DirectionY != 0)
            {
                entity.Angle = MathF.Atan2(command.DirectionX, command.DirectionY);
            }
            var conePosition = new Vector2(entity.PosX, entity.PosY);
            var rotation = new Rotation2(entity.Angle * -1);
            var detectedGhost = Polygon2.Intersects(_flashlightCone, _ghost, conePosition, _ghostPosition, rotation, false);
            /*var color = UnityEngine.Color.yellow;
            if (detectedGhost)
            {
                //UnityEngine.Debug.Log("Ghost was detected!");
                color = UnityEngine.Color.red;
            }
            UnityEngine.Debug.DrawLine(new UnityEngine.Vector3(_ghost.Min.X + _ghostPosition.X, _ghost.Min.Y + _ghostPosition.Y),
                new UnityEngine.Vector3(_ghost.Max.X + _ghostPosition.X, _ghost.Max.Y + _ghostPosition.Y), color);

            var vertices = Polygon2.ActualizePolygon(_flashlightCone, conePosition, rotation);
            
            UnityEngine.Debug.DrawLine(new UnityEngine.Vector3(vertices[0].X, vertices[0].Y),
                new UnityEngine.Vector3(vertices[1].X, vertices[1].Y), color);
            UnityEngine.Debug.DrawLine(new UnityEngine.Vector3(vertices[1].X, vertices[1].Y),
                            new UnityEngine.Vector3(vertices[2].X, vertices[2].Y), color);
            UnityEngine.Debug.DrawLine(new UnityEngine.Vector3(vertices[2].X, vertices[2].Y),
                            new UnityEngine.Vector3(vertices[3].X, vertices[3].Y), color);
            UnityEngine.Debug.DrawLine(new UnityEngine.Vector3(vertices[3].X, vertices[3].Y),
                            new UnityEngine.Vector3(vertices[0].X, vertices[0].Y), color);
            */
        }

        readonly Polygon2 _flashlightCone = new Polygon2(new[] { new Vector2(0, 0), new Vector2(-1, 2.5f), new Vector2(0, 3), new Vector2(1, 2.5f) }, Vector2.Zero);
        readonly Rect2 _ghost;
        Vector2 _ghostPosition;

        const float MOVEMENT_SPEED = 1.5f;
    }
}
