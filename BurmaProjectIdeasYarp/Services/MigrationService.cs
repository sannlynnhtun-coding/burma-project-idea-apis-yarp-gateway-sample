using BurmaProjectIdeasYarp.Models;
using BurmaProjectIdeasYarp.Services;
using System.Text.Json;

namespace BurmaProjectIdeasYarp.Services
{
    public class MigrationService
    {
        private readonly YarpConfigService _configService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MigrationService> _logger;

        private static readonly Dictionary<string, string> ApiConfigMap = new()
        {
            { "burma_calendar", "api-burma-calendar-routes.json" },
            { "burmese_recipes", "api-burmese-recipes-routes.json" },
            { "movie_ticket_online_booking_system", "api-movie-ticket-online-booking-system-routes.json" },
            { "snake", "api-snake-routes.json" }
        };

        public MigrationService(YarpConfigService configService, IConfiguration configuration, ILogger<MigrationService> logger)
        {
            _configService = configService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task MigrateAsync()
        {
            var enabledApis = _configuration.GetSection("EnabledApis").GetChildren()
                .Where(x => x.Get<bool>())
                .Select(x => x.Key)
                .ToList();

            if (!enabledApis.Any())
            {
                _logger.LogWarning("No APIs enabled in api-settings.json. Skipping seed.");
                return;
            }

            _logger.LogInformation("Checking for seed data from JSON files...");

            var currentRoutes = await _configService.GetRoutesAsync();
            var currentClusters = await _configService.GetClustersAsync();

            foreach (var apiKey in enabledApis)
            {
                if (!ApiConfigMap.TryGetValue(apiKey, out var configFile)) continue;

                if (File.Exists(configFile))
                {
                    try
                    {
                        var json = File.ReadAllText(configFile);
                        var config = JsonSerializer.Deserialize<JsonElement>(json);

                        if (config.TryGetProperty("ReverseProxy", out var reverseProxy))
                        {
                            // Load clusters first
                            if (reverseProxy.TryGetProperty("Clusters", out var clusters))
                            {
                                foreach (var cluster in clusters.EnumerateObject())
                                {
                                    if (currentClusters.Any(c => c.ClusterId == cluster.Name)) continue;

                                    var yarpCluster = new YarpCluster
                                    {
                                        ClusterId = cluster.Name,
                                        Destinations = new Dictionary<string, string>()
                                    };

                                    if (cluster.Value.TryGetProperty("Destinations", out var destinations))
                                    {
                                        foreach (var dest in destinations.EnumerateObject())
                                        {
                                            var address = dest.Value.GetProperty("Address").GetString() ?? "";
                                            yarpCluster.Destinations[dest.Name] = address;
                                        }
                                    }
                                    
                                    await _configService.UpsertClusterAsync(yarpCluster);
                                    _logger.LogInformation("Seeded cluster: {ClusterId}", cluster.Name);
                                }
                            }

                            // Load routes
                            if (reverseProxy.TryGetProperty("Routes", out var routes))
                            {
                                foreach (var route in routes.EnumerateObject())
                                {
                                    if (currentRoutes.Any(r => r.RouteId == route.Name)) continue;

                                    var yarpRoute = new YarpRoute
                                    {
                                        RouteId = route.Name,
                                        ClusterId = route.Value.GetProperty("ClusterId").GetString() ?? "",
                                        MatchPath = route.Value.GetProperty("Match").GetProperty("Path").GetString() ?? "",
                                        Transforms = new List<Dictionary<string, string>>()
                                    };

                                    if (route.Value.TryGetProperty("Transforms", out var transforms))
                                    {
                                        foreach (var transform in transforms.EnumerateArray())
                                        {
                                            var transformDict = new Dictionary<string, string>();
                                            foreach (var prop in transform.EnumerateObject())
                                            {
                                                transformDict[prop.Name] = prop.Value.GetString() ?? "";
                                            }
                                            yarpRoute.Transforms.Add(transformDict);
                                        }
                                    }

                                    await _configService.UpsertRouteAsync(yarpRoute);
                                    _logger.LogInformation("Seeded route: {RouteId}", route.Name);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error seeding {ConfigFile}", configFile);
                    }
                }
            }
        }
    }
}
