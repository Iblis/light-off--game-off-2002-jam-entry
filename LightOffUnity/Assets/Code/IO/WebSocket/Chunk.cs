using System;
using System.Buffers;

namespace LightOff.IO.WebSocket
{
    internal class Chunk<T> : ReadOnlySequenceSegment<T>
    {
        public Chunk(ReadOnlyMemory<T> memory)
        {
            Memory = memory;
        }
        public Chunk<T> Add(ReadOnlyMemory<T> mem)
        {
            var segment = new Chunk<T>(mem)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = segment;
            return segment;
        }
    }
}
