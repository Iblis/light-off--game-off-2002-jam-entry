// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Logic;
using LightOff.Messaging;
using RailgunNet.Logic;
using System.Linq;
using UnityEngine;

namespace LightOff.IO.Entity
{
    public class EntityClient : RailEntityClient<EntityState, MoveCommand>, IGhostTracker
    {
        public EntityClient(IEntitySignals signals) 
        {
            _signals= signals;
        }

        protected override void OnAdded()
        {
            _signals.OnAddEntity(this);
        }

        protected override void OnRemoved()
        {
            _signals.OnRemoveEntity(this);
        }

        protected override void OnControllerChanged()
        {
            _signals.OnLocallyControlled(this);
        }

        /// <summary>
        /// Client-side prediction.
        /// This method will set Entity-State on client-side.
        /// Later, RailgunNet will merge this State with the data coming from the server
        /// </summary>
        /// <param name="command">The command that should be applied to the entities state</param>
        protected override void ApplyCommand(MoveCommand command)
        {
            _signals.OnApplyCommand(this, command);
        }

        /// <summary>
        /// Called when a new command should be send to the server (determined by ProducesCommand-property)
        /// </summary>
        /// <param name="toPopulate">Command that should be populated with the data that should be send to the server</param>
        protected override void WriteCommand(MoveCommand toPopulate)
        {
            _signals.OnWriteCommand(this, toPopulate);
        }

        internal void StartProducingCommands()
        {
            ProducesCommands = true;
        }

        internal void StopProducingCommands()
        {
            ProducesCommands= false;
            if (OutgoingCommands.Count() > 0)
            {
                ClearCommands();
            }
        }

        bool IGhostTracker.IsActive => true;

        float IEntity.PosX { get => State.PosX; set => State.PosX = value; }
        float IEntity.PosY { get => State.PosY; set => State.PosY = value; }
        float IEntity.Angle { get => State.Angle; set => State.Angle = value; }

        public Vector3 Position => new Vector3(State.PosX, State.PosY, 0);

        public float AngleInDegrees => State.Angle * Mathf.Rad2Deg;

        readonly IEntitySignals _signals;
    }
}
