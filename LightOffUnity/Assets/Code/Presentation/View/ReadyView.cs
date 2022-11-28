// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using UnityEngine.UIElements;

namespace LightOff.Presentation.View
{
    public class ReadyView : ViewBase
    {
        public Toggle Toggle { get; private set; }
        public Label WaitingForPlayers { get; private set; }

        public void Awake()
        {
            _rootElement = gameObject.GetComponent<UIDocument>().rootVisualElement;
            Toggle = _rootElement.Q<Toggle>("readyToggle");
            Toggle.SetEnabled(false);
            WaitingForPlayers = _rootElement.Q<Label>("waitingForPlayers");
        }

        internal override void SetActive(bool active)
        {
            base.SetActive(active);
            if(active == false)
            {
                Toggle.value = false;
            }
        }
    }
}
