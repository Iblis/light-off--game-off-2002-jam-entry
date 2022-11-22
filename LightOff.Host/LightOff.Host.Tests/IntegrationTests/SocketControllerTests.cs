using LightOff.Host.Client;
using LightOff.Host.Session;
using LightOff.Messaging;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.Win32;
using Moq;
using RailgunNet;
using RailgunNet.Connection.Client;
using RailgunNet.Factory;
using RailgunNet.Logic;
using System.ComponentModel.DataAnnotations;
using Xunit.Abstractions;

namespace LightOff.Host.Tests.IntegrationTests
{
    public class SocketControllerTests
    {
        public SocketControllerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task ShouldConnect()
        {
            var application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        //services.AddSingleton<IHelloService, MockHelloService>();
                    });
                });

            var wsClient = application.Server.CreateWebSocketClient();
            var wsUri = new UriBuilder(application.Server.BaseAddress)
            {
                Scheme = "ws",
                Path = "session/myNewSession/playerOne"
            }.Uri;

            var railClient = CreateRailClient();
            var room = railClient.StartRoom();

            var webSocket = await wsClient.ConnectAsync(wsUri, CancellationToken.None);

            var logger = new Mock<ILogger>();
            var peer = new ClientPeer(webSocket, string.Empty, logger.Object);
            railClient.SetPeer(peer);
            _ = peer.StartListening();
            // No entities in room in the beginning
            Assert.Empty(room.Entities);
            // let server startup / send updates
            await Task.Delay(10);
            for (int i = 0; i < RailConfig.SERVER_SEND_RATE + RailConfig.CLIENT_SEND_RATE + 1; ++i)
            {
                railClient.Update();
            }
            // after some updates, room should have Entity from server
            Assert.Single(room.Entities);
        }

        RailClient CreateRailClient()
        {
            RailRegistry registry = new RailRegistry(RailgunNet.Component.Client);
            registry.AddEntityType<EntityClient, EntityState>();
            registry.SetCommandType<MoveCommand>();
            return new RailClient(registry);
        }

        readonly ITestOutputHelper _output;
    }

    internal class EntityClient : RailEntityClient<EntityState>
    {
    }
}
