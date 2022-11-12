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
using JetBrains.Annotations;
using RailgunNet.Connection.Traffic;
using RailgunNet.Factory;
using RailgunNet.Logic;
using RailgunNet.System.Types;

namespace RailgunNet.Connection
{
    /// <summary>
    ///     Server is the core executing class for communication. It is responsible
    ///     for managing connection contexts and payload I/O.
    /// </summary>
    public abstract class RailConnection
    {
        private bool hasStarted;

        protected RailConnection(RailRegistry registry)
        {
            Resource = new RailResource(registry);
            Interpreter = new RailInterpreter();
            Room = null;
            hasStarted = false;
        }

        protected RailResource Resource { get; }

        private RailRoom Room { get; set; }

        protected RailInterpreter Interpreter { get; }

        [PublicAPI] public event Action Started;

        public abstract void Update();

        protected void SetRoom(RailRoom room, Tick startTick)
        {
            Room = room;
            Room.Initialize(startTick);
        }

        protected void OnEventReceived(RailEvent evnt, RailPeer sender)
        {
            evnt.Invoke(Room, sender);
        }

        protected void DoStart()
        {
            if (!hasStarted)
            {
                hasStarted = true;
                Started?.Invoke();
            }
        }
    }
}
