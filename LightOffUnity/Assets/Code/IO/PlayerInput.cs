// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using System.Numerics;

namespace LightOff.IO
{
    public struct PlayerInput
    {
        public float DirectionX => _currentInputData.X;

        public float DirectionY => _currentInputData.Y;

        public PlayerInput(Vector2 input)
        {
            _currentInputData = input;
        }
        
        public bool Update(Vector2 inputData)
        {
            if(inputData != _currentInputData)
            {
                _currentInputData = inputData;
                return true;
            }
            return false;
        }

        Vector2 _currentInputData;
    }
}
