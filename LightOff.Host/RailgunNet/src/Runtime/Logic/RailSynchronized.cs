using System;
using System.Collections.Generic;
using System.Reflection;
using RailgunNet.System.Encoding;
using RailgunNet.Util;

namespace RailgunNet.Logic
{
    public interface IRailSynchronized
    {
        void WriteTo(RailBitBuffer buffer);
        void ReadFrom(RailBitBuffer buffer);
        void ApplyFrom(IRailSynchronized other);
        bool Equals(IRailSynchronized other);
        void Reset();
    }

    public class RailGenericField
    {
        private object compressor;
        public Func<RailBitBuffer, object> decode { get; }
        public Action<RailBitBuffer, object> encode { get; }
        public Func<object, object> getter { get; }
        public Action<object, object> setter { get; }

        public RailGenericField(MemberInfo member)
        {
            getter = InvokableFactory.CreateUntypedGetter<object>(member);
            setter = InvokableFactory.CreateUntypedSetter<object>(member);
            Type underlyingType = member.GetUnderlyingType();

            CompressorAttribute att = member.GetCustomAttribute<CompressorAttribute>();
            if (att == null)
            {
                encode = InvokableFactory.CreateCall<RailBitBuffer>(
                    GetEncodeMethod(typeof(RailBitBuffer), underlyingType));
                decode = InvokableFactory.CreateCallWithReturn<RailBitBuffer>(
                    GetDecodeMethod(typeof(RailBitBuffer), underlyingType));
            }
            else
            {
                compressor = att.Compressor.GetConstructor(Type.EmptyTypes).Invoke(null);
                if (compressor == null)
                {
                    throw new ArgumentException(
                        "The declared compressor needs to implement a parameterless default constructor.",
                        nameof(member));
                }

                encode = InvokableFactory.CreateCall<RailBitBuffer>(
                    GetEncodeMethod(compressor.GetType(), underlyingType),
                    compressor);
                decode = InvokableFactory.CreateCallWithReturn<RailBitBuffer>(
                    GetDecodeMethod(compressor.GetType(), underlyingType),
                    compressor);
            }
        }
        private static MethodInfo GetEncodeMethod(Type encoder, Type toBeEncoded)
        {
            foreach (MethodInfo method in encoder.GetMethods(
                BindingFlags.Public | BindingFlags.Instance))
            {
                EncoderAttribute att = method.GetCustomAttribute<EncoderAttribute>();
                if (att != null)
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length > 0 && parameters[0].ParameterType == toBeEncoded ||
                        parameters.Length > 1 && parameters[1].ParameterType == toBeEncoded)
                    {
                        return method;
                    }
                }
            }

            if (RailSynchronizedFactory.Encoders.TryGetValue(
                toBeEncoded,
                out MethodInfo encoderMethod))
            {
                return encoderMethod;
            }

            throw new ArgumentException($"Cannot find an encoder method for type {toBeEncoded}.");
        }

        private static MethodInfo GetDecodeMethod(Type decoder, Type toBeDecoded)
        {
            foreach (MethodInfo method in decoder.GetMethods())
            {
                DecoderAttribute att = method.GetCustomAttribute<DecoderAttribute>();

                if (att != null && method.ReturnType == toBeDecoded)
                {
                    return method;
                }
            }

            if (RailSynchronizedFactory.Decoders.TryGetValue(
                toBeDecoded,
                out MethodInfo decoderMethod))
            {
                return decoderMethod;
            }

            throw new ArgumentException($"Cannot find a decoder method for type {toBeDecoded}.");
        }
    }

    public class RailSynchronized<TContainer> : IRailSynchronized
    {
        private static Dictionary<MemberInfo, RailGenericField> fieldCache = new Dictionary<MemberInfo, RailGenericField>();
        private readonly object initialValue;
        private readonly TContainer instance;
        private readonly RailGenericField field;

        public RailSynchronized(TContainer instanceToWrap, MemberInfo member)
        {
            if (!fieldCache.ContainsKey(member))
            {
                fieldCache[member] = new RailGenericField(member);
            }
            field = fieldCache[member];
            
            instance = instanceToWrap;
            initialValue = field.getter(instance);
        }

        public void ReadFrom(RailBitBuffer buffer)
        {
            field.setter(instance, field.decode(buffer));
        }

        public void WriteTo(RailBitBuffer buffer)
        {
            field.encode(buffer, field.getter(instance));
        }

        public void ApplyFrom(IRailSynchronized from)
        {
            RailSynchronized<TContainer> other = (RailSynchronized<TContainer>) from;
            field.setter(instance, field.getter(other.instance));
        }

        public bool Equals(IRailSynchronized from)
        {
            RailSynchronized<TContainer> other = (RailSynchronized<TContainer>) from;
            return field.getter(instance).Equals(field.getter(other.instance));
        }

        public void Reset()
        {
            field.setter(instance, initialValue);
        }

        
    }
}
