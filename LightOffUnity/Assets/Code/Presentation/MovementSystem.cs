// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using UnityEngine;
using VContainer.Unity;

namespace LightOff.Presentation
{
    internal class MovementSystem : IMovementSystem, ITickable
    {
        public MovementSystem(IActiveInputAccessor inputAccessor)
        {
            _inputAccessor = inputAccessor;
        }

        public void SetPlayer(GameObject player)
        {
            _player = player;
        }

        public void Tick()
        {
            var input = _inputAccessor.InputPlayerOne;
            var transform = _player.transform;
            transform.Translate(new Vector3(input.DirectionX, input.DirectionY).normalized * MOVEMENT_SPEED, Space.World);
            if (input.DirectionX != 0 || input.DirectionY != 0)
            {
                float angle = Mathf.Atan2(input.DirectionX, input.DirectionY) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.back);
            }
        }

        GameObject _player;
        readonly IActiveInputAccessor _inputAccessor;

        const float MOVEMENT_SPEED = 0.1f;
    }
}
