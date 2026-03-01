using BurmaProjectIdeasYarp.Services;
using Microsoft.AspNetCore.Mvc;

namespace BurmaProjectIdeasYarp.Controllers
{
    public class HomeController : Controller
    {
        private readonly YarpConfigService _configService;

        public HomeController(YarpConfigService configService)
        {
            _configService = configService;
        }

        public async Task<IActionResult> Index()
        {
            var routes = await _configService.GetRoutesAsync();
            var clusters = await _configService.GetClustersAsync();
            
            ViewBag.RouteCount = routes.Count;
            ViewBag.ClusterCount = clusters.Count;
            ViewBag.EnabledRouteCount = routes.Count(r => r.Enabled);

            return View();
        }

        public IActionResult UserGuide()
        {
            return View();
        }
    }
}
