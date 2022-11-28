// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Cysharp.Threading;
using LightOff.Level;
using LightOff.Messaging;
using MessagePipe;
using RailgunNet.Connection.Server;
using RailgunNet.Factory;

namespace LightOff.Host.Session
{
    public class HostedGameSessions : IHostedGameSessions
    {
        public HostedGameSessions(IGameLoop gameLoop, 
            ISubscriber<EventMessage> eventMessageSubscriber, 
            RailRegistry registry,
            ILogger<HostedGameSessions> logger)
        {
            _gameLoop = gameLoop;
            _registry = registry;
            _eventMessageSubscriber = eventMessageSubscriber;
            _logger = logger;
        }

        public IHostedGameSession GetFor(string sessionName)
        {
            if(_sessions.TryGetValue(sessionName, out var session))
            {
                return session;
            }
            return CreateGameSession(sessionName);
        }

        IHostedGameSession CreateGameSession(string sessionName)
        {
            // TODO: put this into an own factory
            var session = new HostedGameSession(sessionName, new RailServer(_registry), new World(), _eventMessageSubscriber, _logger);
            _sessions.Add(sessionName, session);
            if(!_gameLoop.HasRegistrations)
            {
                _ = _gameLoop.RegisterActionAsync(Update);
            }            
            return session;
        }

        bool Update(in LogicLooperActionContext ctx)
        {
            foreach(var session in _sessionsToBeRemoved)
            {
                session.Dispose();
                _logger.LogInformation($"Removing inactive session: {session.Name}");
                _sessions.Remove(session.Name);
            }
            _sessionsToBeRemoved.Clear();
            foreach(var session in _sessions.Values)
            {
                var sessionIsActive = session.Update((float)ctx.ElapsedTimeFromPreviousFrame.TotalSeconds);
                if(!sessionIsActive)
                {
                    _sessionsToBeRemoved.Add(session);
                }
            }
            return _sessions.Count > 0;
        }

        readonly IGameLoop _gameLoop;
        readonly RailRegistry _registry;
        readonly ISubscriber<EventMessage> _eventMessageSubscriber;
        readonly Dictionary<string, HostedGameSession> _sessions = new ();
        readonly List<HostedGameSession> _sessionsToBeRemoved = new();
        readonly ILogger<HostedGameSessions> _logger;
    }
}
