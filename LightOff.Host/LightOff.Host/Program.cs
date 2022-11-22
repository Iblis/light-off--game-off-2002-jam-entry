// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using LightOff.Host;
using LightOff.Host.Session;
using LightOff.Messaging;
using LightOff.Messaging.Server;
using RailgunNet.Factory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<IHostedGameSessions, HostedGameSessions>();
builder.Services.AddSingleton<IGameLoop, LoopHostedService>();
builder.Services.AddHostedService( (sp) => sp.GetService<IGameLoop>() as LoopHostedService);

RailRegistry registry = new RailRegistry(RailgunNet.Component.Server);
registry.AddEntityType<EntityServer, EntityState>();
registry.SetCommandType<MoveCommand>();
builder.Services.AddSingleton(registry);

var app = builder.Build();
// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseWebSockets();
app.Run();


// needed to be able to test this setup via WebApplicationFactory
public partial class Program { }