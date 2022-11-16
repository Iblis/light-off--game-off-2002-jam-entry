/**
MIT License

Copyright (c) 2021 Benjamin Trosch

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. 
 */

using System;
using System.Collections.Generic;
using System.Numerics;

namespace LightOff.Level.Grid
{
    /// <summary>A fast fixed-size Spatial Hash implementation</summary>
    public class SpatialHash<T> where T : IHashable
    {
        /// <summary>Multidimensional array as [x, y] to represent grid position</summary>
        public readonly List<T>[,] Grid;

        /// <summary>Cached previous query result
        /// to prevent allocating new list on every hash query.
        /// List o(n) will be most efficient dynamic collection
        /// for low expected item count (< 10).</summary>
        private List<T> _queryBucket;
        /// <summary>Unique identifier to deduplicate colliders
        /// that exist in multiple buckets in a single query</summary>
        private int _queryId;

        public int Width;
        public int Height;

        /// <summary>Size represented by pixels</summary>
        public readonly int CellSize;

        public SpatialHash(int width, int height, int cellSize)
        {
            Grid = new List<T>[width, height];
            _queryBucket = new List<T>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Grid[x, y] = new List<T>();
                }
            }

            Width = width;
            Height = height;

            CellSize = cellSize;
        }

        public IEnumerable<T> GetAllColliders()
        {
            var colliders = new List<T>();
            foreach (var entry in Grid)
            {
                colliders.AddRange(entry);
            }
            return colliders;
        }     

        /// <summary>Returns a 2D position as an index within the grid space</summary>
        /// <param name="position">World position of hashed object</param>
        public CellIndex GetCellIndex(Vector2 position)
        {
            float inverseCellSize = 1f / CellSize;

            int x = (int)MathF.Floor(position.X * inverseCellSize);
            int y = (int)MathF.Floor(position.Y * inverseCellSize);

            /***
             * Calculated index clamped between 0 and total # of cells
             * to prevent index going out of bounds
             */
            int clampedX = Math.Min(Width - 1, Math.Max(0, x));
            int clampedY = Math.Min(Height - 1, Math.Max(0, y));

            return new CellIndex(clampedX, clampedY);
        }

        /// <summary>Add a specified collider to every bucket on the grid between it's top left and bottom right bounds</summary>
        public void Insert(T collider, Vector2 min, Vector2 max)
        {
            CellIndex startCoordinates = GetCellIndex(min);
            CellIndex endCoordinates = GetCellIndex(max);

            collider.RegisteredHashBounds = (startCoordinates, endCoordinates);

            /***
             * Use top left and bottom right corners of collider bounds
             * to find every cell in between that the collider belongs in
             */
            for (int x0 = startCoordinates.X, x1 = endCoordinates.X; x0 <= x1; x0++)
            {
                for (int y0 = startCoordinates.Y, y1 = endCoordinates.Y; y0 <= y1; y0++)
                {
                    Grid[x0, y0].Add(collider);
                }
            }
        }

        /// <summary>Remove a collider from every bucket it belongs to and nullify the key</summary>
        public void Remove(T collider)
        {
            if (collider.RegisteredHashBounds != null)
            {
                /***
                * Need to explicitly coerce bounds as non-null tuple
                * because IHashable type is nullable
                */
                (CellIndex, CellIndex) colliderHashBounds = ((CellIndex, CellIndex))collider.RegisteredHashBounds;

                CellIndex startCoordinates = colliderHashBounds.Item1;
                CellIndex endCoordinates = colliderHashBounds.Item2;

                collider.RegisteredHashBounds = null;

                for (int x0 = startCoordinates.X, x1 = endCoordinates.X; x0 <= x1; x0++)
                {
                    for (int y0 = startCoordinates.Y, y1 = endCoordinates.Y; y0 <= y1; y0++)
                    {
                        Grid[x0, y0].Remove(collider);
                    }
                }
            }
        }

        /// <summary>Update a collider (and it's buckets) by removing and then re-inserting it</summary>
        public void UpdateCollider(T collider, Vector2 topLeftBounds, Vector2 bottomRightBounds)
        {
            /***
             * Do not need to update hashed collider if bounds have not moved enough
             * to change cells
             */
            if (ColliderHasMovedCells(collider, topLeftBounds, bottomRightBounds))
            {
                Remove(collider);
                Insert(collider, topLeftBounds, bottomRightBounds);
            }
        }

        /// <summary>Returns whether or not a collider has moved enough to change cells</summary>
        public bool ColliderHasMovedCells(T collider, Vector2 topLeftBounds, Vector2 bottomRightBounds)
        {
            CellIndex startCoordinates = GetCellIndex(topLeftBounds);
            CellIndex endCoordinates = GetCellIndex(bottomRightBounds);

            return collider.RegisteredHashBounds != (startCoordinates, endCoordinates);
        }

        /// <summary>Returns all colliders an entity shares a bucket with (no repeats and self not returned)</summary>
        /// <param name="collider">Target collider</param>
        /// <param name="radius">Amount of additional cells to check in every direction</param>
        public List<T> FindNearbyColliders(T collider, int radius = 0)
        {
            /***
             * Clear previous query to save memory from a new List allocation
             * NOTE: memory does not get released by Clear and may still build up on GC
             */
            _queryBucket.Clear();
            
            if (collider.RegisteredHashBounds != null)
            {
                FillQueryBucketFor(collider.RegisteredHashBounds.Value, radius);
            }
            return _queryBucket;
        }

        public List<T> FindNearbyColliders(CellIndex cellIndex, int radius)
        {
            /***
             * Clear previous query to save memory from a new List allocation
             * NOTE: memory does not get released by Clear and may still build up on GC
             */
            _queryBucket.Clear();
            FillQueryBucketFor((cellIndex, cellIndex), radius);
            return _queryBucket;
        }

        private void FillQueryBucketFor((CellIndex, CellIndex) colliderHashBounds, int radius)
        {
            int startX = Math.Max(0, colliderHashBounds.Item1.X - radius);
            int startY = Math.Max(0, colliderHashBounds.Item1.Y - radius);

            int endX = Math.Min(Width - 1, colliderHashBounds.Item2.X + radius);
            int endY = Math.Min(Height - 1, colliderHashBounds.Item2.Y + radius);

            /***
            * Iterate to ensure unique query id
            */
            int queryId = _queryId++;

            for (int x0 = startX, x1 = endX; x0 <= x1; x0++)
            {
                for (int y0 = startY, y1 = endY; y0 <= y1; y0++)
                {
                    foreach (T coll in Grid[x0, y0])
                    {
                        if (coll.QueryId != queryId &&
                            coll.RegisteredHashBounds != colliderHashBounds)
                        {
                            /***
                            * Set collider query id to current query id to prevent
                            * duplicate object from same query in nearby bucket
                            */
                            coll.QueryId = queryId;
                            _queryBucket.Add(coll);
                        }
                    }
                }
            }
        }
    }
}
