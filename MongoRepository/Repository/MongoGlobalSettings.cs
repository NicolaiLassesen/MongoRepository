using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoRepository
{
    public sealed class MongoGlobalSettings
    {
        private static readonly List<Type> _enums;
        private static MongoGlobalSettings _instance;

        static MongoGlobalSettings()
        {
            _enums = new List<Type>();
        }

        private MongoGlobalSettings()
        {
            // DeSerialize DateTime as local format (not utc) - the Json representation will still be ISODate
            BsonSerializer.RegisterSerializer(typeof(DateTime), DateTimeSerializer.LocalInstance);
            foreach (Type enumType in _enums)
            {
                var serializerType = typeof(EnumSerializer<>);
                var constructedType = serializerType.MakeGenericType(enumType);
                var instance = Activator.CreateInstance(constructedType) as IBsonSerializer; // Should have BsonType.String as arg
                BsonSerializer.RegisterSerializer(enumType, instance);
            }
        }

        public static void Initialize()
        {
            if (_instance != null)
                return;
            _instance = new MongoGlobalSettings();
        }

        public static void RegisterEnumType<TEnum>() where TEnum : struct
        {
            if (!_enums.Contains(typeof(TEnum)))
                _enums.Add(typeof(TEnum));
        }
    }
}