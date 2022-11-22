// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Level;
using RailgunNet.Connection.Server;
using RailgunNet.Factory;

namespace LightOff.Host.Session
{
    public class HostedGameSessions : IHostedGameSessions
    {
        public HostedGameSessions(IGameLoop gameLoop, RailRegistry registry)
        {
            _gameLoop = gameLoop;
            _registry = registry;
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
            var session = new HostedGameSession(sessionName, new RailServer(_registry), new World());
            _sessions.Add(sessionName, session);
            _gameLoop.RegisterActionAsync(session.Update);
            return session;
        }

        readonly IGameLoop _gameLoop;
        readonly RailRegistry _registry;
        readonly Dictionary<string, IHostedGameSession> _sessions = new ();
    }
}
