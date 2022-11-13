// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.IO.Entity;
using LightOff.Messaging;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace LightOff.IO
{
    public class InputSystem : IInputSystem, ITickable
    {
        public InputSystem()
        {
            _currentInput = new PlayerInput(System.Numerics.Vector2.Zero);
        }

        public void Tick()
        {
            if(_locallyControlledEntity == null)
            {
                return;
            }
            var keyDown = Input.GetKey(KeyCode.DownArrow) ? 1 : 0;
            var keyUp = Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
            var keyLeft = Input.GetKey(KeyCode.LeftArrow) ? 1 : 0;
            var keyRight = Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
            var input = new System.Numerics.Vector2(keyRight - keyLeft, keyUp - keyDown);
            
            if(_currentInput.Update(input))
            {
                _inputQueue.Enqueue(_currentInput);
            }
            else if (_inputQueue.Count == 0)
            {
                // input did not change and there is no data in the queue, so stop producing commands for now
                _locallyControlledEntity.StopProducingCommands();
                return;
            }

            // if there is still data in the queue, make sure Entity produces commands!
            if(!_locallyControlledEntity.ProducesCommands)
                //&& _inputQueue.Count > 0)
            {
                _locallyControlledEntity.StartProducingCommands();
            }
        }

        void IInputSystem.SetLocallyControlledEntity(EntityClient entity)
        {
            _locallyControlledEntity = entity;
        }

        void IInputSystem.WriteCommand(EntityClient entity, MoveCommand command)
        {
            if (!CanWriteCommandFor(entity))
            {
                return;
            }

            var input = _inputQueue.Dequeue();
            command.DirectionX = input.DirectionX;
            command.DirectionY = input.DirectionY;
        }

        private bool CanWriteCommandFor(EntityClient entity)
        {
            if(entity == null)
            {
                Debug.LogError("IInputSystem.WriteCommand was called without passing an entity");
                return false;
            }
            if(entity != _locallyControlledEntity)
            {
                Debug.LogError("IInputSystem.WriteCommand was called for an entity that is not locally controlled");
                return false;
            }
            if(!entity.ProducesCommands)
            {
                Debug.LogError("IInputSystem.WriteCommand was called for an entity that is not ready to produce commands");
                return false;
            }
            if(_inputQueue.Count == 0)
            {
                Debug.LogError("IInputSystem.WriteCommand was called but there is no input data to create a command from");
                return false;
            }
            return true;
        }

        PlayerInput _currentInput;
        EntityClient _locallyControlledEntity;
        readonly Queue<PlayerInput> _inputQueue = new ();
    }
}
