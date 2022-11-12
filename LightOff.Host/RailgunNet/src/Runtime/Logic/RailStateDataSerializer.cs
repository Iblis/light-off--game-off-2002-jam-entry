using System;
using System.Collections.Generic;
using System.Reflection;
using RailgunNet.System.Encoding;

namespace RailgunNet.Logic
{
    public class RailStateDataSerializer
    {
        private readonly List<IRailSynchronized> controller = new List<IRailSynchronized>();
        private readonly List<IRailSynchronized> immutable = new List<IRailSynchronized>();
        private readonly List<IRailSynchronized> mutable = new List<IRailSynchronized>();
        private readonly RailState state;

        public RailStateDataSerializer(RailState instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            state = instance;
            foreach (PropertyInfo prop in state
                                          .GetType()
                                          .GetProperties(
                                              BindingFlags.Instance |
                                              BindingFlags.Public |
                                              BindingFlags.NonPublic))
            {
                if (Attribute.IsDefined(prop, typeof(MutableAttribute)))
                {
                    mutable.Add(RailSynchronizedFactory.Create(state, prop));
                }
                else if (Attribute.IsDefined(prop, typeof(ImmutableAttribute)))
                {
                    immutable.Add(RailSynchronizedFactory.Create(state, prop));
                }
                else if (Attribute.IsDefined(prop, typeof(ControllerAttribute)))
                {
                    controller.Add(RailSynchronizedFactory.Create(state, prop));
                }
            }
        }

        public bool HasControllerData => controller.Count == 0;

        private static uint ToFlag(int index)
        {
            return (uint) 0x1 << index;
        }

        #region Interface
        public int FlagBits => mutable.Count;

        public void ApplyControllerFrom(RailStateDataSerializer source)
        {
            for (int i = 0; i < controller.Count; ++i)
            {
                controller[i].ApplyFrom(source.controller[i]);
            }
        }

        public void ApplyImmutableFrom(RailStateDataSerializer source)
        {
            for (int i = 0; i < immutable.Count; ++i)
            {
                immutable[i].ApplyFrom(source.immutable[i]);
            }
        }

        public void ApplyMutableFrom(RailStateDataSerializer source, uint flags)
        {
            for (int i = 0; i < mutable.Count; ++i)
            {
                if ((flags & ToFlag(i)) == ToFlag(i))
                {
                    mutable[i].ApplyFrom(source.mutable[i]);
                }
            }
        }

        public uint CompareMutableData(RailStateDataSerializer other)
        {
            uint uiFlags = 0x0;
            for (int i = 0; i < mutable.Count; ++i)
            {
                if (!mutable[i].Equals(other.mutable[i]))
                {
                    uiFlags |= ToFlag(i);
                }
            }

            return uiFlags;
        }

        public bool IsControllerDataEqual(RailStateDataSerializer other)
        {
            for (int i = 0; i < controller.Count; ++i)
            {
                if (!controller[i].Equals(other.controller[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public void ResetAllData()
        {
            immutable.ForEach(c => c.Reset());
            mutable.ForEach(c => c.Reset());
            controller.ForEach(c => c.Reset());
        }

        public void ResetControllerData()
        {
            controller.ForEach(c => c.Reset());
        }
        #endregion

        #region Encode & Decode
        public void DecodeControllerData(RailBitBuffer buffer)
        {
            controller.ForEach(c => c.ReadFrom(buffer));
        }

        public void DecodeImmutableData(RailBitBuffer buffer)
        {
            immutable.ForEach(i => i.ReadFrom(buffer));
        }

        public void DecodeMutableData(RailBitBuffer buffer, uint flags)
        {
            for (int i = 0; i < mutable.Count; ++i)
            {
                if ((flags & ToFlag(i)) == ToFlag(i))
                {
                    mutable[i].ReadFrom(buffer);
                }
            }
        }

        public void EncodeControllerData(RailBitBuffer buffer)
        {
            controller.ForEach(c => c.WriteTo(buffer));
        }

        public void EncodeImmutableData(RailBitBuffer buffer)
        {
            immutable.ForEach(i => i.WriteTo(buffer));
        }

        public void EncodeMutableData(RailBitBuffer buffer, uint flags)
        {
            for (int i = 0; i < mutable.Count; ++i)
            {
                if ((flags & ToFlag(i)) == ToFlag(i))
                {
                    mutable[i].WriteTo(buffer);
                }
            }
        }
        #endregion
    }
}
