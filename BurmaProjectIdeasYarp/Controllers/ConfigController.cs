using BurmaProjectIdeasYarp.Models;
using BurmaProjectIdeasYarp.Services;
using Microsoft.AspNetCore.Mvc;

namespace BurmaProjectIdeasYarp.Controllers
{
    public class ConfigController : Controller
    {
        private readonly YarpConfigService _configService;

        public ConfigController(YarpConfigService configService)
        {
            _configService = configService;
        }

        public async Task<IActionResult> Routes()
        {
            var routes = await _configService.GetRoutesAsync();
            return View(routes);
        }

        [HttpGet]
        public async Task<IActionResult> EditRoute(string? id)
        {
            var clusters = await _configService.GetClustersAsync();
            ViewBag.Clusters = clusters;

            if (string.IsNullOrEmpty(id))
            {
                return View(new YarpRoute());
            }

            var routes = await _configService.GetRoutesAsync();
            var route = routes.FirstOrDefault(r => r.RouteId == id);
            if (route == null) return NotFound();

            return View(route);
        }

        [HttpPost]
        public async Task<IActionResult> EditRoute(YarpRoute route)
        {
            if (ModelState.IsValid)
            {
                await _configService.UpsertRouteAsync(route);
                return RedirectToAction(nameof(Routes));
            }
            
            var clusters = await _configService.GetClustersAsync();
            ViewBag.Clusters = clusters;
            return View(route);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRoute(string id)
        {
            await _configService.DeleteRouteAsync(id);
            return RedirectToAction(nameof(Routes));
        }

        public async Task<IActionResult> Clusters()
        {
            var clusters = await _configService.GetClustersAsync();
            return View(clusters);
        }

        [HttpGet]
        public async Task<IActionResult> EditCluster(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return View(new YarpCluster());
            }

            var clusters = await _configService.GetClustersAsync();
            var cluster = clusters.FirstOrDefault(c => c.ClusterId == id);
            if (cluster == null) return NotFound();

            return View(cluster);
        }

        [HttpPost]
        public async Task<IActionResult> EditCluster(YarpCluster cluster, string destinationNames, string destinationAddresses)
        {
            // Simple mapping for destinations from comma-separated strings or similar
            // In a real app, we'd use a more robust approach, but for this sample:
            var names = destinationNames?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() ?? new List<string>();
            var addresses = destinationAddresses?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() ?? new List<string>();

            cluster.Destinations = new Dictionary<string, string>();
            for (int i = 0; i < Math.Min(names.Count, addresses.Count); i++)
            {
                cluster.Destinations[names[i]] = addresses[i];
            }

            if (ModelState.IsValid)
            {
                await _configService.UpsertClusterAsync(cluster);
                return RedirectToAction(nameof(Clusters));
            }

            return View(cluster);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCluster(string id)
        {
            await _configService.DeleteClusterAsync(id);
            return RedirectToAction(nameof(Clusters));
        }
    }
}
