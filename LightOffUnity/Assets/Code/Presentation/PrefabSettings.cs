// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Presentation.View;
using System.Collections.Generic;
using UnityEngine;

namespace LightOff.Presentation
{
    [CreateAssetMenu(fileName = "Prefab Settings", menuName = "LightOff/Settings/Prefab")]
    public class PrefabSettings : ScriptableObject
    {
        public Tracker TrackerPrefab;

        public Ghost GhostPrefab;

        public GameObject ObstaclePrefab;

        public ConnectionView ConnectionViewPrefab;

        public ReadyView ReadyViewPrefab;

        public MatchEndedView MatchEndedPrefab;

        public List<Color> ColorSlots;
    }
}
