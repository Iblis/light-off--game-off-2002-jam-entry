// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Host.Client;
using LightOff.Host.Session;
using Microsoft.AspNetCore.Mvc;

namespace LightOff.Host.Controllers
{
    [Route("session/{sessionName?}/{playerName?}")]
    [ApiController]
    public class SocketController : ControllerBase
    {
        public SocketController(IHostedGameSessions sessions, ILogger<SocketController> logger) 
        {
            _sessions = sessions;
            _logger = logger;
        }

        public async Task<IActionResult> AssignWebSocketToSession(string sessionName, string playerName)
        {
            // TODO: security checks
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                if (sessionName.Length > 12)
                {
                    return new BadRequestObjectResult("Session Name must be max 12 character long");
                }
                var session = _sessions.GetFor(sessionName);
                if (!session.CanBeJoined)
                {
                    return new BadRequestObjectResult("Session is not joinable (already full or closed)");
                }
                var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                
                var client = new ClientPeer(socket, playerName, _logger);
                session.Join(client);
                _logger.LogInformation("Listening to client {clientId} in Session {sessionName}", client.ConnectionId, sessionName);
                await client.StartListening();
                return new EmptyResult();
            }
            else
            {
                return new BadRequestResult();
            }
        }

        readonly IHostedGameSessions _sessions;
        readonly ILogger _logger;
    }
}
