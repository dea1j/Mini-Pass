using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;
using MiniPassGateway;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IProxyConfigProvider, RedisProxyConfigProvider>();

builder.Services.AddReverseProxy();

var app = builder.Build();

app.MapReverseProxy();

app.Run();