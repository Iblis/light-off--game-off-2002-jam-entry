// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using UnityEngine;
using VContainer.Unity;

namespace LightOff.Presentation
{
    public class InputSystem : IActiveInputAccessor, ITickable
    {
        PlayerInput IActiveInputAccessor.InputPlayerOne => _inputPlayerOne;

        public InputSystem()
        {
            _inputPlayerOne = new PlayerInput();
        }


        public void Tick()
        {
            var keyDown = Input.GetKey(KeyCode.DownArrow) ? 1 : 0;
            var keyUp = Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
            var keyLeft = Input.GetKey(KeyCode.LeftArrow) ? 1 : 0;
            var keyRight = Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
            var input = new System.Numerics.Vector2(keyRight - keyLeft, keyUp - keyDown);
            _inputPlayerOne.Update(input);
        }

        readonly PlayerInput _inputPlayerOne;
    }
}
