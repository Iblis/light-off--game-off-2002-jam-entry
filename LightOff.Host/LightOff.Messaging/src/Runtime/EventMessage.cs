// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using MessagePipe;
using RailgunNet.Logic;
using RailgunNet.System.Types;

namespace LightOff.Messaging
{
    public class EventMessage : RailEvent
    {
        [EventData] public EventMessageType EventMessageType { get; set;}

        [EventData] public EntityId SourceId {  get; set;}

        public EventMessage(IPublisher<EventMessage> publisher)
        {
            _publisher = publisher;
        }

        protected override void Execute(RailRoom room, RailController sender)
        {
            _publisher.Publish(this);
        }

        readonly IPublisher<EventMessage> _publisher;
    }
}
