using System;

namespace RailgunNet.Logic
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MutableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ImmutableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ControllerAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CompressorAttribute : Attribute
    {
        public CompressorAttribute(Type compressor)
        {
            Compressor = compressor;
        }

        public Type Compressor { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class EventDataAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CommandDataAttribute : Attribute
    {
    }
}
