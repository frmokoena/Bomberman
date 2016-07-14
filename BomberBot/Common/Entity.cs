using BomberBot.Enums;
using BomberBot.Services;
using Newtonsoft.Json;

namespace BomberBot.Common
{
    public abstract class Entity
    {
        [JsonProperty("type")]
        private string _jsonType { get; set; }

        public Location Location { get; set; }

        public EntityType Type
        {
            get
            {
                return _jsonType != null ? GameService.GetEntityType(_jsonType) : EntityType.PlayerEntity;
            }
        }
    }
}