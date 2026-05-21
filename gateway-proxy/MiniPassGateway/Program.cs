using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

var routes = new[]
{
    new RouteConfig()
    {
        RouteId = "app1-route",
        ClusterId = "app1-cluster",
        Match = new RouteMatch { Hosts = new[] { "app1.localhost" } }
    },
    new RouteConfig()
    {
        RouteId = "app2-route",
        ClusterId = "app2-cluster",
        Match = new RouteMatch { Hosts = new[] { "app2.localhost" } }
    }
};

var clusters = new[]
{
    new ClusterConfig()
    {
        ClusterId = "app1-cluster",
        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
        {
            { "destination1", new DestinationConfig() { Address = "http://localhost:5071" } }
        }
    },
    new ClusterConfig()
    {
        ClusterId = "app2-cluster",
        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
        {
            { "destination1", new DestinationConfig() { Address = "http://localhost:5266" } }
        }
    }
};

builder.Services.AddReverseProxy()
    .LoadFromMemory(routes, clusters);

var app = builder.Build();

app.MapReverseProxy();

app.Run();