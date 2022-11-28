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
namespace LightOff.Level.Grid
{
    /// <summary>CellIndex repesents a position as
    /// coordinates on a grid</summary>
    public struct CellIndex
    {
        public int X;
        public int Y;

        public CellIndex(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static bool operator ==(CellIndex a, CellIndex b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(CellIndex a, CellIndex b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is CellIndex))
            {
                return false;
            }

            return Equals((CellIndex)obj);
        }

        public bool Equals(CellIndex other)
        {
            return X == other.X && Y == other.Y;
        }

        public override int GetHashCode()
        {
            return GenerateHashCode(X, Y);
        }

        private static int GenerateHashCode(int x, int y)
        {
            return x << 2 ^ y;
        }
    }

}
