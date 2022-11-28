// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LightOff.Presentation.View
{
    public class ConnectionView : ViewBase
    {
        public Button ConnectButton { get; private set; }

        public TextField MatchNameInput { get; private set; }

        public TextField PlayerNameInput { get; private set; }

        public void Awake()
        {
            _rootElement = gameObject.GetComponent<UIDocument>().rootVisualElement;
            ConnectButton = _rootElement.Q<Button>("connectionButton");
            MatchNameInput = _rootElement.Q<TextField>("matchName");
            PlayerNameInput = _rootElement.Q<TextField>("playerName");
        }
    }
}
