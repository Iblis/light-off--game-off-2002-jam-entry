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
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using RailgunNet.Logic;

namespace RailgunNet.Factory
{
    public class RailRegistry
    {
        private readonly List<EntityConstructionInfo> entityTypes;
        private readonly List<EventConstructionInfo> eventTypes;

        public RailRegistry(Component eComponent)
        {
            Component = eComponent;
            CommandType = null;
            eventTypes = new List<EventConstructionInfo>();
            entityTypes = new List<EntityConstructionInfo>();
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in loadedAssemblies)
            {
                RailSynchronizedFactory.Detect(assembly);
            }
        }

        public Component Component { get; }
        public Type CommandType { get; private set; }

        public IEnumerable<EventConstructionInfo> EventTypes => eventTypes;

        public IEnumerable<EntityConstructionInfo> EntityTypes => entityTypes;

        [PublicAPI]
        public void SetCommandType<TCommand>()
            where TCommand : RailCommand
        {
            CommandType = typeof(TCommand);
        }

        [PublicAPI]
        public void AddEventType<TEvent>(object[] constructorParams = null)
            where TEvent : RailEvent
        {
            Type eventType = typeof(TEvent);
            if (!CanBeConstructedWith<TEvent>(constructorParams))
            {
                throw new ArgumentException(
                    $"The provided constructor arguments {constructorParams} do not match any constructors in {eventType}.");
            }

            eventTypes.Add(new EventConstructionInfo(eventType, constructorParams));
        }

        /// <summary>
        ///     Adds an entity type with its corresponding state to the registry.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TState"></typeparam>
        /// <param name="paramsEntity">Array of parameters for the entity constructor to invoke or null.</param>
        /// <param name="paramsState">Array of parameters for the state constructor to invoke or null.</param>
        [PublicAPI]
        public void AddEntityType<TEntity, TState>(object[] paramsEntity = null)
            where TEntity : RailEntityBase
            where TState : RailState
        {
            // Type check for TEntity
            Type expectedBaseType;
            switch (Component)
            {
                case Component.Server:
                    expectedBaseType = typeof(RailEntityServer);
                    break;
                case Component.Client:
                    expectedBaseType = typeof(RailEntityClient);
                    break;
                default:
                    throw new ArgumentException(nameof(Component));
            }

            Type entityType = typeof(TEntity);
            if (!entityType.IsSubclassOf(expectedBaseType))
            {
                throw new ArgumentException(
                    $"All entities in a {Component} have to be derived from {expectedBaseType}. The provided entity is of type {entityType}.");
            }

            if (!CanBeConstructedWith<TEntity>(paramsEntity))
            {
                throw new ArgumentException(
                    $"The provided constructor arguments {paramsEntity} do not match any constructors in {entityType}.");
            }

            entityTypes.Add(new EntityConstructionInfo(entityType, typeof(TState), paramsEntity));
        }

        private static bool CanBeConstructedWith<T>(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                return typeof(T).GetConstructor(Type.EmptyTypes) != null;
            }

            Type[] paramPack = parameters.Select(obj => obj.GetType()).ToArray();
            return typeof(T).GetConstructor(paramPack) != null;
        }
    }
}
