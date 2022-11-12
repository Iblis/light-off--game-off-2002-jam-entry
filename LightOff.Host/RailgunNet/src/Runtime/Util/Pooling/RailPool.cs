﻿/*
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
using RailgunNet.Factory;
using RailgunNet.Util.Debug;

namespace RailgunNet.Util.Pooling
{
    public static class RailPool
    {
        public static void Free<T>(T instance)
            where T : IRailPoolable<T>
        {
            instance.Pool.Deallocate(instance);
        }

        public static void SafeReplace<T>(ref T destination, T instance)
            where T : IRailPoolable<T>
        {
            if (destination != null) Free(destination);
            destination = instance;
        }

        public static void DrainQueue<T>(Queue<T> queue)
            where T : IRailPoolable<T>
        {
            while (queue.Count > 0)
            {
                Free(queue.Dequeue());
            }
        }
    }

    public interface IRailMemoryPool<T>
    {
        T Allocate();
        void Deallocate(T instance);
    }

    public class RailMemoryPool<T> : IRailMemoryPool<T>
        where T : IRailPoolable<T>
    {
        private readonly IRailFactory<T> factory;
        private readonly Stack<T> freeList;

        public RailMemoryPool(IRailFactory<T> factory)
        {
            this.factory = factory;
            freeList = new Stack<T>();
        }

        public T Allocate()
        {
            if (freeList.Count > 0)
            {
                T obj = freeList.Pop();
                obj.Pool = this;
                obj.Reset();
                return obj;
            }
            else
            {
                T obj = factory.Create();
                obj.Pool = this;
                obj.Allocated();
                return obj;
            }
        }

        public void Deallocate(T instance)
        {
            RailDebug.Assert(instance.Pool == this);

            instance.Reset();
            instance.Pool = null; // Prevent multiple frees
            freeList.Push(instance);
        }
    }
}
