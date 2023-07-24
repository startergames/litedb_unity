using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LiteDB {
    public static class LiteDbExtension {
        public static ILiteCollection<T> C<T>(this LiteDatabase db) => db.GetCollection<T>();
        public static ILiteCollection<T> Ci<T>(this LiteDatabase db) => db.GetCollectionAutoInclude<T>();

        private static class AutoIncludeMethod<T> {
            public static System.Func<int, ILiteCollection<T>> Func;
        }
        public static ILiteCollection<T> GetCollectionAutoInclude<T>(this LiteDatabase db, string name = null, BsonAutoId bsonAutoId = BsonAutoId.ObjectId, int maxDepth = 5) {
            var collection = db.GetCollection<T>(name, bsonAutoId);
            return AutoInclude(collection, typeof(T), maxDepth: maxDepth);
        }

        private static ILiteCollection<T> AutoInclude<T>(ILiteCollection<T> collection, Type type, string prefix = "$",
            int depth = 0, int maxDepth = 5) {
            if (depth >= maxDepth)
                return collection;

            if (AutoIncludeMethod<T>.Func == null) {
                var members = type.GetMembers()
                    .Where(m => Attribute.IsDefined(m, typeof(BsonRefAttribute)))
                    .Select(member => {
                        return member switch {
                            PropertyInfo { PropertyType: { IsArray: true } } property => (
                                path: $"{prefix}.{property.Name}[*]",
                                type: property.PropertyType.GetElementType()
                                ),
                            PropertyInfo property => (
                                path: $"{prefix}.{property.Name}",
                                type: property.PropertyType
                                ),
                            FieldInfo { FieldType: { IsArray: true } } field => (
                                path: $"{prefix}.{field.Name}[*]",
                                type: field.FieldType.GetElementType()
                                ),
                            FieldInfo field => (
                                path: $"{prefix}.{field.Name}", 
                                type: field.FieldType
                                ),
                            _ => throw new Exception($"Unknown member type: {member.GetType()}")
                        };
                    });

                AutoIncludeMethod<T>.Func = d => {
                    foreach (var member in members) {
                        collection = collection.Include(member.path);

                        if (member.type != null &&
                            member.type.GetMembers().Any(m => Attribute.IsDefined(m, typeof(BsonRefAttribute)))) {
                            collection = AutoInclude<T>(collection, member.type, member.path, ++d, maxDepth);
                        }
                    }

                    return collection;
                };
            }

            return AutoIncludeMethod<T>.Func(depth);
        }
    }
}