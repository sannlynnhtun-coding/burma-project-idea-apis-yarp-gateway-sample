using BurmaProjectIdeasYarp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Yarp.ReverseProxy.Configuration;

namespace BurmaProjectIdeasYarp.Controllers
{
    [ApiController]
    [Route("api/gateway")]
    public class GatewayApiController : ControllerBase
    {
        private readonly IProxyConfigProvider _configProvider;
        private readonly IConfiguration _configuration;
        private readonly YarpConfigService _configService;

        private static readonly Dictionary<string, string> ApiConfigMap = new()
        {
            { "burma_calendar", "api-burma-calendar-routes.json" },
            { "burmese_recipes", "api-burmese-recipes-routes.json" },
            { "movie_ticket_online_booking_system", "api-movie-ticket-online-booking-system-routes.json" },
            { "snake", "api-snake-routes.json" }
        };

        public GatewayApiController(IProxyConfigProvider configProvider, IConfiguration configuration, YarpConfigService configService)
        {
            _configProvider = configProvider;
            _configuration = configuration;
            _configService = configService;
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            var proxyConfig = _configProvider.GetConfig();
            
            var routes = proxyConfig.Routes.Select(r => new
            {
                routeId = r.RouteId,
                clusterId = r.ClusterId,
                path = r.Match?.Path,
                order = r.Order,
                metadata = r.Metadata,
                transforms = r.Transforms
            }).ToList();

            var clusters = proxyConfig.Clusters.Select(c => new
            {
                clusterId = c.ClusterId,
                destinations = c.Destinations?.Select(d => new
                {
                    name = d.Key,
                    address = d.Value.Address,
                    health = d.Value.Health,
                    metadata = d.Value.Metadata
                }).ToList(),
                loadBalancingPolicy = c.LoadBalancingPolicy,
                sessionAffinity = c.SessionAffinity?.Policy,
                healthCheck = c.HealthCheck,
                httpClient = c.HttpClient,
                metadata = c.Metadata
            }).ToList();

            return Ok(new
            {
                routes = routes,
                clusters = clusters,
                routeCount = routes.Count,
                clusterCount = clusters.Count,
                lastUpdated = DateTime.UtcNow
            });
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var enabledApis = _configuration.GetSection("EnabledApis")
                .GetChildren()
                .ToDictionary(x => x.Key, x => x.Get<bool>());

            var apiStatus = ApiConfigMap.Select(kvp => new
            {
                apiKey = kvp.Key,
                apiName = ToTitleCase(kvp.Key),
                configFile = kvp.Value,
                enabled = enabledApis.TryGetValue(kvp.Key, out var enabled) && enabled,
                fileExists = System.IO.File.Exists(kvp.Value)
            }).ToList();

            return Ok(new
            {
                apis = apiStatus,
                totalApis = apiStatus.Count,
                enabledCount = apiStatus.Count(a => a.enabled),
                disabledCount = apiStatus.Count(a => !a.enabled)
            });
        }

        [HttpGet("all-json-routes/{apiKey}")]
        public IActionResult GetApiRoutes(string apiKey)
        {
            if (!ApiConfigMap.TryGetValue(apiKey, out var configFile))
            {
                return NotFound(new { message = $"API key '{apiKey}' not found" });
            }

            if (!System.IO.File.Exists(configFile))
            {
                return NotFound(new { message = $"Configuration file '{configFile}' not found" });
            }

            try
            {
                var json = System.IO.File.ReadAllText(configFile);
                var config = JsonSerializer.Deserialize<JsonElement>(json);

                return Ok(new
                {
                    apiKey = apiKey,
                    configFile = configFile,
                    configuration = config
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error reading configuration: {ex.Message}" });
            }
        }

        // --- Managed Config APIs (CRUD) ---

        [HttpGet("routes")]
        public async Task<IActionResult> GetRoutes()
        {
            var routes = await _configService.GetRoutesAsync();
            return Ok(routes);
        }

        [HttpPost("routes")]
        public async Task<IActionResult> UpsertRoute([FromBody] BurmaProjectIdeasYarp.Models.YarpRoute route)
        {
            if (string.IsNullOrEmpty(route.RouteId)) return BadRequest("RouteId is required");
            await _configService.UpsertRouteAsync(route);
            return Ok(new { message = "Route saved successfully", routeId = route.RouteId });
        }

        [HttpDelete("routes/{id}")]
        public async Task<IActionResult> DeleteRoute(string id)
        {
            await _configService.DeleteRouteAsync(id);
            return Ok(new { message = "Route deleted successfully", routeId = id });
        }

        [HttpGet("clusters")]
        public async Task<IActionResult> GetClusters()
        {
            var clusters = await _configService.GetClustersAsync();
            return Ok(clusters);
        }

        [HttpPost("clusters")]
        public async Task<IActionResult> UpsertCluster([FromBody] BurmaProjectIdeasYarp.Models.YarpCluster cluster)
        {
            if (string.IsNullOrEmpty(cluster.ClusterId)) return BadRequest("ClusterId is required");
            await _configService.UpsertClusterAsync(cluster);
            return Ok(new { message = "Cluster saved successfully", clusterId = cluster.ClusterId });
        }

        [HttpDelete("clusters/{id}")]
        public async Task<IActionResult> DeleteCluster(string id)
        {
            await _configService.DeleteClusterAsync(id);
            return Ok(new { message = "Cluster deleted successfully", clusterId = id });
        }

        private string ToTitleCase(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return string.Join(" ", str.Split('_')
                .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
        }
    }
}
