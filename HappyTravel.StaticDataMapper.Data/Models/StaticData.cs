using System.Text.Json;

namespace HappyTravel.Nakijin.Data.Models
{
    public class StaticData
    {
        public int Id { get; set; }
        public StaticDataTypes Type { get; set; }
        public JsonDocument Value { get; set; }
    }
}