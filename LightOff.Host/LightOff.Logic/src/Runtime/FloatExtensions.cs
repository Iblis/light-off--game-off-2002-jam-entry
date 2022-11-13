// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using System;

namespace LightOff.Logic.src.Runtime
{
    public static class FloatExtensions
    {

        public static bool IsNear(this float a, float b)
        {
            return MathF.Abs(a - b) < COORDINATE_PRECISION;
        }

        public static bool IsNearZero(this float a)
        {
            return a.IsNear(ZERO);
        }

        const float ZERO = 0.0f;
        public const float COORDINATE_PRECISION = 0.001f;
    }
}
