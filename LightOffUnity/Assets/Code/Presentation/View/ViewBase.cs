// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using UnityEngine;
using UnityEngine.UIElements;

namespace LightOff.Presentation.View
{
    public class ViewBase : MonoBehaviour
    {
        internal virtual void SetActive(bool active)
        {
            _rootElement.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected VisualElement _rootElement;
    }
}
