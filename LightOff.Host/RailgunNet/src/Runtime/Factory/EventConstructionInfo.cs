using System;

namespace RailgunNet.Factory
{
    public class EventConstructionInfo
    {
        public readonly object[] ConstructorParams;

        public EventConstructionInfo(Type type, object[] constructorParams)
        {
            Type = type;
            ConstructorParams = constructorParams;
        }

        public Type Type { get; }
    }
}
