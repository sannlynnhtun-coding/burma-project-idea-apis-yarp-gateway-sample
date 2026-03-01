using BurmaProjectIdeasYarp.Services;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace BurmaProjectIdeasYarp
{
    public class DynamicProxyConfigProvider : IProxyConfigProvider
    {
        private volatile IProxyConfig _config;
        private readonly YarpConfigService _configService;
        private readonly object _lockObject = new object();
        private CancellationTokenSource _changeTokenSource = new CancellationTokenSource();

        public DynamicProxyConfigProvider(YarpConfigService configService)
        {
            _configService = configService;
            var (routes, clusters) = _configService.GetYarpConfig();
            _config = new MemoryConfig(routes, clusters, _changeTokenSource.Token);
            
            // Subscribe to configuration changes
            _configService.OnConfigChanged += Reload;
        }

        public IProxyConfig GetConfig() => _config;

        public void Reload()
        {
            lock (_lockObject)
            {
                var (routes, clusters) = _configService.GetYarpConfig();

                // Cancel previous token and create new one
                var oldCts = _changeTokenSource;
                _changeTokenSource = new CancellationTokenSource();
                oldCts?.Cancel();
                
                _config = new MemoryConfig(routes, clusters, _changeTokenSource.Token);
            }
        }
    }

    public class MemoryConfig : IProxyConfig
    {
        public IReadOnlyList<RouteConfig> Routes { get; }
        public IReadOnlyList<ClusterConfig> Clusters { get; }
        public IChangeToken ChangeToken { get; }

        public MemoryConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters, CancellationToken cancellationToken)
        {
            Routes = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(cancellationToken);
        }
    }
}
