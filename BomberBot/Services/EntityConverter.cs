using BomberBot.Common;
using BomberBot.Domain.Objects;
using BomberBot.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace BomberBot.Services
{
    class EntityConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            var result = objectType == typeof(Entity) || objectType == typeof(PowerUp);
            return result;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jsonObject = JObject.Load(reader);
            var target = Create(jsonObject);
            if (target == null)
            {
                return null;
            }

            serializer.Populate(jsonObject.CreateReader(), target);

            return target;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public Entity Create(JObject jsonObject)
        {
            EntityType type;

            type = GameService.GetEntityType(jsonObject["type"].ToString());

            Entity entity = null;
            switch (type)
            {
                case EntityType.PlayerEntity:
                    entity = jsonObject.ToObject<Player>();
                    break;
                case EntityType.IndestructibleWallEntity:
                    entity = jsonObject.ToObject<IndestructibleWall>();
                    break;
                case EntityType.DestructibleWallEntity:
                    entity = jsonObject.ToObject<DestructibleWall>();
                    break;
                case EntityType.SuperPowerUp:
                    entity = jsonObject.ToObject<SuperPowerUp>();
                    break;
                case EntityType.BombBagPowerUpEntity:
                    entity = jsonObject.ToObject<BombBagPowerUp>();
                    break;
                case EntityType.BombRaduisPowerUpEntity:
                    entity = jsonObject.ToObject<BombRadiusPowerUp>();
                    break;
            }
            return entity;
        }
    }
}