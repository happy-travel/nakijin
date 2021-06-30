using System.Text.Json;
using StackExchange.Redis.Extensions.Core;

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public class StackExchangeRedisDefaultSerializer: ISerializer
    {
        public byte[] Serialize(object item)
            => JsonSerializer.SerializeToUtf8Bytes(item);
        

        public T? Deserialize<T>(byte[] serializedObject)
            => JsonSerializer.Deserialize<T>(serializedObject);
    }
}