using LiteDB;

namespace BurmaProjectIdeasYarp.Models
{
    public class YarpRoute
    {
        [BsonId]
        public string RouteId { get; set; } = string.Empty;
        public string ClusterId { get; set; } = string.Empty;
        public string MatchPath { get; set; } = string.Empty;
        public List<Dictionary<string, string>> Transforms { get; set; } = new();
        public bool Enabled { get; set; } = true;
    }
}
