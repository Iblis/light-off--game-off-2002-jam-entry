// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Host.Client;
using LightOff.Logic;
using LightOff.Messaging;
using MessagePipe;
using RailgunNet.Connection.Server;
using System.Numerics;

namespace LightOff.Host.Session
{
    internal class HostedGameSession : IHostedGameSession
    {
        public string Name { get; init; }
        public bool CanBeJoined => _world.Players.Count() < PLAYER_SLOT_GHOST && _sessionState == SessionState.MatchInit;

        public HostedGameSession(string sessionName,
                                    RailServer server,
                                    IWorld world,
                                    ISubscriber<EventMessage> eventMessageSubscriber,
                                    ILogger logger)
        {
            Name = sessionName.ToLower();
            _logger = logger;
            _world = world;
            _commandHandler = new Messaging.Server.CommandHandler(_world);
            _server = server;
            _server.ClientAdded += OnClientAdded;
            _room = _server.StartRoom();
            
            _eventSubscription = eventMessageSubscriber.Subscribe(OnEventMessage);
        }

        private void OnEventMessage(EventMessage message)
        {
            if(message.EventMessageType == EventMessageType.PlayerReady) 
            {
                if(_room.Entities.TryGetValue(message.SourceId, out var entity))
                {
                    var player = _world.Players.Where(p => p == entity).Single();
                    player.State.IsReady = !player.State.IsReady;
                }
            }
        }

        public void Join(IClient client)
        {
            client.ClientRemoved += OnClientRemoved;
            _clients.Add(client);
            _server.AddClient(client, client.PlayerName);
        }

        void OnClientRemoved(IClient client)
        {
            var serverPeer = _server.ConnectedClients.Where(cl => cl.Identifier == client.PlayerName).Single();
            foreach (var entity in _server.Room.Entities.Values.Where(ent => ent.Controller == serverPeer))
            { 
                _server.Room.MarkForRemoval(entity.Id);
            }
            _server.RemoveClient(client);
            _clients.Remove(client);
        }

        void OnClientAdded(RailServerPeer client)
        {
            Messaging.Server.EntityServer entityServerSide = _room.AddNewEntity<Messaging.Server.EntityServer>();
            entityServerSide.AssignController(client);
            entityServerSide.CommandHandler = _commandHandler;
            entityServerSide.State.Health = 100;
            entityServerSide.State.Visibility = 100;
            client.Scope.Evaluator = new GhostScopeEvaluator();
            _world.AddPlayer(entityServerSide);
            _playersHaveJoined = true;
        }

        internal bool Update(float deltaTime)
        {
            var prepareForMatch = PreUpdateChecks();
            _commandHandler.UpdateDeltaTime(deltaTime);
            _server.Update();
            // TODO: move all the stuff below into an own Match class
            
            // Needed to make sure Event is not send to soon.
            if (_sessionState == SessionState.MatchEnded)
            {
                if (!_endEventMessageSent)
                {
                    _endEventMessageSent = true;
                    var eventMessage = _room.CreateEvent<EventMessage>();
                    eventMessage.EventMessageType = EventMessageType.MatchEnded;
                    _room.BroadcastEvent(eventMessage);
                }
                return true;
            }
            
            // skip one frame to make sure MatchStart event is send after player slot assignments
            // I don't know whiy, but this is needed for the order of messages to be received in the correct order
            if (prepareForMatch)
            {
                _logger.LogInformation("Skip one state check to prepare for match");
                return true;
            }
            if (_sessionState == SessionState.MatchInit && AllPlayersAreReady())
            {
                _sessionState = SessionState.MatchStarted;
                var eventMessage = _room.CreateEvent<EventMessage>();
                eventMessage.EventMessageType = EventMessageType.MatchStarted;
                _logger.LogInformation("Send event Message");
                _room.BroadcastEvent(eventMessage);
            }
            else if(_sessionState == SessionState.MatchStarted) 
            {
                if (_ghostState != null && _ghostState.Visibility < 100 && _ghostState.Visibility > 10)
                {
                    _ghostState.Visibility -= (uint)(100 * deltaTime);
                }
                var winState = _world.DetermineWinState();
                if(winState != WinState.None)
                {
                    _sessionState = SessionState.MatchEnded;

                    if (_ghostState != null)
                    {
                        _ghostState.Visibility = 100;
                    }
                    // make sure Health is send to client
                    // todo: check if this is really needed
                    _server.Update();
                    return true;
                }
            }
            // If all players have left the session, it can be closed!
            if(_playersHaveJoined && _clients.Count == 0)
            {
                return false;
            }
            return true;
        }

        private bool PreUpdateChecks()
        {
            if (_sessionState == SessionState.MatchInit 
                && AllPlayersAreReady() 
                && !_world.IsPreparedForMatch)
            {
                PrepareMatch();
                return true;
            }
            return false;
        }

        void PrepareMatch()
        {
            var players = _world.Players;
            players = Shuffle(players).ToList();
            for (int i = 0; i < (players.Count() - 1); ++i)
            {
                var entity = players.ElementAt(i);
                entity.State.PlayerSlot = i;
                if(i == 0)
                {
                    entity.State.Position = new Vector2(2, 18);
                }
                else if(i == 1)
                {
                    entity.State.Position = new Vector2(34, 18);
                }
                else if(i == 2)
                {
                    entity.State.Position = new Vector2(2, 2);
                }
                else if(i == 3)
                {
                    entity.State.Position = new Vector2(34, 2);
                }
            }
            // last player is always the ghost!
            var ghost = players.Last();
            _ghostState = ghost.State;
            _ghostState.PlayerSlot = PLAYER_SLOT_GHOST;
            _world.SetGhost(ghost);
            _ghostState.Position = new Vector2(18, 10);
            _ghostState.Visibility = 99;
        }

        bool AllPlayersAreReady()
        {
            return _room.Entities.Count > 1 && _world.Players.All(p => p.State.IsReady);
        }

        public static IEnumerable<IEntity> Shuffle(IEnumerable<IEntity> list)
        {
            return list.OrderBy(x => ThreadSafeRandom.ThisThreadsRandom.Next());
        }

        internal void Dispose()
        {
            foreach(var client in _clients)
            {
                client.Disconnect();
            }
            _clients.Clear();
            _eventSubscription.Dispose();
        }

        public static class ThreadSafeRandom
        {
            [ThreadStatic] private static Random? Local;

            public static Random ThisThreadsRandom
            {
                get { return Local ??= new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)); }
            }
        }

        bool _playersHaveJoined = false;
        IEntityState? _ghostState;
        SessionState _sessionState = SessionState.MatchInit;
        bool _endEventMessageSent = false;
        readonly ILogger _logger;
        readonly RailServer _server;
        readonly RailServerRoom _room;
        readonly IWorld _world;
        readonly IDisposable _eventSubscription;
        readonly Messaging.Server.CommandHandler _commandHandler;
        readonly List<IClient> _clients = new ();
        const UInt16 PLAYER_SLOT_GHOST = 5;
    }
}
