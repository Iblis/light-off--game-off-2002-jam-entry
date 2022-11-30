// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Logic;

namespace LightOff.Presentation
{
    public class Ghost : PlayerBase
    {
        public void MarkAsControlled()
        {
            _isControlled = true;
        }

        public override void UpdateFrom(IEntityState state)
        {
            base.UpdateFrom(state);
            if (!_isControlled)
            {
                var color = _renderer.color;
                color.a = state.Visibility / 100;
                _renderer.color = color;
                if (state.Visibility < 25 && gameObject.activeSelf)
                {
                    UnityEngine.Debug.Log($"hiding ghost now {state.Visibility}");
                    gameObject.SetActive(false);
                }
                else if (state.Visibility > 25 && !gameObject.activeSelf)
                {
                    UnityEngine.Debug.Log($"showing ghost again  {state.Visibility}");
                    gameObject.SetActive(true);
                }
            }
        }

        bool _isControlled;
    }
}
