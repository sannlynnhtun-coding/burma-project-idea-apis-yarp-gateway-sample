using BurmaProjectIdeasYarp.Models;
using LiteDB;
using Yarp.ReverseProxy.Configuration;

namespace BurmaProjectIdeasYarp.Services
{
    public class YarpConfigService
    {
        private readonly ILiteDatabase _db;
        private readonly ILiteCollection<YarpRoute> _routes;
        private readonly ILiteCollection<YarpCluster> _clusters;
        
        // Event to notify when config changes
        public event Action? OnConfigChanged;

        public YarpConfigService(ILiteDatabase db)
        {
            _db = db;
            _routes = _db.GetCollection<YarpRoute>("routes");
            _clusters = _db.GetCollection<YarpCluster>("clusters");
            
            // Ensure unique IDs
            _routes.EnsureIndex(x => x.RouteId, true);
            _clusters.EnsureIndex(x => x.ClusterId, true);
        }

        public async Task<List<YarpRoute>> GetRoutesAsync()
        {
            return await Task.Run(() => _routes.FindAll().ToList());
        }

        public async Task UpsertRouteAsync(YarpRoute route)
        {
            await Task.Run(() => _routes.Upsert(route));
            OnConfigChanged?.Invoke();
        }

        public async Task DeleteRouteAsync(string routeId)
        {
            await Task.Run(() => _routes.Delete(routeId));
            OnConfigChanged?.Invoke();
        }

        public async Task<List<YarpCluster>> GetClustersAsync()
        {
            return await Task.Run(() => _clusters.FindAll().ToList());
        }

        public async Task UpsertClusterAsync(YarpCluster cluster)
        {
            await Task.Run(() => _clusters.Upsert(cluster));
            OnConfigChanged?.Invoke();
        }

        public async Task DeleteClusterAsync(string clusterId)
        {
            await Task.Run(() => _clusters.Delete(clusterId));
            OnConfigChanged?.Invoke();
        }

        public (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) GetYarpConfig()
        {
            var routes = _routes.FindAll().Select(r => new RouteConfig
            {
                RouteId = r.RouteId,
                ClusterId = r.ClusterId,
                Match = new RouteMatch { Path = r.MatchPath },
                Transforms = r.Transforms.Select(t => (IReadOnlyDictionary<string, string>)t).ToList()
            }).ToList();

            var clusters = _clusters.FindAll().Select(c => new ClusterConfig
            {
                ClusterId = c.ClusterId,
                Destinations = c.Destinations.ToDictionary(
                    d => d.Key,
                    d => new DestinationConfig { Address = d.Value }
                ),
                LoadBalancingPolicy = c.LoadBalancingPolicy
            }).ToList();

            return (routes, clusters);
        }
    }
}
