using System;
using System.Collections.Generic;
using System.Reflection;
using RailgunNet.System.Encoding;

namespace RailgunNet.Logic
{
    public class RailCommandDataSerializer
    {
        private readonly RailCommand command;
        private readonly List<IRailSynchronized> members = new List<IRailSynchronized>();

        public RailCommandDataSerializer(RailCommand instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            command = instance;
            foreach (PropertyInfo prop in instance
                                          .GetType()
                                          .GetProperties(
                                              BindingFlags.Instance |
                                              BindingFlags.Public |
                                              BindingFlags.NonPublic))
            {
                if (Attribute.IsDefined(prop, typeof(CommandDataAttribute)))
                {
                    members.Add(RailSynchronizedFactory.Create(instance, prop));
                }
            }
        }

        public void SetDataFrom(RailCommandDataSerializer other)
        {
            if (command.GetType() != other.command.GetType())
            {
                throw new ArgumentException(
                    $"The instance to copy from is not for the same event type. Expected {command.GetType()}, got {other.command.GetType()}.",
                    nameof(other));
            }

            for (int i = 0; i < members.Count; ++i)
            {
                members[i].ApplyFrom(other.members[i]);
            }
        }

        public void EncodeData(RailBitBuffer buffer)
        {
            members.ForEach(e => e.WriteTo(buffer));
        }

        public void DecodeData(RailBitBuffer buffer)
        {
            members.ForEach(e => e.ReadFrom(buffer));
        }

        public void ResetData()
        {
            members.ForEach(m => m.Reset());
        }
    }
}
