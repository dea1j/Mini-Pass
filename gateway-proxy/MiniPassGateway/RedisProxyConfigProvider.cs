using Microsoft.Extensions.Primitives;
using StackExchange.Redis;
using Yarp.ReverseProxy.Configuration;

namespace MiniPassGateway;

public class RedisProxyConfigProvider : IProxyConfigProvider
{
    private CustomConfig _config;
    private readonly ConnectionMultiplexer _redis;

    public RedisProxyConfigProvider()
    {
        _redis = ConnectionMultiplexer.Connect("redis-service:6379");
        LoadConfigFromRedis();
        var subscriber = _redis.GetSubscriber();
        subscriber.Subscribe("minipaas-updates", (channel, message) =>
        {
            Console.WriteLine("Hot-reloading routes from Redis");
            ReloadConfig();
        });
    }

    public IProxyConfig GetConfig() => _config;

    private void ReloadConfig()
    {
        var oldConfig = _config;
        LoadConfigFromRedis();
        oldConfig.SignalChange();
    }

    private void LoadConfigFromRedis()
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer("redis-service:6379");

        var routes = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();

        foreach (var key in server.Keys(pattern: "route:*"))
        {
            string domain = key.ToString().Replace("route:", "");
            string destinationUrl = db.StringGet(key);

            var routeId = $"{domain}-route";
            var clusterId = $"{domain}-cluster";

            routes.Add(new RouteConfig
            {
                RouteId = routeId,
                ClusterId = clusterId,
                Match = new RouteMatch { Hosts = new[] { domain } }
            });

            clusters.Add(new ClusterConfig
            {
                ClusterId = clusterId,
                Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                {
                    { "default", new DestinationConfig { Address = destinationUrl } }
                }
            });
        }

        _config = new CustomConfig(routes, clusters);
    }

    private class CustomConfig : IProxyConfig
    {
        private readonly CancellationChangeToken _changeToken;
        private readonly CancellationTokenSource _cts;

        public CustomConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            Routes = routes;
            Clusters = clusters;
            _cts = new CancellationTokenSource();
            _changeToken = new CancellationChangeToken(_cts.Token);
        }

        public IReadOnlyList<RouteConfig> Routes { get; }
        public IReadOnlyList<ClusterConfig> Clusters { get; }
        public IChangeToken ChangeToken => _changeToken;
        public void SignalChange() => _cts.Cancel();
    }
}