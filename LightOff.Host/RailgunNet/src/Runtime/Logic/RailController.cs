/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016-2018 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
 */

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RailgunNet.Connection.Traffic;
using RailgunNet.Factory;
using RailgunNet.Logic.Scope;
using RailgunNet.System.Types;
using RailgunNet.Util;
using RailgunNet.Util.Debug;

namespace RailgunNet.Logic
{
    public class RailController
    {
        /// <summary>
        ///     The entities controlled by this controller.
        /// </summary>
        private readonly HashSet<RailEntityBase> controlledEntities;

        public RailController(
            IRailStateConstruction stateCreator,
            ExternalEntityVisibility eVisibility,
            [CanBeNull] IRailNetPeer netPeer)
        {
            controlledEntities = new HashSet<RailEntityBase>();
            NetPeer = netPeer;
            Scope = eVisibility == ExternalEntityVisibility.Scoped ?
                new RailScope(this, stateCreator) :
                null;
        }

        /// <summary>
        ///     The network I/O peer for sending/receiving data.
        /// </summary>
        [CanBeNull]
        protected IRailNetPeer NetPeer { get; }

        public object UserData { get; set; }

        public virtual Tick EstimatedRemoteTick =>
            throw new InvalidOperationException("Local controller has no remote tick");

        public IEnumerable<RailEntityBase> ControlledEntities => controlledEntities;

        /// <summary>
        ///     Used for determining which entity updates to send.
        /// </summary>
        [CanBeNull]
        [OnlyIn(Component.Server)]
        public RailScope Scope { get; }

        #region Controller
        /// <summary>
        ///     Detaches the controller from all controlled entities.
        /// </summary>
        public void Shutdown()
        {
            foreach (RailEntityBase entity in controlledEntities)
            {
                entity.AssignController(null);
            }

            controlledEntities.Clear();
        }

        /// <summary>
        ///     Adds an entity to be controlled by this peer.
        /// </summary>
        public void GrantControlInternal(RailEntityBase entity)
        {
            if (entity.Controller == this) return;
            RailDebug.Assert(entity.Controller == null);

            controlledEntities.Add(entity);
            entity.AssignController(this);
        }

        /// <summary>
        ///     Remove an entity from being controlled by this peer.
        /// </summary>
        public void RevokeControlInternal(RailEntityBase entity)
        {
            RailDebug.Assert(entity.Controller == this);

            controlledEntities.Remove(entity);
            entity.AssignController(null);
        }
        #endregion
    }
}
