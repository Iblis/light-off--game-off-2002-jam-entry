// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.IO.Entity;
using LightOff.Logic;
using RailgunNet.System.Types;
using System;
using UnityEngine;

namespace LightOff.Presentation
{
    internal class PlayerEntity
    {
        public EntityId ID => _entity.Id;

        public PlayerEntity(EntityClient entity, PrefabSettings settings, Func<Tracker> trackerFactory, Func<Ghost> ghostFactory) 
        {
            _entity = entity;
            _settings = settings;
            _trackerFactory = trackerFactory;
            _ghostFactory = ghostFactory;
        }

        public void SpawnIn(IWorld world)
        {
            world.AddPlayer(_entity);
            if (_entity.State.PlayerSlot == 5)
            {
                var ghost = _ghostFactory();
                if(_entity.IsControlled)
                {
                    ghost.MarkAsControlled();
                }
                _player = ghost;
                // we don't set the ghost on the client side. 
                // checks against the ghost can only be performed on server side,
                // client does not know its position most of the time
                //world.SetGhost(_entity);
            }
            else
            {
                var tracker = _trackerFactory();
                tracker.GetComponent<SpriteRenderer>().color = _settings.ColorSlots[_entity.State.PlayerSlot];
                _player = tracker;
            }
        }

        public void Update()
        {
            if (_player != null)
            {
                var state = _entity.State;
                _player.UpdateFrom(state);
                // for now, always remove ghost from being active
                // or is this not enough, maybe need to set visible?
                if(state.PlayerSlot == 5 && !_entity.IsControlled && _player.gameObject.activeSelf)
                {
                    _player.gameObject.SetActive(false);
                }
            }
        }

        internal void RemoveFrom(IWorld world)
        {
            world.RemovePlayer(_entity);
            GameObject.Destroy(_player);
        }

        readonly EntityClient _entity;
        readonly PrefabSettings _settings;
        readonly Func<Tracker> _trackerFactory;
        readonly Func<Ghost> _ghostFactory;
        PlayerBase _player;
    }
}
