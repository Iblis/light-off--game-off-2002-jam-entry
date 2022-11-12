using System;
using System.Collections.Generic;
using System.Reflection;
using RailgunNet.System.Encoding;
using RailgunNet.System.Types;

namespace RailgunNet.Logic
{
    public class RailEventDataSerializer
    {
        private readonly RailEvent eventInstance;
        private readonly List<IRailSynchronized> members = new List<IRailSynchronized>();

        public RailEventDataSerializer(RailEvent instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            eventInstance = instance;
            foreach (PropertyInfo prop in instance
                                          .GetType()
                                          .GetProperties(
                                              BindingFlags.Instance |
                                              BindingFlags.Public |
                                              BindingFlags.NonPublic))
            {
                if (Attribute.IsDefined(prop, typeof(EventDataAttribute)))
                {
                    members.Add(RailSynchronizedFactory.Create(instance, prop));
                }
            }
        }

        public void SetDataFrom(RailEventDataSerializer other)
        {
            if (eventInstance.GetType() != other.eventInstance.GetType())
            {
                throw new ArgumentException(
                    $"The instance to copy from is not for the same event type. Expected {eventInstance.GetType()}, got {other.eventInstance.GetType()}.",
                    nameof(other));
            }

            for (int i = 0; i < members.Count; ++i)
            {
                members[i].ApplyFrom(other.members[i]);
            }
        }

        public void WriteData(RailBitBuffer buffer, Tick _)
        {
            // TODO: forward packetTick?
            members.ForEach(m => m.WriteTo(buffer));
        }

        public void ReadData(RailBitBuffer buffer, Tick _)
        {
            // TODO: forward packetTick?
            members.ForEach(m => m.ReadFrom(buffer));
        }

        public void ResetData()
        {
            members.ForEach(m => m.Reset());
        }
    }
}
