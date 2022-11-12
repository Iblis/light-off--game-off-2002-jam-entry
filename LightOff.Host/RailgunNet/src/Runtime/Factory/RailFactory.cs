using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace RailgunNet.Factory
{
    public interface IRailFactory<out T>
    {
        T Create();
    }

    public class RailFactory<T> : IRailFactory<T>
    {
        [NotNull] private readonly ConstructorInfo m_ConstructorToCall;
        [CanBeNull] private readonly object[] m_Parameters;

        public RailFactory() : this(typeof(T))
        {
        }

        public RailFactory([NotNull] Type typeToCreate, [CanBeNull] object[] parameters = null)
        {
            if (typeToCreate.IsAbstract)
            {
                throw new ArgumentException(
                    $"Cannot create a factory for an abstract type {typeToCreate}.");
            }

            if (typeToCreate != typeof(T) && !typeToCreate.IsSubclassOf(typeof(T)))
            {
                throw new ArgumentException(
                    $"{typeToCreate} is not derived from {typeof(T)}.",
                    nameof(typeToCreate));
            }

            ConstructorInfo constructor = null;
            if (parameters == null || parameters.Length == 0)
            {
                constructor = typeToCreate.GetConstructor(Type.EmptyTypes);
            }
            else
            {
                Type[] paramPack = parameters.Select(obj => obj.GetType()).ToArray();
                constructor = typeToCreate.GetConstructor(paramPack);
            }

            if (constructor == null)
            {
                throw new ArgumentException(
                    $"Cannot create a factory for {typeToCreate}: No constructor for parameters {parameters}.");
            }

            m_ConstructorToCall = constructor;
            m_Parameters = parameters;
        }

        public RailFactory([NotNull] ConstructorInfo constructor, [CanBeNull] object[] parameters)
        {
            m_ConstructorToCall = constructor;
            m_Parameters = parameters;
        }

        public virtual T Create()
        {
            return (T) m_ConstructorToCall.Invoke(m_Parameters);
        }
    }
}
