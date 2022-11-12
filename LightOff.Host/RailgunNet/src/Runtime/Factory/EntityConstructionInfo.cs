using System;

namespace RailgunNet.Factory
{
    public class EntityConstructionInfo
    {
        public readonly object[] ConstructorParamsEntity;

        public EntityConstructionInfo(Type entity, Type state, object[] constructorParamsEntity)
        {
            Entity = entity;
            State = state;
            ConstructorParamsEntity = constructorParamsEntity;
        }

        public Type Entity { get; }
        public Type State { get; }
    }
}
