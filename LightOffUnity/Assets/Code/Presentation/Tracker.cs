// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Logic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LightOff.Presentation
{
    public class Tracker : PlayerBase
    {
        // Start is called before the first frame update
        protected override void Awake()
        {
            base.Awake();
            _flashLight = GetComponentInChildren<Light2D>();
            _flashLight.enabled = false;
        }

        public override void UpdateFrom(IEntityState state)
        {
            base.UpdateFrom(state);
            _flashLight.enabled = state.ExecuteAction;
            _flashLight.transform.rotation = Quaternion.AngleAxis(state.Angle * Mathf.Rad2Deg, UnityEngine.Vector3.back);
        }

        Light2D _flashLight;

        public GameObject GameObject => gameObject;
    }
}
