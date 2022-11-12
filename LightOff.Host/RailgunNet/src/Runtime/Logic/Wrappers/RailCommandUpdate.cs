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

using System.Collections.Generic;
using JetBrains.Annotations;
using RailgunNet.Factory;
using RailgunNet.System.Buffer;
using RailgunNet.System.Encoding;
using RailgunNet.System.Types;
using RailgunNet.Util;
using RailgunNet.Util.Pooling;

namespace RailgunNet.Logic.Wrappers
{
    public class RailCommandUpdate : IRailPoolable<RailCommandUpdate>
    {
        private const int BUFFER_CAPACITY = RailConfig.COMMAND_SEND_COUNT;

        private static readonly int BUFFER_COUNT_BITS = RailUtil.Log2(BUFFER_CAPACITY) + 1;

        private readonly RailRollingBuffer<RailCommand> commands;

        public RailCommandUpdate()
        {
            EntityId = EntityId.INVALID;
            commands = new RailRollingBuffer<RailCommand>(BUFFER_CAPACITY);
        }

        [OnlyIn(Component.Client)] [CanBeNull] public RailEntityClient Entity { get; private set; }

        public EntityId EntityId { get; private set; }

        public IEnumerable<RailCommand> Commands => commands.GetValues();

        public static RailCommandUpdate Create(
            IRailCommandConstruction commandCreator,
            RailEntityClient entity,
            IEnumerable<RailCommand> commands)
        {
            RailCommandUpdate update = commandCreator.CreateCommandUpdate();
            update.Initialize(entity.Id, commands);
            update.Entity = entity;
            return update;
        }

        private void Initialize(EntityId entityId, IEnumerable<RailCommand> outgoingCommands)
        {
            EntityId = entityId;
            foreach (RailCommand command in outgoingCommands)
            {
                commands.Store(command);
            }
        }

        private void Reset()
        {
            EntityId = EntityId.INVALID;
            commands.Clear();
        }

        [OnlyIn(Component.Client)]
        public void Encode(RailBitBuffer buffer)
        {
            // Write: [EntityId]
            buffer.WriteEntityId(EntityId);

            // Write: [Count]
            buffer.Write(BUFFER_COUNT_BITS, (uint) commands.Count);

            // Write: [Commands]
            foreach (RailCommand command in commands.GetValues())
            {
                command.Encode(buffer);
            }
        }

        [OnlyIn(Component.Server)]
        public static RailCommandUpdate Decode(
            IRailCommandConstruction commandCreator,
            RailBitBuffer buffer)
        {
            RailCommandUpdate update = commandCreator.CreateCommandUpdate();

            // Read: [EntityId]
            update.EntityId = buffer.ReadEntityId();

            // Read: [Count]
            int count = (int) buffer.Read(BUFFER_COUNT_BITS);

            // Read: [Commands]
            for (int i = 0; i < count; i++)
            {
                update.commands.Store(RailCommand.Decode(commandCreator, buffer));
            }

            return update;
        }

        #region Pooling
        IRailMemoryPool<RailCommandUpdate> IRailPoolable<RailCommandUpdate>.Pool { get; set; }

        void IRailPoolable<RailCommandUpdate>.Reset()
        {
            Reset();
        }

        void IRailPoolable<RailCommandUpdate>.Allocated()
        {
        }
        #endregion
    }
}
