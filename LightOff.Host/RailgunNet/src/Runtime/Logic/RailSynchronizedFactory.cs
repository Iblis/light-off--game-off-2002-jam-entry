using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RailgunNet.System.Encoding;

namespace RailgunNet.Logic
{
    public static class RailSynchronizedFactory
    {
        private static readonly List<int> m_ScannedAssemblies = new List<int>();

        private static readonly Dictionary<Type, MethodInfo> m_Encoders =
            new Dictionary<Type, MethodInfo>();

        private static readonly Dictionary<Type, MethodInfo> m_Decoders =
            new Dictionary<Type, MethodInfo>();

        public static IReadOnlyDictionary<Type, MethodInfo> Encoders => m_Encoders;

        public static IReadOnlyDictionary<Type, MethodInfo> Decoders => m_Decoders;

        public static IRailSynchronized Create<T>(T instance, MemberInfo info)
        {
            return new RailSynchronized<T>(instance, info);
        }

        public static void Detect(Assembly assembly)
        {
            int hash = assembly.GetHashCode();
            if (!m_ScannedAssemblies.Contains(hash))
            {
                DetectEncoders(assembly);
                DetectDecoders(assembly);
                m_ScannedAssemblies.Add(hash);
            }
        }

        private static void DetectEncoders(Assembly assembly)
        {
            foreach (MethodInfo method in FindStaticExtensions<EncoderAttribute>(assembly))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 1)
                {
                    Type encodedType = parameters[1].ParameterType;
                    if (m_Encoders.ContainsKey(encodedType))
                    {
                        throw new Exception(
                            $"Multiple RailBitBuffer encoder extensions for {encodedType} detected.");
                    }

                    m_Encoders.Add(encodedType, method);
                }
            }
        }

        private static void DetectDecoders(Assembly assembly)
        {
            foreach (MethodInfo method in FindStaticExtensions<DecoderAttribute>(assembly))
            {
                Type decodedType = method.ReturnType;
                if (m_Decoders.ContainsKey(decodedType))
                {
                    throw new Exception(
                        $"Multiple RailBitBuffer decoder extensions for {decodedType} detected.");
                }

                m_Decoders.Add(decodedType, method);
            }
        }

        private static List<MethodInfo> FindStaticExtensions<TAttribute>(Assembly assembly)
            where TAttribute : Attribute
        {
            IEnumerable<MethodInfo> query =
                from t in assembly.GetTypes()
                where !t.IsGenericType && !t.IsNested
                from m in t.GetMethods(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                where m.IsDefined(typeof(ExtensionAttribute), false)
                where m.IsDefined(typeof(TAttribute))
                where m.GetParameters()[0].ParameterType == typeof(RailBitBuffer)
                select m;
            return query.ToList();
        }
    }
}
