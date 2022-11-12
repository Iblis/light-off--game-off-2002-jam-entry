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
using RailgunNet.System.Encoding;

namespace RailgunNet.System.Types
{
    public static class EntityIdExtensions
    {
        [Encoder]
        public static void WriteEntityId(this RailBitBuffer buffer, EntityId entityId)
        {
            entityId.Write(buffer);
        }

        [Decoder]
        public static EntityId ReadEntityId(this RailBitBuffer buffer)
        {
            return EntityId.Read(buffer);
        }

        [Encoder]
        public static void WriteEntityIdArray(this RailBitBuffer buffer, EntityId[] entityIds)
        {
            if(entityIds == null)
            {
                buffer.WriteUInt(0);
                return;
            }
            buffer.WriteUInt((uint)entityIds.Length);
            foreach(EntityId entityId in entityIds)
            {
                entityId.Write(buffer);
            }
        }

        [Decoder]
        public static EntityId[] ReadEntityIdArray(this RailBitBuffer buffer)
        {
            uint length = buffer.ReadUInt();
            if (length == 0)
            {
                return null;
            }

            var entities = new EntityId[length];
            for(int i = 0; i < length; i++)
            {
                entities[i] = EntityId.Read(buffer);
            }
            return entities;
        }

        public static EntityId PeekEntityId(this RailBitBuffer buffer)
        {
            return EntityId.Peek(buffer);
        }
    }

    public readonly struct EntityId
    {
        #region Encoding/Decoding
        #region Byte Writing
        public int PutBytes(byte[] buffer, int start)
        {
            return RailBitBuffer.PutBytes(idValue, buffer, start);
        }

        public static EntityId ReadBytes(byte[] buffer, ref int position)
        {
            return new EntityId(RailBitBuffer.ReadBytes(buffer, ref position));
        }
        #endregion

        public void Write(RailBitBuffer buffer)
        {
            buffer.WriteUInt(idValue);
        }

        public static EntityId Read(RailBitBuffer buffer)
        {
            return new EntityId(buffer.ReadUInt());
        }

        public static EntityId Peek(RailBitBuffer buffer)
        {
            return new EntityId(buffer.PeekUInt());
        }
        #endregion

        private class EntityIdComparer : IEqualityComparer<EntityId>
        {
            public bool Equals(EntityId x, EntityId y)
            {
                return x.idValue == y.idValue;
            }

            public int GetHashCode(EntityId x)
            {
                return (int) x.idValue;
            }
        }

        /// <summary>
        ///     An invalid entity ID. Should never be used explicitly.
        /// </summary>
        public static readonly EntityId INVALID = new EntityId(0);

        /// <summary>
        ///     Never used internally in Railgun, and will never be assigned to
        ///     an entity. Provided for use as a "special" entityId in applications.
        /// </summary>
        public static readonly EntityId RESERVED1 = new EntityId(1);

        public static readonly EntityId RESERVED2 = new EntityId(2);
        public static readonly EntityId RESERVED3 = new EntityId(3);
        public static readonly EntityId RESERVED4 = new EntityId(4);

        public static readonly EntityId START = new EntityId(5);

        public static IEqualityComparer<EntityId> CreateEqualityComparer()
        {
            return new EntityIdComparer();
        }

        public static bool operator ==(EntityId a, EntityId b)
        {
            return a.idValue == b.idValue;
        }

        public static bool operator !=(EntityId a, EntityId b)
        {
            return a.idValue != b.idValue;
        }

        public bool IsValid => idValue > 0;

        private readonly uint idValue;

        private EntityId(uint idValue)
        {
            this.idValue = idValue;
        }

        public EntityId GetNext()
        {
            return new EntityId(idValue + 1);
        }

        public override int GetHashCode()
        {
            return (int) idValue;
        }

        public override bool Equals(object obj)
        {
            if (obj is EntityId) return ((EntityId) obj).idValue == idValue;
            return false;
        }

        public override string ToString()
        {
            return "EntityId:" + idValue;
        }
    }
}
